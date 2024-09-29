using Iot.Device.RotaryEncoder;
using Microsoft.Extensions.Logging;
using System.Device.Gpio;

namespace InputControls
{
    public class ControlKnob : IDisposable
    {
        private readonly ILogger? logger;
        private readonly GpioController controller;
        private readonly QuadratureRotaryEncoder rotaryEncoder;
        private readonly List<Action> cleanupActions = new();

        private readonly System.Timers.Timer buttonFirstClickTimer = new(TimeSpan.FromMilliseconds(250)) { AutoReset = false };
        private readonly System.Timers.Timer buttonSecondClickTimer = new(TimeSpan.FromMilliseconds(350)) { AutoReset = false };
        private readonly System.Timers.Timer buttonLongPressTimer = new(TimeSpan.FromMilliseconds(2500)) { AutoReset = false };

        public ControlKnob(int pinA, int pinB, int pinButton, int pinSwitch, int pulsesPerRotation, GpioController? controller = null, ILoggerFactory? loggerFactory = null)
        {
            logger = loggerFactory?.CreateLogger<ControlKnob>();

            if (controller == null)
            {
                try
                {
                    controller = new();
                    cleanupActions.Add(controller.Dispose);
                }
                catch (Exception exception)
                {
                    logger?.LogError(exception, $"Failed to initialize GPIO controller due to an error.");
                    throw;
                }
            }

            this.controller = controller;

            try
            {
                controller.OpenPin(pinA, PinMode.Input);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to open GPIO pin {pinA} (A) due to an error.");
                throw;
            }

            try
            {
                controller.OpenPin(pinB, PinMode.Input);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to open GPIO pin {pinB} (B) due to an error.");
                throw;
            }

            try
            {
                controller.OpenPin(pinButton, PinMode.Input);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to open GPIO pin {pinButton} (Button) due to an error.");
                throw;
            }

            try
            {
                controller.OpenPin(pinSwitch, PinMode.Input);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to open GPIO pin {pinSwitch} (Switch) due to an error.");
                throw;
            }

            logger?.LogDebug($"Opened GPIO pins: A={pinA}, B={pinB}, Button={pinButton}, Switch={pinSwitch}");

            buttonSecondClickTimer.Elapsed += (s, e) => RaiseButtonClickEvent();
            buttonLongPressTimer.Elapsed += (s, e) => RaiseButtonLongPressEvent();

            cleanupActions.Add(controller.RegisterCallbackForPinValueChangedEvent(pinButton, debounce: TimeSpan.FromMilliseconds(10), (s, e) =>
            {
                if (e.ChangeType == PinEventTypes.Rising)
                {
                    IsButtonPressed = true;
                    logger?.LogDebug("Button pressed");
                    RaiseButtonPressedEvent();

                    // After button is pressed down, if there are no timers enabled, start timer for detecting first click and long press
                    if (!buttonFirstClickTimer.Enabled && !buttonSecondClickTimer.Enabled)
                    {
                        buttonFirstClickTimer.Start();
                        buttonLongPressTimer.Start();
                    }
                }
                else
                {
                    IsButtonPressed = false;
                    logger?.LogDebug("Button released");

                    // Reset long press timer
                    buttonLongPressTimer.Stop();

                    // After button is released, if timer for first click is enabled, stop it and start timer for second click
                    if (buttonFirstClickTimer.Enabled)
                    {
                        buttonFirstClickTimer.Stop();
                        buttonSecondClickTimer.Start();
                    }

                    // otherwise if timer for second click is enabled, raise button double click event
                    else if (buttonSecondClickTimer.Enabled)
                    {
                        RaiseButtonDoubleClickEvent();
                    }

                    // otherwise, since no timers are enabled, raise a plain button release event
                    else
                    {
                        RaiseButtonReleasedEvent();
                    }
                }
            }));

            IsButtonPressed = controller.Read(pinButton) == PinValue.High;

            cleanupActions.Add(controller.RegisterCallbackForPinValueChangedEvent(pinSwitch, debounce: TimeSpan.FromMilliseconds(40), (s, e) =>
            {
                if (e.ChangeType == PinEventTypes.Rising)
                {
                    IsSwitchOn = true;
                    logger?.LogDebug("Switch ON");
                    SwitchOnEvent?.Invoke(this, new EventArgs());
                }
                else
                {
                    IsSwitchOn = false;
                    logger?.LogDebug("Switch OFF");
                    SwitchOffEvent?.Invoke(this, new EventArgs());
                }
            }));

            IsSwitchOn = controller.Read(pinSwitch) == PinValue.High;

            rotaryEncoder = new(pinA, pinB, edges: PinEventTypes.Rising, pulsesPerRotation, controller, shouldDispose: false);
            rotaryEncoder.Debounce = TimeSpan.FromMilliseconds(2);

            int previousValue = 0;
            rotaryEncoder.PulseCountChanged += (s, e) =>
            {
                int value = (int)e.Value;
                ControlKnobRotatedEvent?.Invoke(this, new ControlKnobRotatedEventArgs(value, previousValue, IsButtonPressed, IsSwitchOn));
                previousValue = value;
            };
        }

        public bool IsButtonPressed { get; private set; }
        public bool IsSwitchOn { get; private set; }

        public delegate void ControlKnobRotatedEventHandler(object sender, ControlKnobRotatedEventArgs e);

        public event ControlKnobRotatedEventHandler ControlKnobRotatedEvent;

        public delegate void ButtonEventHandler(object sender, ButtonEventArgs e);

        private void RaiseButtonPressedEvent()
        {
            ButtonPressedEvent?.Invoke(this, new ButtonEventArgs(ButtonEventArgs.ButtonEventTypeFlags.Pressed));
        }
        private void RaiseButtonReleasedEvent()
        {
            ButtonReleasedEvent?.Invoke(this, new ButtonEventArgs(ButtonEventArgs.ButtonEventTypeFlags.Released));
        }
        private void RaiseButtonClickEvent()
        {
            var eventArgs = new ButtonEventArgs(ButtonEventArgs.ButtonEventTypeFlags.Click);
            ButtonReleasedEvent?.Invoke(this, eventArgs);
            ButtonClickEvent?.Invoke(this, eventArgs);
        }
        private void RaiseButtonDoubleClickEvent()
        {
            buttonSecondClickTimer.Stop();
            var eventArgs = new ButtonEventArgs(ButtonEventArgs.ButtonEventTypeFlags.DoubleClick);
            ButtonReleasedEvent?.Invoke(this, eventArgs);
            ButtonDoubleClickEvent?.Invoke(this, eventArgs);
        }
        private void RaiseButtonLongPressEvent()
        {
            ButtonLongPressEvent?.Invoke(this, new ButtonEventArgs(ButtonEventArgs.ButtonEventTypeFlags.LongPress));
        }

        public event ButtonEventHandler ButtonPressedEvent;
        public event ButtonEventHandler ButtonReleasedEvent;
        public event ButtonEventHandler ButtonClickEvent;
        public event ButtonEventHandler ButtonDoubleClickEvent;
        public event ButtonEventHandler ButtonLongPressEvent;

        public event EventHandler SwitchOnEvent;
        public event EventHandler SwitchOffEvent;

        public void Dispose()
        {
            rotaryEncoder.Dispose();
            cleanupActions.ForEach(action => action());
        }
    }

    public class ControlKnobRotatedEventArgs : EventArgs
    {
        public ControlKnobRotatedEventArgs(int currentValue, int previousValue, bool isButtonPressed, bool isSwitchOn)
        {
            CurrentValue = currentValue;
            PreviousValue = previousValue;
            IsButtonPressed = isButtonPressed;
            IsSwitchOn = isSwitchOn;
        }

        public int CurrentValue { get; }
        public int PreviousValue { get; }
        public int ValueChange => CurrentValue - PreviousValue;
        public bool IsButtonPressed { get; }
        public bool IsSwitchOn { get; }
    }

    public class ButtonEventArgs(ButtonEventArgs.ButtonEventTypeFlags eventTypeFlags) : EventArgs
    {
        [Flags]
        public enum ButtonEventTypeFlags
        {
            Pressed     = 0b00001,
            Released    = 0b00010,
            Click       = 0b00110,
            DoubleClick = 0b01010,
            LongPress   = 0b10000
        }

        public ButtonEventTypeFlags eventTypeFlags { get; } = eventTypeFlags;

        public bool IsClick => eventTypeFlags.HasFlag(ButtonEventTypeFlags.Click);
        public bool IsDoubleClick => eventTypeFlags.HasFlag(ButtonEventTypeFlags.DoubleClick);
        public bool IsPressed => eventTypeFlags.HasFlag(ButtonEventTypeFlags.Pressed);
        public bool IsReleased => eventTypeFlags.HasFlag(ButtonEventTypeFlags.Released);
    }
}