using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Communication.Data
{
    /// <summary>
    /// Class that represents a message structure used in communication with BandBridge server.
    /// </summary>
    [DataContract]
    public class Message
    {
        #region Static fields
        /// <summary>
        /// An array with types known by DataContractSerializer.
        /// </summary>
        public static Type[] SerializedTypesSet = { typeof(SensorData), typeof(SensorData[]) };
        #endregion

        #region Properties
        /// <summary>
        /// Message command type code.
        /// </summary>
        [DataMember]
        public MessageCode Code { get; set; }

        /// <summary>
        /// Message result object.
        /// </summary>
        [DataMember]
        public object Result { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new instance of class <see cref="Message"/>.
        /// </summary>
        /// <param name="code">Message command type code</param>
        /// <param name="result">Message result object</param>
        public Message(MessageCode code, object result)
        {
            Code = code;
            Result = result;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Serializes <see cref="Message"/> object to a byte array.
        /// </summary>
        /// <param name="message">Message to serialize</param>
        /// <returns>Serialized message in form of an array of bytes</returns>
        public static byte[] Serialize(Message message)
        {
            byte[] data = null;
            using (MemoryStream stream = new MemoryStream())
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(Message), SerializedTypesSet);
                serializer.WriteObject(writer, message);
                writer.Flush();
                data = stream.ToArray();
            }
            return data;
        }

        /// <summary>
        /// Deserializes byte array as <see cref="Message"/> object.
        /// </summary>
        /// <param name="data">Array of bytes with serialized message</param>
        /// <returns>Deserialized message</returns>
        public static Message Deserialize(byte[] data)
        {
            Message message = null;
            using (MemoryStream stream = new MemoryStream(data))
            using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                DataContractSerializer deserializer = new DataContractSerializer(typeof(Message), SerializedTypesSet);
                message = (Message)deserializer.ReadObject(reader);
            }
            return message;
        }

        /// <summary>
        /// Writes Message object in form: 'Message: [Code][Result] -> Result.ToString()'.
        /// </summary>
        /// <returns>String version of the <see cref="Message"/> object</returns>
        public override string ToString()
        {
            if (Result != null)
                return string.Format("Message: [{0}][{1}] -> {2}", Code, Result, Result.ToString());
            else
                return string.Format("Message: [{0}][{1}]", Code, Result);
        }
        #endregion
    }
}