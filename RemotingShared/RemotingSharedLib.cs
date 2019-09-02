using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using Client; // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)

namespace RuntimeUnityEditor.Core.Networking.Remoting
{
    #region[Delegate Declarations]

    public delegate void delegateClientInfo(string ClientID);
    public delegate void delegateCommsInfo(CommsInfo info);

    #endregion

    #region[RemotingTalk Class]

    // Class is created on the server and allows for client to register their existence and
    // a callback that the server can use to communicate back through.
    public class RemotingComms : MarshalByRefObject
    {
        #region[Declarations]

        private static delegateClientInfo _NewClient;
        private static delegateCommsInfo _ClientToHost;
        private static List<ClientWrap> _list = new List<ClientWrap>();

        #endregion

        #region[Delegates]

        /// <summary>
        /// The host should register a function pointer to which it wants a signal sent when a client registers
        /// </summary>
        public static delegateClientInfo NewClient
        {
            get { return _NewClient; }
            set { _NewClient = value; }
        }

        /// <summary>
        /// The host should register a function pointer to which it wants the CommsInfo object
        /// sent to when the client wants to communicate to the server
        /// </summary>
        public static delegateCommsInfo ClientToHost
        {
            get { return _ClientToHost; }
            set { _ClientToHost = value; }
        }

        #endregion

        #region[Communication and Queues]

        /// <summary>
        /// Register a new client. Basically adds to a list of clients
        /// </summary>
        /// <param name="ClientID"></param>
        /// <param name="htc"></param>
        public void RegisterHostToClient(string ClientID, delegateCommsInfo htc)
        {
            _list.Add(new ClientWrap(ClientID, htc));

            if (_NewClient != null)
                _NewClient(ClientID);
        }

        /// <summary>
        /// The static method that will be invoked by the server when it wants to send a message to a specific client or all of them.
        /// </summary>
        /// <param name="ClientID"></param>
        /// <param name="Message"></param>
        public static void RaiseHostToClient(string ClientID, string Message, GameConfiguration gameConfig) // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll) Replace GameConfiguration
        {
            foreach (ClientWrap client in _list)
            {
                if ((client.ClientID == ClientID || ClientID == "*") && client.HostToClient != null)
                    client.HostToClient(new CommsInfo(Message, gameConfig)); // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
            }
        }

        /// <summary>
        /// A thread-safe queue that will contain any message objects that should be sent to the server
        /// </summary>
        private static Queue _ClientToServer = Queue.Synchronized(new Queue());

        /// <summary>
        /// This instance method allows a client to send a message to the server
        /// </summary>
        /// <param name="Message"></param>
        public void SendMessageToServer(CommsInfo Message)
        {
            _ClientToServer.Enqueue(Message);
        }

        public static Queue ClientToServerQueue
        {
            get { return _ClientToServer; }
        }

        #endregion

        #region[ClientWrap Class]

        /// <summary>
        /// Small private class to wrap the Client and the callback together.
        /// </summary>
        private class ClientWrap
        {
            private string _ClientID = "";
            private delegateCommsInfo _HostToClient = null;

            public ClientWrap(string ClientID, delegateCommsInfo HostToClient)
            {
                _ClientID = ClientID;
                _HostToClient = HostToClient;
            }

            public string ClientID
            {
                get { return _ClientID; }
            }

            public delegateCommsInfo HostToClient
            {
                get { return _HostToClient; }
            }
        }

        #endregion
    }

    #endregion

    #region[The Good Stuff! Our Comms Object]

    /// <summary>
    /// A serializable class that is what gets passed back and forth. Any Derivative Types need to also be Serializable.
    /// For example, below here you'll see a GameObject this will cause an exception since in Unity the GameObject, and
    /// it's base type Unity.Object are not serializable, however, with Harmony we can overcome that later on by setting
    /// the class attribute [Serializable] at runtime.
    /// </summary>
    [Serializable]
    public class CommsInfo
    {
        private string _Message = "";

        // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
        [SerializeField] private GameConfiguration _GConfig = null; 

        public CommsInfo(string Message, GameConfiguration gameConfig = null)
        {
            _Message = Message;
            _GConfig = gameConfig;
        }

        public string Message
        {
            get { return _Message; }
            set { _Message = value; }
        }

        // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
        public GameConfiguration GConfig
        {
            get { return _GConfig; }
            set { _GConfig = value; }
        }
    }

    #endregion

    #region[Callback Sink]

    /// <summary>
    /// This CallbackSink object is hosted on the client and is used as the target for a callback given to the server.
    /// This is the method that the server will make the callback to.
    /// </summary>
    public class CallbackSink : MarshalByRefObject
    {
        public event delegateCommsInfo OnHostToClient;

        public CallbackSink() { }

        [OneWay] // Note one-way communication
        public void HandleToClient(CommsInfo info)
        {
            if (OnHostToClient != null)
                OnHostToClient(info);
        }
    }

    #endregion
}
