using System;
using System.Linq;
using System.Text;
using Android.OS;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Communication.Packet;
using Communication.Data;


namespace BandBridge.Sockets
{
    public class BBServer
    {
        #region Constants
        /// <summary>Service port default number.</summary>
        public const int DefaultServicePort = 2055;
        /// <summary>Maximum size of one message packet.</summary>
        public const int MaxMessageSize = 2048;
        #endregion


        #region Static fields
        /// <summary>ManualResetEvent instances signal completion for completing all actions.</summary>
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        /// <summary><see cref="PacketProtocol"/> object.</summary>
        private static PacketProtocol packetizer = null;
        /// <summary>Received response. Is instance of <see cref="Communication.Data.Message"/>.</summary>
        private static Communication.Data.Message receivedResponse;
        #endregion


        #region Private fields
        /// <summary>Local host IP address.</summary>
        private IPAddress hostAddress;
        /// <summary>Service port number.</summary>
        private int servicePort;
        /// <summary>Size of data buffer.</summary>
        private int dataBufferSize;
        /// <summary>Size of calibration data buffer.</summary>
        private int calibrationBufferSize;
        /// <summary>Received message.</summary>
        private Communication.Data.Message message;
        /// <summary>Message buffer size.</summary>
        private const int bufferSize = 256;
        /// <summary>Max message size.</summary>
        private int maxMessageSize;
        /// <summary>Buffer for incoming data.</summary>
        private byte[] receiveBuffer;
        /// <summary>Is server working?</summary>
        private bool isServerWorking;
        /// <summary>Server <see cref="Socket"/> object.</summary>
        private Socket serverSocketListener;
        /// <summary>Server log.</summary>
        private StringBuilder serverLog;
        #endregion


        #region Public properties
        /// <summary> Local host IP address.</summary>
        public IPAddress HostAddress { get { return hostAddress; } }
        /// <summary>Service port number.</summary>
        public int ServicePort { get { return servicePort; } }
        /// <summary>Size of data buffer.</summary>
        public int DataBufferSize { get { return dataBufferSize; } }
        /// <summary>Size of calibration data buffer.</summary>
        public int CalibrationBufferSize { get { return calibrationBufferSize; } }
        /// <summary>Server log.</summary>
        public string ServerLog
        {
            get { return serverLog.ToString(); }
            set
            {
                if (serverLog == null) serverLog = new StringBuilder();
                serverLog.Clear();
                serverLog.Append(value);
            }
        }
        #endregion


        #region Constructors
        /// <summary>
        /// Creates an instance of class <see cref="BBServer"/>.
        /// </summary>
        public BBServer()
        {
            hostAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            servicePort = DefaultServicePort;
            dataBufferSize = 16;
            calibrationBufferSize = 200;
            maxMessageSize = MaxMessageSize;


            // create packetizer object:
            packetizer = new PacketProtocol(maxMessageSize);
            packetizer.MessageArrived += receivedMsg =>
            {
                if (receivedMsg.Length > 0)
                {
                    receivedResponse = Communication.Data.Message.Deserialize(receivedMsg);
                    ServerLog = "Received: " + receivedResponse;
                    allDone.Set();
                }
            };
        }
        #endregion


        #region Public methods
        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            try
            {
                // explicitly close the serverSocketListener:
                if (serverSocketListener != null)
                {
                    serverSocketListener.Close();
                    serverSocketListener = null;
                    isServerWorking = false;
                }
            }
            catch (Exception e)
            {
                ServerLog = ">> Exception in StopServer(): " + e.Message;
            }
        }

        /// <summary>
        /// Starts listening for incoming connections.
        /// Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        /// </summary>
        public void StartListening()
        {
            // create buffer for incoming data:
            receiveBuffer = new byte[bufferSize];

            // establish the local endpoint for the socket:
            IPEndPoint localEndPoint = new IPEndPoint(hostAddress, servicePort);
            // create a TCP/IP socket:
            serverSocketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                serverSocketListener.Bind(localEndPoint);
                serverSocketListener.Listen(100);

                while (isServerWorking)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    ServerLog = "Waiting for a connection...";
                    serverSocketListener.BeginAccept(new AsyncCallback(AcceptCallback), serverSocketListener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                ServerLog = e.ToString();
            }
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.  
                allDone.Set();

                // Get the socket that handles the client request.  
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = handler;
                // Begin receiving the data from the remote device:
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                ServerLog = e.ToString();
            }
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                String content = String.Empty;

                // Retrieve the state object and the handler socket from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // pass received data and receiving process to packetizer object:
                    packetizer.DataReceived(state.buffer);
                }




                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read more data.  
                    content = state.sb.ToString();
                    if (content.IndexOf("<EOF>") > -1)
                    {
                        // All the data has been read from the client. Display it on the console.  
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                        // Echo the data back to the client.  
                        Send(handler, content);
                    }
                    else
                    {
                        // Not all data received. Get more.  
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch (Exception e)
            {
                ServerLog = e.ToString();
            }
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        private void Send(Socket handler, string content)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        #endregion
    }
}