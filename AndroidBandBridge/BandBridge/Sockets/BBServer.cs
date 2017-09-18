using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Communication.Packet;
using System.Threading.Tasks;
using System.Diagnostics;
using Communication.Data;
using System.Collections.Generic;
using BandBridge.Data;
using System.Linq;
using Microsoft.Band.Portable;


namespace BandBridge.Sockets
{
    /// <summary>
    /// Class that implements BandBridge server services.
    /// </summary>
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
        /// <summary>Received response. Is instance of <see cref="Message"/>.</summary>
        private static Message receivedMessage;
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
        /// <summary>MS Band log.</summary>
        private StringBuilder msBandLog;
        /// <summary>Connected Band device.</summary>
        private BandData connectedBand;
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
        /// <summary>Informs that sensor readings changed.</summary>
        public Action BandInfoChanged { get; set; }
        /// <summary>Connected MS Band device.</summary>
        public BandData ConnectedBand { get { return connectedBand; } }
        /// <summary>Server log.</summary>
        public string ServerLog
        {
            get { return serverLog.ToString(); }
            set
            {
                if (serverLog == null) serverLog = new StringBuilder();
                serverLog.Clear();
                serverLog.Append(value);

                Debug.WriteLine(value);
                Console.WriteLine(value);
            }
        }
        /// <summary>MS Band log.</summary>
        public string MSBandLog
        {
            get { return msBandLog.ToString(); }
            set
            {
                if (msBandLog == null) msBandLog = new StringBuilder();
                msBandLog.Clear();
                msBandLog.Append(value);

                Debug.WriteLine(value);
                Console.WriteLine(value);
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
            ServerLog = hostAddress.ToString();
            servicePort = DefaultServicePort;
            dataBufferSize = 16;
            calibrationBufferSize = 200;
            maxMessageSize = MaxMessageSize;
            BandInfoChanged += () => { };
        }
        #endregion


        #region Public methods
        /// <summary>
        /// Updates server settings.
        /// </summary>
        /// <param name="servicePortText">New service port number as string</param>
        /// <param name="dataBufferSizeText">New data buffer size as string</param>
        /// <param name="calibrationBufferSizeText">New calibration buffer size as string</param>
        public void UpdateServerSettings(string servicePortText, string dataBufferSizeText, string calibrationBufferSizeText)
        {
            int _servicePort = 0;
            if(Int32.TryParse(servicePortText, out _servicePort)) servicePort = _servicePort;
            int _dataBufferSize = 0;
            if (Int32.TryParse(dataBufferSizeText, out _dataBufferSize)) dataBufferSize = _dataBufferSize;
            int _calibrationBufferSize = 0;
            if (Int32.TryParse(calibrationBufferSizeText, out _calibrationBufferSize)) calibrationBufferSize = _calibrationBufferSize;
        }
        #endregion


        #region MS Band methods
        /// <summary>
        /// Gets MS Band devices connected to local computer.
        /// </summary>
        /// <returns></returns>
        public async Task GetMSBandDevices()
        {
            // remove currently connected Band:
            if (connectedBand != null)
            {
                await connectedBand.StopReadingSensorsData();
                connectedBand = null;
            }
            MSBandLog = ">> Get MS Band devices...";
            var bandClientManager = BandClientManager.Instance;
            // query the service for paired devices
            var pairedBands = await bandClientManager.GetPairedBandsAsync();
            int pairedBandsCount = pairedBands.ToArray().Length;
            MSBandLog = String.Format("Found {0} devices", pairedBandsCount);
            if (pairedBandsCount > 0)
            {
                // connect to the first device
                var bandInfo = pairedBands.FirstOrDefault();
                var bandClient = await bandClientManager.ConnectAsync(bandInfo);

                if (bandClient != null)
                {
                    connectedBand = new BandData(bandClient, bandInfo.Name, bufferSize, calibrationBufferSize);
                    connectedBand.ReadingsChanged += () => { BandInfoChanged(); };
                    await connectedBand.StartReadingSensorsData();
                }
            }
        }
        #endregion


        #region Socket methods
        /// <summary>
        /// Stops the server.
        /// </summary>
        public async Task StopServer()
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
        public async Task StartListening()
        {
            // create buffer for incoming data:
            receiveBuffer = new byte[bufferSize];
            // create packetizer object:
            packetizer = new PacketProtocol(maxMessageSize);
            packetizer.MessageArrived += receivedMsg =>
            {
                ServerLog = String.Format("\tReceived {0} bytes from client", receivedMsg.Length);
                if (receivedMsg.Length > 0)
                {
                    receivedMessage = Message.Deserialize(receivedMsg);
                }
            };

            // establish the local endpoint for the socket:
            IPEndPoint localEndPoint = new IPEndPoint(hostAddress, servicePort);
            // create a TCP/IP socket:
            serverSocketListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerLog = "Established local EndPoint...";

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                serverSocketListener.Bind(localEndPoint);
                serverSocketListener.Listen(1);
                isServerWorking = true;

                while (isServerWorking)
                {
                    // reset signals:
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    ServerLog = "Waiting for a connection...";
                    serverSocketListener.BeginAccept(new AsyncCallback(AcceptCallback), serverSocketListener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();

                    packetizer.DEBUG_ShowDataBuffer();
                }
            }
            catch (Exception e)
            {
                ServerLog = e.ToString();
            }
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        /// <summary>
        /// Accepts incoming connection.
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                //// Signal the main thread to continue.  
                //allDone.Set();

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
        /// <summary>
        /// Reads incoming data received from remote connection.
        /// </summary>
        /// <param name="ar"></param>
        private async void ReadCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the handler socket from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);
                if (bytesRead > 0)
                {
                    Debug.WriteLine("\t-- bytes read: " + bytesRead);
                    Console.WriteLine("\t-- bytes read: " + bytesRead);

                    // pass received data and receiving process to packetizer object:
                    packetizer.DataReceived(state.buffer);

                    // wait for the rest of the message:
                    if (!packetizer.AllBytesReceived)
                    {
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                    }
                    // already received whole message:
                    else
                    {
                        // prepare response:
                        Message response = await PrepareResponseToClient(receivedMessage);
                        byte[] byteData = PacketProtocol.WrapMessage(Message.Serialize(response));

                        // send response to remote client socket:
                        Send(handler, byteData);
                    }
                }
            }
            catch (Exception e)
            {
                ServerLog = e.ToString();
            }
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        /// <summary>
        /// Sends responce to remote client.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="data"></param>
        private void Send(Socket handler, byte[] data)
        {
            try
            {
                // Begin sending the data to the remote device.  
                handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception e)
            {
                ServerLog = e.ToString();
            }
        }

        // Based on: http://msdn.microsoft.com/en-us/library/fx6588te.aspx
        /// <summary>
        /// Finalizes sending response to remote client.
        /// </summary>
        /// <param name="ar"></param>
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                ServerLog = String.Format("\tSent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                // Signal the main thread to continue.
                allDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        #endregion


        #region Private methods
        /// <summary>
        /// Prepares response to given message.
        /// </summary>
        /// <param name="message">Received message</param>
        /// <returns>Response to send</returns>
        private async Task<Message> PrepareResponseToClient(Message message)
        {
            ServerLog = String.Format("\t- Message code: {0}", message.Code);

            switch (message.Code)
            {
                // send the list of all connected Bands:
                case MessageCode.SHOW_LIST_ASK:
                    if (connectedBand != null)
                        return new Message(MessageCode.SHOW_LIST_ANS, new string[] { connectedBand.Name });
                    else
                        return new Message(MessageCode.SHOW_LIST_ANS, null);

                // send current sensors data from specific Band device:
                case MessageCode.GET_DATA_ASK:
                    if (connectedBand != null && message.Result != null && message.Result.GetType() == typeof(string))
                    {
                        if (connectedBand.Name == (string)message.Result)
                        {
                            // get current sensors data and send them back to remote client:
                            SensorData hrData = new SensorData(SensorCode.HR, connectedBand.HrBuffer.GetAverage());
                            SensorData gsrData = new SensorData(SensorCode.HR, connectedBand.GsrBuffer.GetAverage());
                            return new Message(MessageCode.GET_DATA_ANS, new SensorData[] { hrData, gsrData });
                        }
                        else
                            return new Message(MessageCode.GET_DATA_ANS, null);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // callibrate sensors data to get control average values:
                case MessageCode.CALIB_ASK:
                    if (connectedBand != null && message.Result != null && message.Result.GetType() == typeof(string))
                    {
                        if (connectedBand.Name == (string)message.Result)
                        {
                            // get current sensors data and send them back to remote client:
                            var data = await connectedBand.CalibrateSensorsData();
                            return new Message(MessageCode.CALIB_ANS, data);
                        }
                        else
                            return new Message(MessageCode.CALIB_ANS, null);
                    }
                    return new Message(MessageCode.CTR_MSG, null);

                // wrong message code:
                default:
                    return new Message(MessageCode.CTR_MSG, null);
            }
        }
        #endregion
    }
}