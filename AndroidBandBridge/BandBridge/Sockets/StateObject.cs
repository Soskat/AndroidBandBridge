using System.Net.Sockets;

namespace BandBridge.Sockets
{
    // Original source: https://msdn.microsoft.com/en-us/library/bew39x2a(v=vs.110).aspx

    /// <summary>
    /// State object for receiving data from remote device.
    /// </summary>
    public class StateObject
    {
        /// <summary>
        /// Client socket.
        /// </summary>
        public Socket workSocket = null;

        /// <summary>
        /// Size of receive buffer.
        /// </summary>
        public const int BufferSize = 256;

        /// <summary>
        /// Receive buffer.
        /// </summary>
        public byte[] buffer = new byte[BufferSize];
    }
}