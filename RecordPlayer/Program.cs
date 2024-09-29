// Record Player
using InputControls;
using Microsoft.Extensions.Logging;

// Step 1. Configure logging

using var loggerFactory = LoggerFactory.Create(builder => builder
    .AddFilter(typeof(Program).FullName, LogLevel.Information)
    .AddFilter(typeof(RecordPlayer).FullName, LogLevel.Information)
    .AddFilter(typeof(Spotify.ApiClient).FullName, LogLevel.Warning)
    .AddFilter(typeof(ControlKnob).FullName, LogLevel.Warning)
    .AddFilter(typeof(Led).FullName, LogLevel.Warning)
    .AddConsole());

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Starting record player...");

// Step 2. Configure GPIO devices

RecordPlayerGpioConfig gpioConfig = new()
{
    LedPin = 26,

    ControlKnobPinA = 16,
    ControlKnobPinB = 12,
    ControlKnobPinButton = 13,
    ControlKnobPinSwitch = 6,
    ControlKnobPulsesPerRotation = 20,

    RfidReaderPinReset = 25,
    RfidReaderPinSS = 7,
    RfidReaderSpiBusId = 0,
    RfidReaderSpiChipSelectLine = 0
};

// Step 3. Initialize record player

RecordPlayer player = new(gpioConfig, loggerFactory);
if (!await player.Initilize())
{
    logger.LogWarning("Failed to initiailize record player");
    return;
}

logger.LogInformation("Record player started.");

// Step 4. Wait until exit termination is signaled

CancellationTokenSource cts = new();
Console.CancelKeyPress += (s, e) => cts.Cancel();

try
{
    await Task.Delay(Timeout.Infinite, cts.Token).ConfigureAwait(false);
}
catch (TaskCanceledException)
{
    logger.LogInformation("Record player stopping...");
    player.Dispose();
}

logger.LogInformation("Record player stopped.");
