using Microsoft.Extensions.Logging;
using System.Device.Gpio;

namespace InputControls
{
    public class Led : IDisposable
    {
        private readonly ILogger? logger;

        private readonly GpioController controller;
        private readonly List<Action> cleanupActions = new();

        private CancellationTokenSource blinkingCts;
        private static readonly TimeSpan blinkOnDuration = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan blinkOffDuration = TimeSpan.FromMilliseconds(500);

        public Led(int pin, GpioController? controller = null, ILoggerFactory? loggerFactory = null)
        {
            logger = loggerFactory?.CreateLogger<Led>();

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
                controller.OpenPin(pin, PinMode.Output);
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to open GPIO pin {pin} due to an error.");
                throw;
            }

            Pin = pin;
        }

        public int Pin { get; }

        public bool IsOn => controller.Read(Pin) == PinValue.High;
        public bool IsBlinking { get; private set; }

        public void On()
        {
            controller.Write(Pin, PinValue.High);
        }

        public void Off()
        {
            controller.Write(Pin, PinValue.Low);
        }

        public async Task BlinkOnce()
        {
            controller.Write(Pin, PinValue.High);
            await Task.Delay(blinkOnDuration);
            controller.Write(Pin, PinValue.Low);
        }

        public async Task StartBlinking()
        {
            if (IsBlinking) return;

            IsBlinking = true;
            blinkingCts = new CancellationTokenSource();

            try
            {
                while (IsBlinking)
                {
                    controller.Write(Pin, PinValue.High);
                    await Task.Delay(blinkOnDuration, blinkingCts.Token);
                    controller.Write(Pin, PinValue.Low);
                    await Task.Delay(blinkOffDuration, blinkingCts.Token);
                }
            }
            catch (TaskCanceledException exception)
            {
                
            }
        }

        public async Task StopBlinking()
        {
            if (!IsBlinking) return;
            
            IsBlinking = false;
            await blinkingCts.CancelAsync();
            blinkingCts.Dispose();

            controller.Write(Pin, PinValue.Low);
        }

        public void Dispose()
        {
            cleanupActions.ForEach(action => action());
        }
    }
}