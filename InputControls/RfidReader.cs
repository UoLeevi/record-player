using Iot.Device.Mfrc522;
using Iot.Device.Rfid;
using Microsoft.Extensions.Logging;
using System.Device.Gpio;
using System.Device.Spi;

namespace InputControls
{
    public class RfidReader : IDisposable
    {
        private readonly ILogger? logger;
        private SemaphoreSlim semaphore;
        private Thread thread;
        private readonly MfRc522 mfrc522;
        private bool quit;

        private readonly static TimeSpan pauseBetweenReading = TimeSpan.FromMilliseconds(250);
        private readonly static TimeSpan activeReadingDuration = TimeSpan.FromMilliseconds(10);

        public RfidReader(int pinReset, int pinSS, int spiBusId, int spiChipSelectLine, GpioController? controller = null, ILoggerFactory? loggerFactory = null)
        {
            logger = loggerFactory?.CreateLogger<RfidReader>();

            SpiConnectionSettings connection = new(spiBusId, spiChipSelectLine)
            {
                ClockFrequency = MfRc522.MaximumSpiClockFrequency
            };

            SpiDevice spi = SpiDevice.Create(connection);

            mfrc522 = new(spi, pinReset, controller, shouldDispose: false);

            semaphore = new(0);
            thread = new(async () =>
            {
                Data106kbpsTypeA card;
                bool isCardRead;

                while (!quit)
                {
                    await semaphore.WaitAsync();

                    while (!Enabled)
                    {
                        Thread.Sleep(pauseBetweenReading);
                    }

                    if (quit) break;

                    do
                    {
                        isCardRead = mfrc522.ListenToCardIso14443TypeA(out card, activeReadingDuration);
                        if (!isCardRead) Thread.Sleep(pauseBetweenReading);
                    }
                    while (!isCardRead && Enabled);

                    if (isCardRead)
                    {
                        string nfcId = Convert.ToHexString(card.NfcId);
                        RfidTagReadEvent?.Invoke(this, new RfidTagReadEventArgs(nfcId));
                    }
                }
            });

            thread.Start();
        }

        public bool Enabled
        {
            get
            {
                return mfrc522.Enabled;
            }
            set
            {
                mfrc522.Enabled = value;
            }
        }

        public delegate void RfidTagReadEventHandler(object sender, RfidTagReadEventArgs e);

        public event RfidTagReadEventHandler RfidTagReadEvent;

        public void ResumeReading()
        {
            semaphore.Release();
        }

        public void Dispose()
        {
            quit = true;
            ResumeReading();
            thread.Join();
            semaphore.Dispose();
            mfrc522.Dispose();
        }

    }

    public class RfidTagReadEventArgs : EventArgs
    {
        public RfidTagReadEventArgs(string nfcId)
        {
            NfcId = nfcId;
        }

        public string NfcId { get; }
    }
}