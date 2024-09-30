// Record Player
using InputControls;
using Microsoft.Extensions.Logging;
using System.Device.Gpio;
using System.Text.Json;
using System.Text.Json.Serialization;

public class RecordPlayer : IDisposable
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger logger;

    private readonly Spotify.ApiClient spotifyClient;

    private readonly GpioController gpioController;
    private readonly ControlKnob knob;
    private readonly Led led;
    private readonly RfidReader rfidReader;

    const string defaultMusicRecordConfigurationFilepath = "music-record-config.json";
    private readonly string musicRecordConfigurationFilepath;
    private MusicRecordConfiguration? musicRecordConfig;

    public RecordPlayer(RecordPlayerGpioConfig gpioConfig, ILoggerFactory loggerFactory)
    {
        // Step 1. Create logger

        logger = loggerFactory.CreateLogger<RecordPlayer>();

        // Step 2. Resolve music record configuration path

        musicRecordConfigurationFilepath = Environment.GetEnvironmentVariable("MUSIC_RECORD_CONFIG_PATH") ?? defaultMusicRecordConfigurationFilepath;

        // Step 3. Create Spotify API client

        spotifyClient = new(loggerFactory);

        // Step 4. Initialize GPIO devices

        gpioController = new();

        led = new(
            pin: gpioConfig.LedPin,
            controller: gpioController,
            loggerFactory: loggerFactory);

        knob = new(
            pinA: gpioConfig.ControlKnobPinA,
            pinB: gpioConfig.ControlKnobPinB,
            pinButton: gpioConfig.ControlKnobPinButton,
            pinSwitch: gpioConfig.ControlKnobPinSwitch,
            pulsesPerRotation: gpioConfig.ControlKnobPulsesPerRotation,
            controller: gpioController,
            loggerFactory: loggerFactory);

        rfidReader = new(
            pinReset: gpioConfig.RfidReaderPinReset,
            pinSS: gpioConfig.RfidReaderPinSS,
            spiBusId: gpioConfig.RfidReaderSpiBusId,
            spiChipSelectLine: gpioConfig.RfidReaderSpiChipSelectLine,
            controller: gpioController,
            loggerFactory: loggerFactory);
    }

    public bool IsInitialized { get; private set; }

    public bool IsPlaying { get; private set; }

    public bool IsRfidWritingMode { get; private set; }

    public async Task<bool> Initilize()
    {
        if (IsInitialized) return true;

        // Step 1. Authenticate Spotify API client

        bool spotifyAuthenticated = await spotifyClient.Authenticate();

        if (!spotifyAuthenticated) return false;

        // Step 2. Get current playback state

        var playbackState = await spotifyClient.GetPlaybackState();

        var devices = await spotifyClient.GetDevices();
        if (devices == null || devices.Count == 0)
        {
            logger?.LogWarning("No playback devices are available.");
            return false;
        }

        logger.LogInformation($"Available devices:\n{string.Join("\n", devices.Select(d => $"- id: {d.Id}, name: {d.Name}, type: {d.Type}, active: {d.IsActive}"))}");

        IsPlaying = playbackState?.IsPlaying is true;

        Spotify.Track currentTrack;

        if (IsPlaying is true)
        {
            currentTrack = playbackState.Item;
            logger?.LogInformation($"Playing: {currentTrack.Name} by {currentTrack.Artists.FirstOrDefault().Name}");
        }

        // Step 3. Read configuration file containing music records

        if (File.Exists(musicRecordConfigurationFilepath))
        {
            using var musicRecordConfigurationFile = File.OpenRead(musicRecordConfigurationFilepath);
            musicRecordConfig = await JsonSerializer.DeserializeAsync<MusicRecordConfiguration>(musicRecordConfigurationFile);
            logger?.LogDebug($"Loaded {musicRecordConfig.Records.Count} music records from file '{musicRecordConfigurationFilepath}'.");
        }
        else
        {
            logger?.LogWarning($"Music records config file '{musicRecordConfigurationFilepath}' does not exist.");

            musicRecordConfig = new();
        }

        // Step 4. Configure event handlers for input devices

        // Step 4.1. Button single click is used to pause or resume playback

        knob.ButtonClickEvent += async (s, e) =>
        {
            await SetRfidReadingMode();

            led.On();

            try
            {
                if (IsPlaying)
                {
                    await PausePlayback();
                }
                else
                {
                    await ResumePlayback();
                }
            }
            finally
            {
                led.Off();
            }
        };

        // Step 4.2. Button double click is used to skip to next song or resume playback

        knob.ButtonDoubleClickEvent += async (s, e) =>
        {
            await SetRfidReadingMode();

            led.On();

            try
            {
                if (IsPlaying)
                {
                    await SkipToNextTrack();
                }
                else
                {
                    await ResumePlayback();
                }
            }
            finally
            {
                led.Off();
            }
        };

        // Step 4.3. Button long press is used to start RFID writing mode if playback is on

        knob.ButtonLongPressEvent += (s, e) =>
        {
            if (IsPlaying)
            {
                SetRfidWritingMode();
            }
        };

        // Step 4.4. Knob rotation is used to control volume

        int volumePercent = playbackState?.Device.VolumePercent ?? 50;
        knob.ControlKnobRotatedEvent += async (s, e) =>
        {
            volumePercent = Math.Clamp(volumePercent + e.ValueChange, min: 1, max: 100);

            led.On();

            try
            {
                await spotifyClient.SetVolume(volumePercent);
            }
            finally
            {
                led.Off();
            }
        };

        // Step 4.5. RFID reader is used to play records or write new records to tags

        TimeSpan tagReadingDelay = TimeSpan.FromMilliseconds(200);
        string? previousNfcId = null;
        System.Timers.Timer previousNfcIdResetTimer = new(TimeSpan.FromMilliseconds(5000)) { AutoReset = false };
        previousNfcIdResetTimer.Elapsed += (s, e) => previousNfcId = null;

        rfidReader.RfidTagReadEvent += async (s, e) =>
        {
            // Step 1. If same tag is read in quick succession, ignore the event

            if (e.NfcId == previousNfcId)
            {
                await Task.Delay(tagReadingDelay);
                rfidReader.ResumeReading();
                return;
            }

            // Step 2. Turn on the led to indicate that tag was successfully read

            if (led.IsBlinking)
            {
                await led.StopBlinking();
            }

            led.On();

            try
            {
                logger?.LogInformation($"Read RFID tag with ID '{e.NfcId}'");

                // Step 3. Depending on if the player is on writing or reading mode do following:
                // In writing mode, store currently playing track info to music record.
                // In reading mode, play track on the music record.

                if (IsRfidWritingMode)
                {
                    await SetRfidReadingMode();
                    await SaveCurrentlyPlayingTrackToRecord(e.NfcId);
                }
                else if (musicRecordConfig.Records.TryGetValue(e.NfcId, out var record))
                {
                    await PlayRecord(record);
                }
            }
            finally
            {
                // Step 4. Wait a short moment

                await Task.Delay(tagReadingDelay);

                // Step 5. Turn off the led

                led.Off();

                // Step 6. Save value for previous NFC ID and set its' reset timer

                previousNfcId = e.NfcId;
                previousNfcIdResetTimer.Start();

                // Step 7. Resume reading

                rfidReader.ResumeReading();
            }
        };

        rfidReader.ResumeReading();

        IsInitialized = true;
        return true;
    }

    private async Task SetRfidReadingMode()
    {
        if (!IsRfidWritingMode) return;

        IsRfidWritingMode = false;
        await led.StopBlinking();
    }

    private void SetRfidWritingMode()
    {
        if (IsRfidWritingMode) return;

        IsRfidWritingMode = true;
        _ = led.StartBlinking();
    }

    private async Task<MusicRecord?> SaveCurrentlyPlayingTrackToRecord(string nfcId)
    {
        var currentlyPlayingTrack = await spotifyClient.GetCurrentlyPlayingTrack();
        if (currentlyPlayingTrack?.IsPlaying is not true) return null;

        Spotify.Track currentTrack = currentlyPlayingTrack.Item;

        MusicRecord? record = new()
        {
            NfcId = nfcId,
            TrackUri = currentTrack.Uri,
            ContextUri = currentlyPlayingTrack.Context?.Uri
        };

        musicRecordConfig.Records[record.NfcId] = record;
        using var musicRecordConfigurationFile = File.OpenWrite(musicRecordConfigurationFilepath);
        await JsonSerializer.SerializeAsync(musicRecordConfigurationFile, musicRecordConfig, jsonSerializerOptions);
        musicRecordConfigurationFile.SetLength(musicRecordConfigurationFile.Position);

        logger?.LogInformation($"Saved '{currentTrack.Name}' by {currentTrack.Artists.FirstOrDefault().Name} with ID '{record.NfcId}'");

        return record;
    }

    private async Task PlayRecord(MusicRecord record)
    {
        if (await spotifyClient.Play(record.TrackUri, record.ContextUri))
        {
            logger?.LogInformation("Played record.");
            IsPlaying = true;
        }
        else
        {
            logger?.LogWarning($"Failed to play record with track URI '{record.TrackUri}' and context URI '{record.ContextUri}'.");
        }
    }

    private async Task ResumePlayback()
    {
        if (await spotifyClient.Resume())
        {
            logger?.LogInformation("Playback resumed.");
            IsPlaying = true;
        }
    }

    private async Task PausePlayback()
    {
        if (await spotifyClient.Pause())
        {
            logger?.LogInformation("Playback paused.");
            IsPlaying = false;
        }
    }

    private async Task SkipToNextTrack()
    {
        if (await spotifyClient.SkipToNext())
        {
            logger?.LogInformation("Skipped to next track.");
            IsPlaying = true;
        }
    }

    public void Dispose()
    {
        spotifyClient.Dispose();
        rfidReader.Dispose();
        knob.Dispose();
        led.Dispose();
        gpioController.Dispose();
    }
}

public record RecordPlayerGpioConfig
{
    public int LedPin { get; init; }

    public int ControlKnobPinA { get; init; }
    public int ControlKnobPinB { get; init; }
    public int ControlKnobPinButton { get; init; }
    public int ControlKnobPinSwitch { get; init; }
    public int ControlKnobPulsesPerRotation { get; init; }

    public int RfidReaderPinReset { get; init; }
    public int RfidReaderPinSS { get; init; }
    public int RfidReaderSpiBusId { get; init; }
    public int RfidReaderSpiChipSelectLine { get; init; }
}

public record MusicRecordConfiguration
{
    [JsonPropertyName("records")]
    public Dictionary<string, MusicRecord> Records { get; set; } = new();
}

public record MusicRecord
{
    [JsonPropertyName("nfc_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NfcId { get; set; }

    [JsonPropertyName("track_uri")]
    public string TrackUri { get; set; } = "";

    [JsonPropertyName("context_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContextUri { get; set; }
}