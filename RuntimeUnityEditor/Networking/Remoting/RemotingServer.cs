﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting; //Note: Use the one from Mono in the Unity install folder. Unity's version is a lightweight version.
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using UnityEngine;
using Client; // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)

namespace RuntimeUnityEditor.Core.Networking.Remoting
{
    public class RemotingServer : MonoBehaviour
    {
        #region[Declarations]

        private static RemotingServer instance;

        private List<string> _Clients = new List<string>();

        public bool isRunning = false;

        private bool _ServerStopping = false;

        //public string _scrnMessage = "Remoting Server Status: Unknown";

        #endregion

        #region[Constructor]

        public RemotingServer()
        {
            instance = this;
        }

        #endregion

        #region[Unity Workflow]

        public void Start()
        {
            if (!isRunning)
            {
                // Required if you don't want the server slowing down the main thread.
                Application.runInBackground = true;

                // Turn Off Stacktraces: Get rid of Unity log spam bug (i.e. (Filename: C:\buildslave\unity\build\Runtime/Export/Debug.bindings.h Line: XX) showing after every Debug.Write() )
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

                StartServer();
            }
            else
            {
                Console.WriteLine("[Remoting Server]: Already Running!");
            }
        }

        public void OnGUI()
        {
            //if (RuntimeUnityEditorCore.Instance.RemotingState) { _scrnMessage = "Remoting Server Running"; } else { _scrnMessage = "Remoting Server NOT Running!"; }

            // Display in-game notification status
            //GUI.color = Color.red;
            //GUI.Label(new Rect(725, 70, 350, 100), _scrnMessage);
        }

        public void Update()
        {
            //_message = "Remoting Server Running: " + this.isRunning.ToString();
        }

        public void OnDisable()
        {
            StopServer();
        }

        #endregion

        #region[Server Control]

        private void StartServer()
        {
            Console.WriteLine("[Remoting Server]: Starting");

            // Register a server channel on the Server where we will listen for clients
            RegisterChannel();

            // Register callbacks to the static properties in the shared (dll) ServerTalk object
            RemotingComms.NewClient = new delegateClientInfo(NewClient);
            RemotingComms.ClientToHost = new delegateCommsInfo(ClientToHost);

            // Run listening thread that checks for new messages received from clients 
            Thread t = new Thread(new ThreadStart(CheckClientToServerQueue));
            t.Start();

            this.isRunning = true;

            Console.WriteLine("[Remoting Server]: Running");
        }

        public void StopServer()
        {
            Console.WriteLine("[Remoting Server]: Stopping!]");

            _ServerStopping = true;

            this.isRunning = false;
        }

        // Registers a new Remoting httpChannel utilizing SOAP formatter for serialization
        private void RegisterChannel()
        {
            Console.WriteLine("[Remoting Server]: Registering httpChannel");

            try
            {
                // Set the TypeFilterLevel to Full since callbacks require additional security requirements
                SoapServerFormatterSinkProvider serverFormatter = new SoapServerFormatterSinkProvider();
                serverFormatter.TypeFilterLevel = TypeFilterLevel.Full;

                // We have to change the name since we can't have two channels with the same name.
                Hashtable ht = new Hashtable();
                ht["name"] = "ServerChannel";
                ht["port"] = 9000;

                // Now create and register our custom HttpChannel 
                HttpChannel channel = new HttpChannel(ht, null, serverFormatter);
                ChannelServices.RegisterChannel(channel, false);

                // Register a 'Well Known Object' type in Singleton mode
                string identifier = "RUETalk";
                WellKnownObjectMode mode = WellKnownObjectMode.Singleton;

                // Register our Object model (RemotingComms)
                WellKnownServiceTypeEntry entry = new WellKnownServiceTypeEntry(typeof(RemotingComms), identifier, mode);
                RemotingConfiguration.RegisterWellKnownServiceType(entry);
            }
            catch(Exception e)
            {
                if (!e.Message.Contains("Prefix already in use."))
                {
                    Console.WriteLine("[Remoting Server ERROR]: Message - " + e.Message);
                }
                else
                {
                    Console.WriteLine("[Remoting Server]: httpChannel Registered");
                }
            }
        }

        // The method that will be called when a new client registers.
        private void NewClient(string ClientID)
        {
            Console.WriteLine("[Remoting Server]: Registering New Client - " + ClientID);
            _Clients.Add(ClientID);

            // since it originated from a different thread we need to marshal this back to the current UI thread.
            /*
            if (this._Clients.InvokeRequired)
                this._Clients.Invoke(new delClientInfo(NewClient), new object[] { ClientID });
            else
            {
                this._Clients.Items.Add(ClientID);
                this._Clients.Text = ClientID;
            }
            */
        }

        #endregion

        #region[Receiving]

        // A helper method that will marshal a CommsInfo from the client to our UI thread.
        private void ClientToHost(CommsInfo Info)
        {
            // Since it originated from a different thread we need to marshal this back to the current UI thread.
            //if (this.txtFromClient.InvokeRequired)
            //    this.txtFromClient.Invoke(new delegateCommsInfo(ClientToHost), new object[] { Info });
            //else { }

            Console.WriteLine("[Remoting Server]: Received - " + Info.Message);
        }

        // Called from our t.Start(). A loop invoked by a worker-thread which will monitor the static thread-safe  
        // ClientToServer Queue on the ServerTalk class and passes on any CommsInfo objects that are placed here.
        // If the variable _ServerStopping turns true it will stop the loop and subsequently the life of the worker-thread.
        private void CheckClientToServerQueue()
        {
            while (!_ServerStopping)
            {
                Thread.Sleep(50);   // Allow rest of the system to continue whilst waiting...
                if (RemotingComms.ClientToServerQueue.Count > 0)
                {
                    // Received a new message, Marshal it to appropriate thread passing it through ClientToHost to update server GUI for example
                    CommsInfo message = (CommsInfo)RemotingComms.ClientToServerQueue.Dequeue();
                    ClientToHost(message); // Marshall it

                    //----------------------------------------------------------------------------------------------------------
                    // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
                    bool loaded = false;
                    var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                    if (currentScene.name != "MainMenu")
                    {
                        Game.Settings.RailsCost = message.GConfig.RailsCost;
                        Game.Settings.RailsRefund = message.GConfig.RailsRefund;
                        Game.Settings.EngineMaxVagonsBase = message.GConfig.EngineMaxVagonsBase;
                        Game.Settings.EngineMaxVagonsPerGeneration = message.GConfig.EngineMaxVagonsPerGeneration;
                        Game.Settings.EnginePriceBase = message.GConfig.EnginePriceBase;
                        Game.Settings.EnginePricePerGeneration = message.GConfig.EnginePricePerGeneration;
                        Game.Settings.EnginePricePerUpgrade = message.GConfig.EnginePricePerUpgrade;
                        Game.Settings.EngineSpeedFullLoad = message.GConfig.EngineSpeedFullLoad;
                        Game.Settings.EngineAccelerationFullLoad = message.GConfig.EngineAccelerationFullLoad;
                        loaded = true;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("    RailsCost=" + message.GConfig.RailsCost.ToString());
                    sb.AppendLine("    RailsRefund=" + message.GConfig.RailsRefund.ToString());
                    sb.AppendLine("    NumBaseCars=" + message.GConfig.EngineMaxVagonsBase.ToString());
                    sb.AppendLine("    NumCarsPerGen=" + message.GConfig.EngineMaxVagonsPerGeneration.ToString());
                    sb.AppendLine("    EnginePrice=" + message.GConfig.EnginePriceBase.ToString());
                    sb.AppendLine("    EnginePricePerGen=" + message.GConfig.EnginePricePerGeneration.ToString());
                    sb.AppendLine("    EnginePriceUpgrade=" + message.GConfig.EnginePricePerUpgrade.ToString());
                    sb.AppendLine("    EngineFullLoadSpeed=" + message.GConfig.EngineSpeedFullLoad.ToString());
                    sb.AppendLine("    EngineFullLoadAccel=" + message.GConfig.EngineAccelerationFullLoad.ToString());                    

                    //In this case I'm just sending back the Updated GameConfiguration object
                    if (loaded == true)
                    {
                        Console.WriteLine("[Remoting Server]: Received Message\r\n\r\nGameConfig:\r\n" + sb.ToString());
                        SendToClient("GameConfig", _Clients.FirstOrDefault(), false, Game.Settings);
                    }
                    else
                    {
                        Console.WriteLine("[Remoting Server]: Received Message\r\n\r\nDefault GameConfig:\r\n" + sb.ToString());
                        SendToClient("NOTLOADED", _Clients.FirstOrDefault(), false, new GameConfiguration());
                    }
                    //----------------------------------------------------------------------------------------------------------
                }
            }
        }

        #endregion

        #region[Sending]

        // Send a message to client. EXAMPLE Params, you can replace GameConfiguration
        private void SendToClient(string message, string client, bool allClients = false, GameConfiguration gConfig = null)
        {
            if (_Clients.Count == 0) { Console.WriteLine("[Remoting Server]: No Clients Registered!"); return; }

            string ClientID = _Clients.FirstOrDefault(s => s.Contains(client));
            if (ClientID == null) { Console.WriteLine("[Remoting Server]: Client Doesn't Exist!]"); return; }
            if (allClients) ClientID = "*";

            //----------------------------------------------------------------------------------------------------------
            // Remember, we don't modify it server side the game does that, we just pass the object to the client and
            // let the client modify it and send it back. When we get it back, we update the Game object.
            // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
            RemotingComms.RaiseHostToClient(ClientID, message, gConfig);
            //----------------------------------------------------------------------------------------------------------

            //Console.WriteLine("[Remoting Server]: Server Replied to Client");
        }

        #endregion

    }
}
