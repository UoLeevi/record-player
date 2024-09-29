using System.Device.Gpio;
using System.Diagnostics;

namespace InputControls
{
    internal static class Extensions
    {
        // This function registers a callback for a GPIO pin value changed event with debouncing logic
        public static Action RegisterCallbackForPinValueChangedEvent(this GpioController controller, int pinNumber, TimeSpan debounce, PinChangeEventHandler callback)
        {
            // Start a stopwatch to track time between events
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Let's use CancellationTokenSource to control the debounce delay task
            CancellationTokenSource? cts = null;

            // Store the last stable event type to filter out bounces. Initial state is unknown.
            PinEventTypes previousStableEventType = PinEventTypes.None;

            // Define the event handler for when a pin value changes
            PinChangeEventHandler eventHandler = (s, e) =>
            {
                // Cancel possible previous delayed event
                if (cts?.IsCancellationRequested is false) cts.Cancel();

                // If events occur in quick succession, let's delay event processing a bit to verify that the event was not caused by a bounce
                if (stopwatch.Elapsed < debounce)
                {
                    // Create new CancellationTokenSource
                    cts = new();

                    // Delay the event handling for the debounce interval
                    Task.Delay(debounce, cts.Token)
                        .ContinueWith((t, state) =>
                        {
                            var ownedCts = (CancellationTokenSource)state!;

                            // Dispose the CancellationTokenSource used for this task
                            ownedCts.Dispose();

                            // Set current CancellationTokenSource to null to ensure it is not used after being disposed
                            if (cts == ownedCts) cts = null;

                            // If the task was canceled, it means another event came in during the delay, so we exit
                            if (t.IsCanceled) return;

                            // If the new event type is the same as the last stable one, we consider it as bounce, so we exit
                            if (e.ChangeType == previousStableEventType) return;

                            // Update the last stable event type
                            previousStableEventType = e.ChangeType;

                            // Invoke the callback as the delayed event can be considered stable
                            callback(s, e);
                        }, cts, TaskScheduler.Default);

                    return;
                }

                // Reset the stopwatch for the next event
                stopwatch.Restart();

                // If the new event type is the same as the last stable one, we consider it as bounce, so we exit
                if (e.ChangeType == previousStableEventType) return;

                // Update the last stable event type
                previousStableEventType = e.ChangeType;

                // Invoke the callback as the event is considered stable
                callback(s, e);
            };

            // Register the event handler with the GPIO controller for both rising and falling edge events
            controller.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Rising | PinEventTypes.Falling, eventHandler);

            // Return an action that can be called to unregister the event handler
            return () => controller.UnregisterCallbackForPinValueChangedEvent(pinNumber, eventHandler);
        }
    }
}