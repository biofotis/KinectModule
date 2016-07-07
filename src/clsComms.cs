using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace KinectModule
{

    // State object for receiving data from remote device.
    public class StateObject
    {
        public Socket workSocket = null;                    // Client socket.
        public const int BufferSize = 256;                  // Size of receive buffer.
        public byte[] buffer = new byte[BufferSize];        // Receive buffer.
        public StringBuilder sb = new StringBuilder();      // Received data string.
    }

    public class AsynchronousClient
    {
        #region Module Variables
            private const int port = 12345;                     // The port number for the remote device.
            
            // ManualResetEvent instances signal completion.
            private static ManualResetEvent connectDone = new ManualResetEvent(false);
            private static ManualResetEvent sendDone = new ManualResetEvent(false);
            private static ManualResetEvent receiveDone = new ManualResetEvent(false);

            private static String response = String.Empty;      // The response from the remote device.
            private Socket gsckClient;
            public bool bConnected;
            private KinectModule myinstance;

            // local copies of the tracked data - volatile as quick-fix for threading issues
            public volatile bool bTracked = false;
            public volatile int iSkeletons = 0;
            public volatile float[] fltHeadPose = { 0.0f, 0.0f, 0.0f };
            public volatile float[] fltHeadPosition = { 0.0f, 0.0f, 0.0f };
            public volatile float fltLookAtRobot = 0.0f;
            public volatile float fltEnergy = 0.0f;
            public volatile float fltConfidence = 0.0f;
            public volatile float fltAngle = 0.0f;
            public volatile bool bTrackingFace = false;
            public volatile int iLastFrame = 0;
        #endregion

        #region Setup and Init
            public string StartClient(IPAddress ipAddressIn, KinectModule forminst)  //, clsTransform pclsTrans)
            {
            myinstance = forminst;
                try
                {
                    // Create a TCP/IP socket.
                    IPEndPoint remoteEP = new IPEndPoint(ipAddressIn, port);
                    gsckClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Connect to the remote endpoint.
                    gsckClient.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), gsckClient);
                    System.Threading.Thread.Sleep(200);
                    connectDone.WaitOne(5000);
                
                    Send("connected");      // Send test data to the remote device.
                    sendDone.WaitOne(5000);
                
                    Receive(gsckClient);                // Receive the response from the remote device.

                    //mclsTransform = pclsTrans;

                    bConnected = true;
                    return "Connected to " + ipAddressIn.ToString() + ":" + port.ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return "Connection failed.";
                }
            }

            private void ConnectCallback(IAsyncResult ar)
            {
                try
                {
                    Socket client = (Socket)ar.AsyncState;      // Retrieve the socket from the state object.
                    client.EndConnect(ar);                      // Complete the connection.
                    Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());
                    connectDone.Set();                          // Signal that the connection has been made.
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        #endregion

        #region Receive Handling
        public void Receive(Socket client)
        {
            try
            {
                StateObject state = new StateObject();      // Create the state object.
                state.workSocket = client;

                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
             
        }

        private void ReceiveCallback(IAsyncResult ar)
            {
            
            try
                {
                    // Retrieve the state object and the client socket from the asynchronous state object.
                    StateObject state = (StateObject)ar.AsyncState;
                    Socket client = state.workSocket;

                    // Read data from the remote device.
                    int bytesRead = client.EndReceive(ar);
                

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);

                    Console.WriteLine("Received from NAO: " + state.sb.ToString());
                    receivehandler(state.sb.ToString());
                    //GenerateResponse(state.sb.ToString());
                    state.sb.Clear();
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            
        }
        #endregion

        #region Send Handling
            public void Send(String data)
            {


                data += ",END";                             // need an end delimiter as the kinect fires too quick for urbi at times
            
            Console.WriteLine("Sending: " + data);
                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(data);

                // Begin sending the data to the remote device.
                gsckClient.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), gsckClient);
            }

            private void SendCallback(IAsyncResult ar)
            {
                try
                {
                    Socket client = (Socket)ar.AsyncState;          // Retrieve the socket from the state object.

                    // Complete sending the data to the remote device.
                    int bytesSent = client.EndSend(ar);
                    Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                    // Signal that all bytes have been sent.
                    sendDone.Set();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        #endregion

        #region Application Specific Responses
        public void receivehandler(String sDataIn)
        {

            if (sDataIn.StartsWith("START"))
            {
                myinstance.addlog("Logging data for participant: " + sDataIn.Substring(6, 2));
                myinstance.kinectclass.startcapture(sDataIn.Substring(6,2)); //start video capture
               myinstance.startaudio(Convert.ToInt16( sDataIn.Substring(6,2)));
                
                
            }
            if (sDataIn.StartsWith("STOP"))
            {
                myinstance.addlog("Logs saved");
                myinstance.kinectclass.stopcapture(); //stop video
                System.Threading.Thread.Sleep(1000);
                myinstance.stopaudio();
                
            }
        }
            

           
        #endregion

        public void CloseSocket()
        {
            bConnected = false;
            gsckClient.Shutdown(SocketShutdown.Both);
            gsckClient.Close();
            gsckClient = null;
        }
    }
}