using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
//Game Specific
//using Utilities;
//using Client;
//using Shared.Model;

namespace RuntimeUnityEditor.Core.Networking
{
    public class TCPServer : MonoBehaviour
    {
        #region[Declarations]

        private System.Threading.Thread SocketThread;

        private volatile bool keepReading = false;

        public static TCPServer instance;

        private string _message = "Unknown";

        public bool isRunning = false;

        private Socket listener;

        private Socket handler;
        
        #endregion

        #region[Constructor]

        public TCPServer()
        {
            instance = this;
        }

        #endregion

        public void OnGUI()
        {
            //Display in-game notification status
            //GUI.Label(new Rect(725, 70, 350, 100), _message);
        }

        public void Start()
        {
            //Auto-start on load
            //StartTCPServer();
        }

        public void OnDisable()
        {
            StopTCPServer();
        }

        #region[TCP Server]

        private string GetIPAddress(bool loopback)
        {
            if (!loopback)
            {
                // THIS WILL FAIL IF ANY VIRTUAL NIC's EXISTS
                IPHostEntry host;
                string localIP = "";
            
                host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                    }
                }

                return localIP;
            }
            else
            {
                return IPAddress.Loopback.ToString();
            }
        }

        public void StartTCPServer()
        {
            // Required if you don't want the server slowing down the main thread.
            Application.runInBackground = true;

            SocketThread = new System.Threading.Thread(SocketConnection);
            SocketThread.IsBackground = true;
            SocketThread.Start();
            _message = "Socket Started";
            this.isRunning = true;
        }

        public void StopTCPServer()
        {
            keepReading = false;

            if (SocketThread != null)
            {
                SocketThread.Abort();
            }

            if (handler != null && handler.Connected)
            {
                handler.Disconnect(false);
                _message = "Disconnected";
                Debug.Log("Disconnected!");
            }

            this.isRunning = false;
        }

        private void SendResponse(string response)
        {
            if (!handler.Connected)
            {
                return;
            }

            try
            {
                string serverMessage = response;

                // Convert string message to byte array.                 
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage);
                    
                // Write byte array to socket Connection             
                handler.Send(serverMessageAsByteArray, serverMessageAsByteArray.Length, SocketFlags.None);

                Debug.Log("Server sent: " + response);
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        }

        private void SocketConnection()
        {
            string data = "";

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Host running the application.
            Debug.Log("Ip " + GetIPAddress(true).ToString());
            IPAddress[] ipArray = Dns.GetHostAddresses(GetIPAddress(true));
            IPEndPoint localEndPoint = new IPEndPoint(ipArray[0], 1755);

            // Create a TCP/IP socket.
            listener = new Socket(ipArray[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); //Prevents Reusing Socket Error!

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.
                while (true)
                {
                    keepReading = true;

                    if (handler == null || handler.Connected == false)
                    {
                        // Program is suspended while waiting for an incoming connection.
                        Debug.Log("Waiting for Connection");

                        handler = listener.Accept();
                        SendResponse("Connected to Unity Live Terminal\r\n");

                        _message = "Client Connected";
                        Debug.Log("Client Connected");
                    }

                    _message = "Waiting for Command";
                    data = null;

                    // An incoming connection needs to be processed.
                    while (keepReading)
                    {
                        System.Threading.Thread.Sleep(1);

                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        data = data.Replace("\r\n", "");

                        Debug.Log("Received Message: [" + data.Length.ToString() + "] " + data);

                        if (bytesRec <= 0 || data.Contains("EXIT"))
                        {
                            keepReading = false;
                            _message = "Disconnecting";
                            SendResponse("Disconnecting...\r\n");
                            handler.Disconnect(false);
                            break;
                        }
                        else if (data.Length == 0 || data.Length == 21)
                        {
                            SendResponse("> ");
                            break;
                        }
                        else if (data.IndexOf("<EOF>") > -1)
                        {
                            SendResponse("\r\n");
                            break;
                        }
                        else if (data.IndexOf("get gameconfig") > -1)
                        {
                            _message = "Listing Object<GameConfiguration>";

                            //Game Client_Game = UnityEngine.Object.FindObjectOfType<Game>();
                            //GameConfiguration config = Client_Game.GetComponent<GameConfiguration>();
                            string tmpTxt = "";
                            //tmpTxt = config.Dump();

                            Debug.Log(tmpTxt);
                            SendResponse(tmpTxt + "\r\n");

                            break;
                        }
                        else if (data.IndexOf("get funds") > -1)
                        {
                            _message = "Listing Funds";
                            //string funds = Client.Game.Model.GeneralModel.Funds.ToString();
                            //SendResponse("Funds: $" + funds + "\r\n");

                            break;
                        }
                        else if (data.IndexOf("get game") > -1)
                        {
                            _message = "Listing Object<Game>";

                            //Game Client_Game = UnityEngine.Object.FindObjectOfType<Game>();
                            string tmpTxt = "";
                            //tmpTxt = Client_Game.Dump();

                            Debug.Log(tmpTxt);
                            SendResponse(tmpTxt + "\r\n");

                            break;
                        }
                        else
                        {
                            SendResponse("\r\n");
                            break;
                        }
                    }

                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e.ToString());
            }            
        }

        #endregion

    }

    //Genric class to dump an object to string block
    public static class ObjectExtensions
    {
        /// <summary>
        /// Tries to Dump and object to string
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="writer"></param>
        public static string QuickDump(this object obj, int indentsize = 0)
        {
            string strOut = "";
            string indent = "";

            if (indentsize > 0)
            {
                for (int i = 1; i <= indentsize; i++)
                {
                    indent = indent + " ";
                }

                if (obj == null)
                {
                    return "Object is null";
                }
            }

            var props = GetObjectProperties(obj);
            foreach (var prop in props)
            {
                if (prop.Key != null)
                {
                    strOut += indent + prop.Key + ": " + prop.Value + "\r\n";
                }

            }
            return strOut;
        }

        /// <summary>
        /// Enumerates an Object and Gets all Properties and their Values
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetObjectProperties(object obj)
        {
            var props = new Dictionary<string, string>();
            if (obj == null)
                return props;

            var type = obj.GetType();
            foreach (var prop in type.GetProperties())
            {
                var val = prop.GetValue(obj, new object[] { });
                var valStr = val == null ? "" : val.ToString();
                props.Add(prop.Name, valStr);
            }

            return props;
        }
    }
}
