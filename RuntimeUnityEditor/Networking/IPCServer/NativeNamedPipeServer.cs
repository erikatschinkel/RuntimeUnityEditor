using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.ComponentModel;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;

namespace RuntimeUnityEditor.Core.Networking.IPCServer
{
    class NativeNamedPipeServer
    {
        private static bool KillServerRequested = false;
        public static SafePipeHandle pipeHandle;

        /// <summary> 
        /// P/Invoke native APIs related to named pipe operations to create the named pipe. 
        /// System.IO.Pipes in Unity is missing crucial types/methods for performing reliable and
        /// accurate transfers. Such as bytesSents/bytesReceived.
        /// </summary> 
        public static void Run()
        {
            while (!KillServerRequested)
            {
                // ----------------------------------------------------------------------------------------------------------------
                // Create the named pipe. 
                using (SafePipeHandle hNamedPipe = NativeMethod.CreateNamedPipe(
                    ServerUtils.FullPipeName,           // The unique pipe name. 
                    PipeOpenMode.PIPE_ACCESS_DUPLEX,    // The pipe is duplex 
                    PipeMode.PIPE_TYPE_MESSAGE |        // Message type pipe  
                    PipeMode.PIPE_READMODE_MESSAGE |    // Message-read mode  
                    PipeMode.PIPE_WAIT,                 // Blocking mode is on 
                    2,                                  // Max server instances 
                    ServerUtils.BufferSize,             // Output buffer size 
                    ServerUtils.BufferSize,             // Input buffer size 
                    NMPWAIT_USE_DEFAULT_WAIT            // Time-out interval
                    ))
                {
                    try
                    {
                        if (hNamedPipe.IsInvalid)
                        {
                            throw new Win32Exception();
                        }

                        pipeHandle = hNamedPipe;
                        Console.WriteLine("[IPC Server Waiting for Connection] - \"{0}\"", ServerUtils.FullPipeName);

                        // ----------------------------------------------------------------------------------------------------------------
                        // Wait for the connections. Runs on background thread.
                        if (!NativeMethod.ConnectNamedPipe(hNamedPipe, IntPtr.Zero))
                        {
                            if (Marshal.GetLastWin32Error() != ERROR_PIPE_CONNECTED)
                            {
                                throw new Win32Exception();
                            }
                        }
                        Console.WriteLine("[IPC Server Status] - Client Connected");

                        // ---------------------------------------------------------------------------------------------------------------- 
                        // Received a request from client.
                        string message;
                        bool finishRead = false;
                        do
                        {
                            byte[] bRequest = new byte[ServerUtils.BufferSize];
                            int cbRequest = bRequest.Length, cbRead;

                            finishRead = NativeMethod.ReadFile(
                                hNamedPipe,             // Handle of the pipe 
                                bRequest,               // Buffer to receive data 
                                cbRequest,              // Size of buffer in bytes 
                                out cbRead,             // Number of bytes read  
                                IntPtr.Zero             // Not overlapped  
                                );

                            if (!finishRead && Marshal.GetLastWin32Error() != ERROR_MORE_DATA)
                            {
                                throw new Win32Exception();
                            }

                            // UTF8-encode the received byte array and trim all the '\0' characters at the end. 
                            message = Encoding.UTF8.GetString(bRequest).Replace("\0", "");
                            Console.WriteLine("[IPC Server Received {0} bytes] Message: {1}\r\n", cbRead, message);
                        }
                        while (!finishRead); // Repeat loop if ERROR_MORE_DATA 

                        // If message is not KILL_SERVER, then process client request
                        if (message != "KILL_SERVER")
                        {
                            //Get our message header and data
                            string[] msgArray = message.Split(new string[] { "|:|" }, StringSplitOptions.None);
                            string header = msgArray[0];
                            string data = msgArray[1];

                            // Process Client Requests Here based off request header
                            Console.WriteLine("    Message Header: " + header);
                            Order order = JsonConvert.DeserializeObject<Order>(data);
                            Console.WriteLine("    Message Data: {0} ordered {1} {2}, delivery address: {3}\r\n", order.CustomerName, order.Quantity, order.ProductName, order.Address);
                        }
                    
                        // ----------------------------------------------------------------------------------------------------------------
                        // Send a message received response to client. 
                        string rmessage = ServerUtils.ResponseMessage;
                        byte[] bResponse = Encoding.UTF8.GetBytes(rmessage);
                        int cbResponse = bResponse.Length, cbWritten;

                        if (!NativeMethod.WriteFile(
                            hNamedPipe,                 // Handle of the pipe 
                            bResponse,                  // Message to be written 
                            cbResponse,                 // Number of bytes to write 
                            out cbWritten,              // Number of bytes written 
                            IntPtr.Zero                 // Not overlapped 
                            ))
                        {
                            throw new Win32Exception();
                        }

                        Console.WriteLine("[IPC Server Sent {0} bytes] Message: {1}", cbWritten, rmessage.Replace("\0", ""));

                        if (message == "KILL_SERVER") { KillServerRequested = true; }

                        // ----------------------------------------------------------------------------------------------------------------
                        // Flush the pipe to allow the client to read the pipe's contents before disconnecting. Then disconnect the client's connection. 
                        NativeMethod.FlushFileBuffers(hNamedPipe);
                        NativeMethod.DisconnectNamedPipe(hNamedPipe);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message != "Thread was being aborted")
                        {
                            Console.WriteLine("[IPC Server ERROR] - {0}", ex.Message);
                        }

                        hNamedPipe.Close();
                        hNamedPipe.Dispose();
                        NativeMethod.DisconnectNamedPipe(hNamedPipe);
                    }
                    finally
                    {
                        if (hNamedPipe != null)
                        {
                            hNamedPipe.Close();
                            hNamedPipe.Dispose();
                            NativeMethod.DisconnectNamedPipe(hNamedPipe);
                        }
                    }
                }
            }

            if (KillServerRequested) { ServerUtils.StopServer(); }
        }


        /// <summary> 
        /// CreateNativePipeSecurity creates and initializes a new SECURITY_ATTRIBUTES object to allow Authenticated Users read and  
        /// write access to a pipe, and to allow the Administrators group full access to the pipe. Windows only.
        /// </summary> 
        /// <returns> 
        /// A SECURITY_ATTRIBUTES object that allows Authenticated Users read and write access to a pipe, and allows the Administrators group full access to the pipe. 
        /// </returns>
        static SECURITY_ATTRIBUTES CreateNativePipeSecurity()
        {
            // Define the SDDL for the security descriptor. 
            string sddl = "D:" +        // Discretionary ACL 
                "(A;OICI;GRGW;;;AU)" +  // Allow read/write to authenticated users 
                "(A;OICI;GA;;;BA)";     // Allow full control to administrators 

            SafeLocalMemHandle pSecurityDescriptor = null;
            if (!NativeMethod.ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, 1, out pSecurityDescriptor, IntPtr.Zero))
            {
                throw new Win32Exception();
            }

            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.lpSecurityDescriptor = pSecurityDescriptor;
            sa.bInheritHandle = false;
            return sa;
        }

        #region Native API Signatures and Types 

        #region[Pipe Options]

        /// <summary> 
        /// Desired Access of File/Device 
        /// </summary> 
        [Flags]
        internal enum FileDesiredAccess : uint
        {
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000
        }

        /// <summary> 
        /// File share mode 
        /// </summary> 
        [Flags]
        internal enum FileShareMode : uint
        {
            Zero = 0x00000000,                  // No sharing. 
            FILE_SHARE_DELETE = 0x00000004,
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002
        }

        /// <summary> 
        /// File Creation Disposition 
        /// </summary> 
        internal enum FileCreationDisposition : uint
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        /// <summary> 
        /// Named Pipe Open Modes
        /// </summary> 
        [Flags]
        internal enum PipeOpenMode : uint
        {
            PIPE_ACCESS_INBOUND = 0x00000001,   // Inbound pipe access. 
            PIPE_ACCESS_OUTBOUND = 0x00000002,  // Outbound pipe access. 
            PIPE_ACCESS_DUPLEX = 0x00000003     // Duplex pipe access. 
        }

        /// <summary> 
        /// Named Pipe Type, Read, and Wait Modes
        /// </summary> 
        internal enum PipeMode : uint
        {
            // Type Mode 
            PIPE_TYPE_BYTE = 0x00000000,        // Byte pipe type. 
            PIPE_TYPE_MESSAGE = 0x00000004,     // Message pipe type. 

            // Read Mode 
            PIPE_READMODE_BYTE = 0x00000000,    // Read mode of type Byte. 
            PIPE_READMODE_MESSAGE = 0x00000002, // Read mode of type Message. 

            // Wait Mode 
            PIPE_WAIT = 0x00000000,             // Pipe blocking mode. 
            PIPE_NOWAIT = 0x00000001            // Pipe non-blocking mode. 
        }
        
        /// <summary> 
        /// Unlimited server pipe instances. 
        /// </summary> 
        internal const int PIPE_UNLIMITED_INSTANCES = 255;

        /// <summary> 
        /// The operation completed successfully. 
        /// </summary> 
        internal const int ERROR_SUCCESS = 0;

        /// <summary> 
        /// The system cannot find the file specified. 
        /// </summary> 
        internal const int ERROR_CANNOT_CONNECT_TO_PIPE = 2;

        /// <summary> 
        /// All pipe instances are busy. 
        /// </summary> 
        internal const int ERROR_PIPE_BUSY = 231;

        /// <summary> 
        /// The pipe is being closed. 
        /// </summary> 
        internal const int ERROR_NO_DATA = 232;

        /// <summary> 
        /// No process is on the other end of the pipe. 
        /// </summary> 
        internal const int ERROR_PIPE_NOT_CONNECTED = 233;

        /// <summary> 
        /// More data is available. 
        /// </summary> 
        public const int ERROR_MORE_DATA = 234;

        /// <summary> 
        /// There is a process on other end of the pipe. 
        /// </summary> 
        internal const int ERROR_PIPE_CONNECTED = 535;

        /// <summary> 
        /// Waiting for a process to open the other end of the pipe. 
        /// </summary> 
        internal const int ERROR_PIPE_LISTENING = 536;

        /// <summary> 
        /// Waits indefinitely when connecting to a pipe. 
        /// </summary> 
        internal const uint NMPWAIT_WAIT_FOREVER = 0xffffffff;

        /// <summary> 
        /// Does not wait for the named pipe. 
        /// </summary> 
        internal const uint NMPWAIT_NOWAIT = 0x00000001;

        /// <summary> 
        /// Uses the default time-out specified in a call to the CreateNamedPipe method. 
        /// </summary> 
        internal const uint NMPWAIT_USE_DEFAULT_WAIT = 0x00000000;

        #endregion

        #region[SECURITY_ATTRIBUTES]

        /// <summary> 
        /// The SECURITY_ATTRIBUTES structure contains the security descriptor for  
        /// an object and specifies whether the handle retrieved by specifying  
        /// this structure is inheritable. This structure provides security  
        /// settings for objects created by various functions, such as CreateFile,  
        /// CreateNamedPipe, CreateProcess, RegCreateKeyEx, or RegSaveKeyEx. 
        /// </summary> 
        [StructLayout(LayoutKind.Sequential)]
        internal class SECURITY_ATTRIBUTES
        {
            public int nLength;
            public SafeLocalMemHandle lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        #endregion

        #region[SafePipeHandles Wrapper Class]

        /// <summary> 
        /// Represents a wrapper class for a pipe handle.  
        /// </summary> 
        [SecurityCritical(SecurityCriticalScope.Everything), HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true), SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        internal sealed class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafePipeHandle() : base(true)
            {
            }

            public SafePipeHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool CloseHandle(IntPtr handle);

            protected override bool ReleaseHandle()
            {
                return CloseHandle(base.handle);
            }
        }

        #endregion

        #region[SafeMemHandles Wrapper Class]

        /// <summary> 
        /// Represents a wrapper class for a local memory pointer.  
        /// </summary> 
        [SuppressUnmanagedCodeSecurity, HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
        internal sealed class SafeLocalMemHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeLocalMemHandle() : base(true)
            {
            }

            public SafeLocalMemHandle(IntPtr preexistingHandle, bool ownsHandle) : base(ownsHandle)
            {
                base.SetHandle(preexistingHandle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success), DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr LocalFree(IntPtr hMem);

            protected override bool ReleaseHandle()
            {
                return (LocalFree(base.handle) == IntPtr.Zero);
            }
        }

        #endregion

        #region[NativeMathods Class (P/Invoke API)]

        /// <summary> 
        /// NativeMethod class exposes Windows APIs used in using the Named Pipe Server. 
        /// </summary> 
        [SuppressUnmanagedCodeSecurity]
        internal class NativeMethod
        {
            ///-------------------------------------------------------------------------------------------------
            /// <summary>   Suspend thread. </summary>
            /// <param name="hThread">  The thread. </param>
            ///
            /// <returns>   An int. </returns>
            ///-------------------------------------------------------------------------------------------------
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int SuspendThread(IntPtr hThread);

            ///-------------------------------------------------------------------------------------------------
            /// <summary>   Opens a thread. </summary>
            /// <param name="dwDesiredAccess">  The desired access. </param>
            /// <param name="bInheritHandle">   True to inherit handle. </param>
            /// <param name="dwThreadId">       Identifier for the thread. </param>
            ///
            /// <returns>   An IntPtr. </returns>
            ///-------------------------------------------------------------------------------------------------
            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

            ///-------------------------------------------------------------------------------------------------
            /// <summary>   Closes a handle. </summary>
            /// <param name="hObject">  The object. </param>
            ///
            /// <returns>   True if it succeeds, false if it fails. </returns>
            ///-------------------------------------------------------------------------------------------------
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            ///-------------------------------------------------------------------------------------------------
            /// <summary>   Terminate thread. </summary>
            /// <param name="hThread">      The thread. </param>
            /// <param name="dwExitCode">   The exit code. </param>
            ///
            /// <returns>   True if it succeeds, false if it fails. </returns>
            ///-------------------------------------------------------------------------------------------------
            [DllImport("kernel32.dll")]
            public static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);

            /// <summary> 
            /// Creates an instance of a named pipe and returns a handle for subsequent pipe operations. 
            /// </summary> 
            /// <param name="pipeName">Pipe name</param> 
            /// <param name="openMode">Pipe open mode</param> 
            /// <param name="pipeMode">Pipe-specific modes</param> 
            /// <param name="maxInstances">Maximum number of instances</param> 
            /// <param name="outBufferSize">Output buffer size</param> 
            /// <param name="inBufferSize">Input buffer size</param> 
            /// <param name="defaultTimeout">Time-out interval</param>
            /// <returns>If the function succeeds, the return value is a handle  
            /// to the server end of a named pipe instance.</returns> 
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafePipeHandle CreateNamedPipe(string pipeName, PipeOpenMode openMode, PipeMode pipeMode, int maxInstances, int outBufferSize, int inBufferSize, uint defaultTimeout);


            /// <summary> 
            /// Creates an instance of a named pipe with security attributes and returns a handle for subsequent pipe operations. 
            /// </summary> 
            /// <param name="pipeName">Pipe name</param> 
            /// <param name="openMode">Pipe open mode</param> 
            /// <param name="pipeMode">Pipe-specific modes</param> 
            /// <param name="maxInstances">Maximum number of instances</param> 
            /// <param name="outBufferSize">Output buffer size</param> 
            /// <param name="inBufferSize">Input buffer size</param> 
            /// <param name="defaultTimeout">Time-out interval</param> 
            /// <param name="securityAttributes">Security attributes</param> 
            /// <returns>If the function succeeds, the return value is a handle  
            /// to the server end of a named pipe instance.</returns> 
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafePipeHandle CreateNamedPipe(string pipeName, PipeOpenMode openMode, PipeMode pipeMode, int maxInstances, int outBufferSize, int inBufferSize, uint defaultTimeout, SECURITY_ATTRIBUTES securityAttributes);


            /// <summary> 
            /// Enables a named pipe server process to wait for a client process to  
            /// connect to an instance of a named pipe. 
            /// </summary> 
            /// <param name="hNamedPipe"> 
            /// Handle to the server end of a named pipe instance. 
            /// </param> 
            /// <param name="overlapped">Pointer to an Overlapped object.</param> 
            /// <returns> 
            /// If the function succeeds, the return value is true. 
            /// </returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool ConnectNamedPipe(SafePipeHandle hNamedPipe, IntPtr overlapped);


            /// <summary> 
            /// Waits until either a time-out interval elapses or an instance of the  
            /// specified named pipe is available for connection (that is, the pipe's  
            /// server process has a pending ConnectNamedPipe operation on the pipe). 
            /// </summary> 
            /// <param name="pipeName">The name of the named pipe.</param> 
            /// <param name="timeout"> 
            /// The number of milliseconds that the function will wait for an  
            /// instance of the named pipe to be available. 
            /// </param> 
            /// <returns> 
            /// If an instance of the pipe is available before the time-out interval  
            /// elapses, the return value is true. 
            /// </param> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool WaitNamedPipe(string pipeName, uint timeout);


            /// <summary> 
            /// Sets the read mode and the blocking mode of the specified named pipe. 
            /// </summary> 
            /// <remarks> 
            /// If the specified handle is to the client end of a named pipe and if 
            /// the named pipe server process is on a remote computer, the function 
            /// can also be used to control local buffering. 
            /// </remarks> 
            /// <param name="hNamedPipe">Handle to the named pipe instance.</param> 
            /// <param name="mode"> 
            /// Pointer to a variable that supplies the new mode. 
            /// </param> 
            /// <param name="maxCollectionCount"> 
            /// Reference to a variable that specifies the maximum number of bytes  
            /// collected on the client computer before transmission to the server. 
            /// </param> 
            /// <param name="collectDataTimeout"> 
            /// Reference to a variable that specifies the maximum time, in  
            /// milliseconds, that can pass before a remote named pipe transfers  
            /// information over the network. 
            /// </param> 
            /// <returns>If the function succeeds, the return value is true.</returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetNamedPipeHandleState(SafePipeHandle hNamedPipe, PipeMode mode, IntPtr maxCollectionCount, IntPtr collectDataTimeout);

            /// <summary> 
            /// Sets the read mode and the blocking mode of the specified named pipe. 
            /// </summary> 
            /// <remarks> 
            /// If the specified handle is to the client end of a named pipe and if 
            /// the named pipe server process is on a remote computer, the function 
            /// can also be used to control local buffering. 
            /// </remarks> 
            /// <param name="hNamedPipe">Handle to the named pipe instance.</param> 
            /// <param name="mode"> 
            /// Pointer to a variable that supplies the new mode. 
            /// </param> 
            /// <param name="maxCollectionCount"> 
            /// Reference to a variable that specifies the maximum number of bytes  
            /// collected on the client computer before transmission to the server. 
            /// </param> 
            /// <param name="collectDataTimeout"> 
            /// Reference to a variable that specifies the maximum time, in  
            /// milliseconds, that can pass before a remote named pipe transfers  
            /// information over the network. 
            /// </param> 
            /// <returns>If the function succeeds, the return value is true.</returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool SetNamedPipeHandleState(SafePipeHandle hNamedPipe, ref PipeMode mode, IntPtr maxCollectionCount, IntPtr collectDataTimeout);

            /// <summary> 
            /// Creates or opens a file, directory, physical disk, volume, console  
            /// buffer, tape drive, communications resource, mailslot, or named pipe. 
            /// </summary> 
            /// <param name="fileName"> 
            /// The name of the file or device to be created or opened. 
            /// </param> 
            /// <param name="desiredAccess"> 
            /// The requested access to the file or device, which can be summarized  
            /// as read, write, both or neither (zero). 
            /// </param> 
            /// <param name="shareMode"> 
            /// The requested sharing mode of the file or device, which can be read,  
            /// write, both, delete, all of these, or none (refer to the following  
            /// table).  
            /// </param> 
            /// <param name="securityAttributes"> 
            /// A SECURITY_ATTRIBUTES object that contains two separate but related  
            /// data members: an optional security descriptor, and a Boolean value  
            /// that determines whether the returned handle can be inherited by  
            /// child processes. 
            /// </param> 
            /// <param name="creationDisposition"> 
            /// An action to take on a file or device that exists or does not exist. 
            /// </param> 
            /// <param name="flagsAndAttributes"> 
            /// The file or device attributes and flags. 
            /// </param> 
            /// <param name="hTemplateFile">Handle to a template file.</param> 
            /// <returns> 
            /// If the function succeeds, the return value is an open handle to the  
            /// specified file, device, named pipe, or mail slot. 
            /// If the function fails, the return value is INVALID_HANDLE_VALUE. 
            /// </returns> 
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern SafePipeHandle CreateFile(string fileName, FileDesiredAccess desiredAccess, FileShareMode shareMode, SECURITY_ATTRIBUTES securityAttributes, FileCreationDisposition creationDisposition, int flagsAndAttributes, IntPtr hTemplateFile);


            /// <summary> 
            /// Reads data from the specified file or input/output (I/O) device. 
            /// </summary> 
            /// <param name="handle"> 
            /// A handle to the device (for example, a file, file stream, physical  
            /// disk, volume, console buffer, tape drive, socket, communications  
            /// resource, mailslot, or pipe). 
            /// </param> 
            /// <param name="bytes"> 
            /// A buffer that receives the data read from a file or device. 
            /// </param> 
            /// <param name="numBytesToRead"> 
            /// The maximum number of bytes to be read. 
            /// </param> 
            /// <param name="numBytesRead"> 
            /// The number of bytes read when using a synchronous IO. 
            /// </param> 
            /// <param name="overlapped"> 
            /// A pointer to an OVERLAPPED structure if the file was opened with  
            /// FILE_FLAG_OVERLAPPED. 
            /// </param>  
            /// <returns> 
            /// If the function succeeds, the return value is true. If the function  
            /// fails, or is completing asynchronously, the return value is false. 
            /// </returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool ReadFile(SafePipeHandle handle, byte[] bytes, int numBytesToRead, out int numBytesRead, IntPtr overlapped);


            /// <summary> 
            /// Writes data to the specified file or input/output (I/O) device. 
            /// </summary> 
            /// <param name="handle"> 
            /// A handle to the file or I/O device (for example, a file, file stream, 
            /// physical disk, volume, console buffer, tape drive, socket,  
            /// communications resource, mailslot, or pipe).  
            /// </param> 
            /// <param name="bytes"> 
            /// A buffer containing the data to be written to the file or device. 
            /// </param> 
            /// <param name="numBytesToWrite"> 
            /// The number of bytes to be written to the file or device. 
            /// </param> 
            /// <param name="numBytesWritten"> 
            /// The number of bytes written when using a synchronous IO. 
            /// </param> 
            /// <param name="overlapped"> 
            /// A pointer to an OVERLAPPED structure is required if the file was  
            /// opened with FILE_FLAG_OVERLAPPED. 
            /// </param> 
            /// <returns> 
            /// If the function succeeds, the return value is true. If the function  
            /// fails, or is completing asynchronously, the return value is false. 
            /// </returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool WriteFile(SafePipeHandle handle, byte[] bytes, int numBytesToWrite, out int numBytesWritten, IntPtr overlapped);


            /// <summary> 
            /// Flushes the buffers of the specified file and causes all buffered  
            /// data to be written to the file. 
            /// </summary> 
            /// <param name="hHandle">A handle to the open file. </param> 
            /// <returns> 
            /// If the function succeeds, the return value is true. 
            /// </returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool FlushFileBuffers(SafePipeHandle handle);


            /// <summary> 
            /// Disconnects the server end of a named pipe instance from a client 
            /// process. 
            /// </summary> 
            /// <param name="hNamedPipe">Handle to a named pipe instance.</param> 
            /// <returns> 
            /// If the function succeeds, the return value is true. 
            /// </returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DisconnectNamedPipe(SafePipeHandle hNamedPipe);


            /// <summary> 
            /// The ConvertStringSecurityDescriptorToSecurityDescriptor function  
            /// converts a string-format security descriptor into a valid,  
            /// functional security descriptor. 
            /// </summary> 
            /// <param name="sddlSecurityDescriptor"> 
            /// A string containing the string-format security descriptor (SDDL)  
            /// to convert. 
            /// </param> 
            /// <param name="sddlRevision"> 
            /// The revision level of the sddlSecurityDescriptor string.  
            /// Currently this value must be 1. 
            /// </param> 
            /// <param name="pSecurityDescriptor"> 
            /// A pointer to a variable that receives a pointer to the converted  
            /// security descriptor. 
            /// </param> 
            /// <param name="securityDescriptorSize"> 
            /// A pointer to a variable that receives the size, in bytes, of the  
            /// converted security descriptor. This parameter can be IntPtr.Zero. 
            /// </param> 
            /// <returns> 
            /// If the function succeeds, the return value is true. 
            /// </returns> 
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string sddlSecurityDescriptor, int sddlRevision, out SafeLocalMemHandle pSecurityDescriptor, IntPtr securityDescriptorSize);
        }

        #endregion

        #endregion
    }

    // Test Data Object - Used for testing IPC Server
    public class Order
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
    }
}

