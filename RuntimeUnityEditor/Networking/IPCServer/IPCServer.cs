using System;
using UnityEngine;

namespace RuntimeUnityEditor.Core.Networking.IPCServer
{
    public class IPCServer : MonoBehaviour
    {
        #region[Declarations]

        internal const string ServerName = ".";
        internal const string PipeName = "RUEPipe";
        internal const string FullPipeName = @"\\" + ServerName + @"\pipe\" + PipeName;
        internal const int BufferSize = 1024;

        // Default Respone from Server. '\0' is appended in the end because the client may be a native C++ application that expects NULL termiated string. 
        internal const string ResponseMessage = "MSG_RECEIVED\0";

        public static System.Threading.Thread IPCThread;
        public bool isRunning = false;
        public static IPCServer instance;

        #endregion

        #region[Constructor]

        public IPCServer()
        {
            instance = this;
        }

        #endregion

        #region[Unity Workflow]

        public void Start() { }

        public void OnGUI()
        {

        }

        public void Update() { }

        public void OnDisable() { }

        #endregion

        public void StartServer()
        {
            Application.runInBackground = true;

            if (!this.isRunning)
            {
                IPCThread = new System.Threading.Thread(NativeNamedPipeServer.Run);
                IPCThread.IsBackground = true;
                IPCThread.Start();
                this.isRunning = true;
            }
        }

        public void StopServer()
        {
            Console.WriteLine("[IPC Server Shutting Down]");
            IPCKillClient.SendKillRequest();
            IPCThread.Interrupt();
            if (!IPCThread.Join(200)) { IPCThread.Abort(); }
            this.isRunning = false;
        }
    }
}
