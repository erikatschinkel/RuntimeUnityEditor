using System;

namespace NamedPipeTestClient
{
    class Program
    {
        // The full name of the pipe in the format of \\servername\pipe\pipename.
        internal const string ServerName = ".";
        internal const string PipeName = "RUEPipe";
        internal const string FullPipeName = @"\\" + ServerName + @"\pipe\" + PipeName;
        internal const int BufferSize = 1024;

        // '\0' is appended in the end because the client may be a native C++ application that expects NULL termiated string.
        internal const string RequestMessage = "CONNECT_REQUEST\0";
        internal const string KillRequestMessage = "KILL_SERVER\0";

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "-native")
            {
                // Use P/Invoke native APIs
                NativeNamedPipeClient.Run();
            }
            else
            {
                // Use the types in System.IO.Pipes
                if (args.Length > 0 && args[0] == "-kill")
                {
                    SystemIONamedPipeClient.Run(true);
                }
                else
                {
                    SystemIONamedPipeClient.Run();
                }
            }
        }
    }
}