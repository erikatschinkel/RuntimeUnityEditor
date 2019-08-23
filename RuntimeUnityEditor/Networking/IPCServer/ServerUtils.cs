using System;
using UnityEngine;

namespace RuntimeUnityEditor.Core.Networking.IPCServer
{
    public class ServerUtils : MonoBehaviour
    {
        internal const string ServerName = ".";
        internal const string PipeName = "RUEPipe";
        internal const string FullPipeName = @"\\" + ServerName + @"\pipe\" + PipeName;
        internal const int BufferSize = 1024;

        // Default Respone from Server. '\0' is appended in the end because the client may be a native C++ application that expects NULL termiated string. 
        internal const string ResponseMessage = "MSG_RECEIVED\0";

        public static System.Threading.Thread IPCThread;
        public static bool isRunning = false;
        public ServerUtils instance;

        public ServerUtils()
        {
            this.instance = this;
        }

        public static void StartServer()
        {
            Application.runInBackground = true;

            if (!ServerUtils.isRunning)
            {
                IPCThread = new System.Threading.Thread(NativeNamedPipeServer.Run);
                IPCThread.IsBackground = true;
                IPCThread.Start();
                ServerUtils.isRunning = true;
            }
        }

        public void OnDisable()
        {
            Console.WriteLine("[IPC Server Shutting Down]");
            IPCKillClient.SendKillRequest();
            IPCThread.Interrupt();
            if (!IPCThread.Join(200)) { IPCThread.Abort(); }
            ServerUtils.isRunning = false;
        }

        public static void StopServer()
        {
            Console.WriteLine("[IPC Server Shutting Down]");
            IPCKillClient.SendKillRequest();
            IPCThread.Interrupt();
            if (!IPCThread.Join(200)) { IPCThread.Abort(); }
            ServerUtils.isRunning = false;
        }
    }
}
