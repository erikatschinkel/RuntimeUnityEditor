using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using UnityEngine;

namespace RuntimeUnityEditor.Core.Networking.Remoting
{
    //NOTE: del = Delegate

    public delegate void delUserInfo(string UserID);
    public delegate void delCommsInfo(CommsInfo info);

    // Class is created on the server and allows for client to register their existence and
    // a callback that the server can use to communicate back through.
    public class ServerTalk : MarshalByRefObject
    {
        private static delUserInfo _NewUser;
        private static delCommsInfo _ClientToHost;
        private static List<ClientWrap> _list = new List<ClientWrap>();

        /// <summary>
        /// Register a new client. Basically adds to a list of clients
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="htc"></param>
        public void RegisterHostToClient(string UserID, delCommsInfo htc)
        {
            _list.Add(new ClientWrap(UserID, htc));

            if (_NewUser != null)
                _NewUser(UserID);
        }

        /// <summary>
        /// The host should register a function pointer to which it wants a signal sent when a User Registers
        /// </summary>
        public static delUserInfo NewUser
        {
            get { return _NewUser; }
            set { _NewUser = value; }
        }

        /// <summary>
        /// The host should register a function pointer to which it wants the CommsInfo object
        /// send when the client wants to communicate to the server
        /// </summary>
        public static delCommsInfo ClientToHost
        {
            get { return _ClientToHost; }
            set { _ClientToHost = value; }
        }

        /// <summary>
        /// The static method that will be invoked by the server when it wants to send a message to a specific user or all of them.
        /// </summary>
        /// <param name="UserID"></param>
        /// <param name="Message"></param>
        public static void RaiseHostToClient(string UserID, string Message)
        {
            foreach (ClientWrap client in _list)
            {
                if ((client.UserID == UserID || UserID == "*") && client.HostToClient != null)
                    client.HostToClient(new CommsInfo(Message));
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

        /// <summary>
        /// Small private class to wrap the User and the callback together.
        /// </summary>
        private class ClientWrap
        {
            private string _UserID = "";
            private delCommsInfo _HostToClient = null;

            public ClientWrap(string UserID, delCommsInfo HostToClient)
            {
                _UserID = UserID;
                _HostToClient = HostToClient;
            }

            public string UserID
            {
                get { return _UserID; }
            }

            public delCommsInfo HostToClient
            {
                get { return _HostToClient; }
            }
        }
    }

    /// <summary>
    /// A serializable class that is what gets passed back and forth.
    /// </summary>
    [Serializable()]
    public class CommsInfo
    {
        private string _Message = "";
        private GameObject _GO = null;

        public CommsInfo(string Message, GameObject gameObj = null)
        {
            _Message = Message;
            _GO = gameObj;
        }

        public string Message
        {
            get { return _Message; }
            set { _Message = value; }
        }

        public GameObject GO
        {
            get { return _GO; }
            set { _GO = value; }
        }
    }

    /// <summary>
    /// This CallbackSink object will be 'anchored' on the client and is used as the target for a callback given to the server.
    /// this is the method that the server make make the callback to.
    /// </summary>
    public class CallbackSink : MarshalByRefObject
    {
        public event delCommsInfo OnHostToClient;

        public CallbackSink()
        { }

        [OneWay]
        public void HandleToClient(CommsInfo info)
        {
            if (OnHostToClient != null)
                OnHostToClient(info);
        }
    }
}
