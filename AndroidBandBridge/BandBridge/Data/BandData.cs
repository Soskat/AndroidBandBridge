using Communication.Data;
using Microsoft.Band.Portable;
using Microsoft.Band.Portable.Sensors;
using System;
using System.Diagnostics;
using System.Threading.Tasks;


namespace BandBridge.Data
{
    /// <summary>
    /// Represents MS Band data used in Unity game project.
    /// </summary>
    public class BandData
    {
        #region Fields
        /// <summary>Connected MS Band device.</summary>
        private BandClient bandClient;
        /// <summary>MS Band device name.</summary>
        private string name;
        /// <summary>Last Heart Rate sensor reading.</summary>
        private int hrReading;
        /// <summary>Last GSR sensor reading.</summary>
        private int gsrReading;
        /// <summary>Size of the buffer for incoming sensors readings.</summary>
        private int bufferSize;
        /// <summary>Size of the buffer for calibration sensors readings.</summary>
        private int calibrationBufferSize;
        /// <summary>Storage for Heart Rate sensor values.</summary>
        private CircularBuffer hrBuffer;
        /// <summary>Storage for GSR sensor values.</summary>
        private CircularBuffer gsrBuffer;
        #endregion


        #region Properties
        /// <summary>Connected MS Band device.</summary>
        public BandClient BandClient
        {
            get { return bandClient; }
            set { bandClient = value; }
        }
        /// <summary>MS Band device name.</summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>Last Heart Rate sensor reading.</summary>
        public int HrReading
        {
            get { return hrReading; }
            set { hrReading = value; }
        }
        /// <summary>Last GSR sensor reading.</summary>
        public int GsrReading
        {
            get { return gsrReading; }
            set { gsrReading = value; }
        }
        /// <summary>Storage for Heart Rate sensor values.</summary>
        public CircularBuffer HrBuffer
        {
            get { return hrBuffer; }
            set { hrBuffer = value; }
        }
        /// <summary>Storage for GSR sensor values.</summary>
        public CircularBuffer GsrBuffer
        {
            get { return gsrBuffer; }
            set { gsrBuffer = value; }
        }
        /// <summary>Informs that sensor readings changed.</summary>
        public Action ReadingsChanged { get; set; }
        #endregion


        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="BandData"/>.
        /// </summary>
        /// <param name="bandClient"><see cref="BandClient"/> object connected with Band device</param>
        /// <param name="bandName">Band device name</param>
        /// <param name="bufferSize">Size of the buffer for incoming sensors readings</param>
        /// <param name="calibrationBufferSize">Size of the buffer for calibration sensors readings</param>
        public BandData(BandClient bandClient, string bandName, int bufferSize, int calibrationBufferSize)
        {
            BandClient = bandClient;
            Name = bandName;
            this.bufferSize = bufferSize;
            this.calibrationBufferSize = calibrationBufferSize;
            HrBuffer = new CircularBuffer(this.bufferSize);
            GsrBuffer = new CircularBuffer(this.bufferSize);
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Gets Hear Rate sensor values from connected Band device.
        /// </summary>
        /// <returns></returns>
        private async Task StartHrReading()
        {
            // get the heart rate sensor
            var heartRate = bandClient.SensorManager.HeartRate;
            // add a handler
            heartRate.ReadingChanged += (o, args) =>
            {
                HrBuffer.Add(args.SensorReading.HeartRate);
                HrReading = args.SensorReading.HeartRate;
                // inform that hr reading changed:
                ReadingsChanged();
            };

            // check current user heart rate consent; if user hasn’t consented, request consent:
            if (heartRate.UserConsented == UserConsent.Unspecified)
            {
                bool granted = await heartRate.RequestUserConsent();
            }
            if (heartRate.UserConsented == UserConsent.Granted)
            {
                // start reading, with the interval
                await heartRate.StartReadingsAsync(BandSensorSampleRate.Ms16);
            }
        }
        
        /// <summary>
        /// Gets Galvenic Skin Response sensor values from connected Band device.
        /// </summary>
        /// <returns></returns>
        private async Task StartGsrReading()
        {
            // get the gsr sensor
            var gsr = bandClient.SensorManager.Gsr;
            // add a handler
            gsr.ReadingChanged += (o, args) => {
                GsrBuffer.Add((int)args.SensorReading.Resistance);
                GsrReading = (int)args.SensorReading.Resistance;
                // inform that gsr reading changed:
                ReadingsChanged();
            };
            // start reading, with the interval
            try
            {
                await gsr.StartReadingsAsync(BandSensorSampleRate.Ms16);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops reading from connected Band's Heart Rate sensor.
        /// </summary>
        /// <returns></returns>
        private async Task StopHrReading()
        {
            try
            {
                // stop the HR sensor:
                await bandClient.SensorManager.HeartRate.StopReadingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Stops reading from connected Band's Galvanic Skin Response sensor.
        /// </summary>
        /// <returns></returns>
        private async Task StopGsrReading()
        {
            try
            {
                // stop the GSR sensor:
                await bandClient.SensorManager.Gsr.StopReadingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion


        #region Public methods
        /// <summary>
        /// Starts reading data from connected Band's sensors.
        /// </summary>
        /// <returns></returns>
        public async Task StartReadingSensorsData()
        {
            await StartHrReading();
            await StartGsrReading();
            Debug.WriteLine(name + ": Started reading data...");
        }

        /// <summary>
        /// Stops reading data from connected Band's sensors.
        /// </summary>
        /// <returns></returns>
        public async Task StopReadingSensorsData()
        {
            await StopHrReading();
            await StopGsrReading();
            Debug.WriteLine(name + ": Stopped reading data...");
        }
        #endregion




        public async Task<SensorData[]> CalibrateSensorsData()
        {
            return new SensorData[] { new SensorData(SensorCode.HR, hrReading), new SensorData(SensorCode.GSR, gsrReading) };
        }
    }
}
