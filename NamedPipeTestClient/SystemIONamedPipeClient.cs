using System;
using System.Text;
using System.IO;
using System.IO.Pipes;

namespace NamedPipeTestClient
{
    class SystemIONamedPipeClient
    {
        public static void Run(bool sendKillSig = false)
        {
            NamedPipeClientStream pipeClient = null;

            try
            {
                RESEND:;    
                for(int c = 0; c < 1; c++)
                {
                    // ----------------------------------------------------------------------------------------------------------------
                    // Try to open the named pipe identified by the pipe name.
                    pipeClient = new NamedPipeClientStream(
                        Program.ServerName,         // The server name
                        Program.PipeName,           // The unique pipe name
                        PipeDirection.InOut,        // The pipe is duplex
                        PipeOptions.None            // No additional parameters
                        );

                    pipeClient.Connect(5000);
                    Console.WriteLine("[IPC Client Connected] - Pipe Name: \"{0}\"", Program.FullPipeName);

                    pipeClient.ReadMode = PipeTransmissionMode.Message;

                    // ----------------------------------------------------------------------------------------------------------------
                    // Send our request to server
                    string message = "";
                    if (sendKillSig)
                    {
                        message = Program.KillRequestMessage;
                    }
                    else
                    {
                        message = Program.RequestMessage;
                    }

                    byte[] bRequest = Encoding.UTF8.GetBytes(message);
                    int cbRequest = bRequest.Length;
                    pipeClient.Write(bRequest, 0, cbRequest);
                    pipeClient.WaitForPipeDrain();

                    Console.WriteLine("[IPC Client Sent {0} bytes] Message: {1}", cbRequest, message);

                    // ----------------------------------------------------------------------------------------------------------------
                    // Receive acknowledgement from server.
                    string msg = "";
                    var reader = new StreamReader(pipeClient);
                    msg = reader.ReadToEnd();

                    byte[] tmp = Encoding.UTF8.GetBytes(msg);

                    string tmpStr = Encoding.UTF8.GetString(tmp).Replace("\0", "");
                    Console.WriteLine("[IPC Client Received {0} bytes] Message: {1}", tmp.Length, tmpStr);

                    if(tmpStr != "MSG_RECEIVED") { goto RESEND; } else { goto ENDLOOP; }
                }

                ENDLOOP:;
                // ----------------------------------------------------------------------------------------------------------------
                // Close the pipe
                pipeClient.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[IPC Client ERROR] - {0}", ex.Message);
            }
            finally
            {
                // Close the pipe.
                if (pipeClient != null)
                {
                    pipeClient.Close();
                    pipeClient = null;
                }
            }
        }
    }
}