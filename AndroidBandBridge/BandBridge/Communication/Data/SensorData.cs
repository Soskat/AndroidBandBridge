using System.Runtime.Serialization;

namespace Communication.Data
{
    /// <summary>
    /// Class that encapsulates specific value of Microsoft Band sensor reading.
    /// </summary>
    [DataContract]
    public class SensorData
    {
        #region Properties
        /// <summary>
        /// Microsoft Band sensor type code.
        /// </summary>
        [DataMember]
        public SensorCode Code { get; set; }

        /// <summary>
        /// Value of sensor reading.
        /// </summary>
        [DataMember]
        public int Data { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="SensorData"/>.
        /// </summary>
        /// <param name="code">Microsoft Band sensor type code</param>
        /// <param name="data">Value of sensor reading</param>
        public SensorData(SensorCode code, int data)
        {
            Code = code;
            Data = data;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Writes SensorData object in form: '[Code][Data]'.
        /// </summary>
        /// <returns>String version of the <see cref="SensorData"/> object</returns>
        public override string ToString()
        {
            return string.Format("[{0}][{1}]", Code, Data);
        }
        #endregion
    }
}