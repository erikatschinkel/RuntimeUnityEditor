using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UObject = UnityEngine.Object;

namespace RuntimeUnityEditor.Core.Networking.TCPServer
{
    public class TelnetServer : MonoBehaviour
    {
        #region[Declarations]

        private System.Threading.Thread SocketThread;

        private volatile bool keepReading = false;

        public static TelnetServer instance;

        public string _message = "Telnet Server Status: Unknown";

        private Text statusText;

        public bool isRunning = false;

        private Socket listener;

        private Socket handler;

        private CommandProcessor cmdProcessor = new CommandProcessor();
        
        #endregion

        #region[Constructor]

        public TelnetServer()
        {
            instance = this;
        }

        #endregion

        #region[Unity Workflow]

        public void Start()
        {
            if (!isRunning)
            {
                _message = "Opening Telnet Server Socket";

                // Required if you don't want the server slowing down the main thread.
                Application.runInBackground = true;

                // Turn Off Stacktraces: Get rid of Unity log spam bug (i.e. (Filename: C:\buildslave\unity\build\Runtime/Export/Debug.bindings.h Line: XX) showing after every Debug.Write() )
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

                StartTCPServer();
            }
            else
            {
                _message = "Telnet Server Already Running!";
                Console.WriteLine("[Telnet Server Already Running!]");
            }
        }

        public void OnGUI()
        {
            //if (RuntimeUnityEditorCore.Instance.TelnetState) { _message = "Telnet Server Running"; } else { _message = "Telnet Server NOT Running!"; }
            
            // Display in-game notification status
            //GUI.color = Color.red;
            //GUI.Label(new Rect(725, 40, 350, 100), _message);
        }

        public void Update()
        {
            //_message = "Telnet Server Running: " + RuntimeUnityEditorCore.Instance.TelnetState.ToString();
        }

        public void OnDisable()
        {
            StopTCPServer();
        }

        #endregion

        #region[Telnet Server]

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
            SocketThread = new System.Threading.Thread(SocketConnection);
            SocketThread.IsBackground = true;
            SocketThread.Start();

            _message = "Telnet Server Socket Started";
            Console.WriteLine("[Telnet Server Socket Started]");

            isRunning = true;
        }

        public void StopTCPServer()
        {
            keepReading = false;

            if (SocketThread != null)
            {
                _message = "Telnet Server Shutdown";
                Console.WriteLine("[Telnet Server Shutdown]");
                SocketThread.Abort();
            }

            if (handler != null && handler.Connected)
            {
                handler.Disconnect(false);
                _message = "Telnet Client Disconnected";
                Console.WriteLine("[Telnet Client Disconnected]");
            }

            // Reenable Stacktraces: Get rid of Unity log spam bug (i.e. (Filename: C:\buildslave\unity\build\Runtime/Export/Debug.bindings.h Line: XX) showing after every Debug.Write() )
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.Full);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);

            isRunning = false;
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

                Console.WriteLine("[Telnet Server Sent]\r\n   Bytes: " + response.Length.ToString() + "\r\n    Data: " + response);
            }
            catch (SocketException socketException)
            {
                Console.WriteLine("[Telnet Server Socket Error] - Exception:\r\n    " + socketException);
            }
        }

        private void SocketConnection()
        {
            string data = "";

            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Host running the application.
            Console.WriteLine("[Telnet Server IP] - " + GetIPAddress(true).ToString() + ":1755");

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
                        Console.WriteLine("[Telnet Server Waiting for Connection]");

                        handler = listener.Accept();
                        SendResponse("Connected to Unity Live Terminal\r\n");

                        _message = "Client Connected";
                        Console.WriteLine("[Telnet Client Connected]");
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

                        Console.WriteLine("[Telnet Server Received Message]\r\n    Bytes: " + data.Length.ToString() + "\r\n    Data: " + data);

                        #region[ PROCESS COMMANDS HERE ]

                        if (bytesRec <= 0 || data.Contains("EXIT"))
                        {
                            keepReading = false;
                            _message = "Disconnecting";
                            SendResponse("Disconnecting...\r\n");
                            handler.Disconnect(false);
                            break;
                        } // Disconnect
                        else if (data.Length == 0 || data.Length == 21)
                        {
                            SendResponse("> ");
                            break;
                        } // User just hit 'Enter'/'Return'
                        else if (data.IndexOf("<EOF>") > -1)
                        {
                            SendResponse("\r\n");
                            break;
                        } // End of multi-line entry

                        //Predefined Mono InteracteBase Functions
                        else if (data.IndexOf(":Describe") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // Describe
                        else if (data.IndexOf(":Print") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // Print
                        else if (data.IndexOf(":ShowUsing") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // ShowUsings
                        else if (data.IndexOf(":ShowVars") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // ShowVars
                        else if (data.IndexOf(":LoadAssembly") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // LoadAssembly
                        else if (data.IndexOf(":LoadPackage") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // LoadPackage
                        else if (data.IndexOf(":Time") > -1)
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // Time
                        else if (data.IndexOf(":help") > -1) 
                        {
                            SendResponse(ProcessCommand(data));
                            break;
                        } // Help
                        
                        //Addtional Functions
                        else if (data.IndexOf(":find<") > -1)
                        {                            
                            SendResponse(ProcessCommand(data));
                            break;
                        } // Find<>

                        else // Do nothing
                        {
                            SendResponse("\r\n");
                            break;
                        }

                        #endregion
                    }

                    System.Threading.Thread.Sleep(1);
                }
            }
            catch (Exception e)
            {
                if (e.GetType().ToString() != "System.Threading.ThreadAbortException")
                {
                    Console.WriteLine("[Telnet Server Error] - Exception:\r\n    " + e.Message);
                }
            }
        }

        private string ProcessCommand(string instruction)
        {
            string response = "";
            response = cmdProcessor.ProcessCommand(instruction);
            response = response.Replace("\n", "\r\n");
            if (response.Length <= 0) { response = "Game returned empty response!\r\n"; }
            return response;
        }

        #endregion

    }

    #region[Extensions]

    // Quick and dirty class to dump a simple object to string block
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

    #endregion

}

    /* TESTING ! TESTING ! TESTING ! TESTING ! TESTING ! TESTING ! TESTING ! TESTING ! TESTING ! TESTING !
    
    // ref: https://gamedev.stackexchange.com/questions/141088/how-can-i-look-up-an-object-given-only-the-name-of-its-type?rq=1
    // Not really what I was hoping for so trying several different methods
    
    // References to the Unity Engine types need an assembly qualified name,
    // so we cache that here. Repeat for any 3rd-party assemblies you use.
    static readonly string engineAssemblyName = System.Reflection.Assembly.GetAssembly(typeof(GameObject)).FullName;

    void InvokeAll(string componentName, string methodName, System.Object[] arguments)
    {
        // We'll search for a type matching the given component name.
        System.Type type;

        // First, check our own CSharp assembly (no extra qualification needed).
        type = System.Type.GetType(componentName);

        // If not found there, then check the UnityEngine assembly.
        if (type == null)
        {
            string qualifiedName = string.Format("UnityEngine.{0}",
                System.Reflection.Assembly.CreateQualifiedName
                (engineAssemblyName, componentName));
            type = System.Type.GetType(qualifiedName);
        }

        if (type == null)
        {
            Debug.LogErrorFormat(
                  "Could not find type {0} in Assembly-CSharp or UnityEngine.",
                  componentName);
            return;
        }

        // We've found a valid type.
        // Use the Unity method to retrieve all active instances in the scene.
        var components = FindObjectsOfType(type);

        // Note: this currently works only for methods with a single definition.
        // More information is needed to disambiguate which method you want when
        // it has multiple overloads (same name with different signatures).
        var method = type.GetMethod(methodName);

        // If you need to access private/protected methods too,
        // use this version that peeks into non-public areas...
        //var method = type.GetMethod(methodName, 
        //      System.Reflection.BindingFlags.Public 
        //    | System.Reflection.BindingFlags.NonPublic 
        //    | System.Reflection.BindingFlags.Instance);

        // You could also search through the array to select only 
        // certain instances to invoke...
        foreach (var component in components)
        {
            method.Invoke(component, arguments);
        }
    }
*/