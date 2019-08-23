using System;
using System.Text;
using System.IO;
using System.IO.Pipes;

namespace RuntimeUnityEditor.Core.Networking.IPCServer
{
    public class IPCKillClient
    {
        internal const string ServerName = ".";
        internal const string PipeName = "RUEPipe";
        internal const string FullPipeName = @"\\" + ServerName + @"\pipe\" + PipeName;
        internal const int BufferSize = 1024;
        internal const string KillRequestMessage = "KILL_SERVER\0";

        public static void SendKillRequest()
        {
            NamedPipeClientStream pipeClient = null;

            try
            {
                pipeClient = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.None);
                pipeClient.Connect(1000);
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                byte[] bRequest = Encoding.UTF8.GetBytes(KillRequestMessage);
                int cbRequest = bRequest.Length;
                pipeClient.Write(bRequest, 0, cbRequest);
                pipeClient.WaitForPipeDrain();

                string msg = "";
                var reader = new StreamReader(pipeClient);
                msg = reader.ReadToEnd();

                pipeClient.Close();
            }
            catch
            {
                if (pipeClient != null)
                {
                    pipeClient.Close();
                    pipeClient = null;
                }
            }
        }
    }
}
