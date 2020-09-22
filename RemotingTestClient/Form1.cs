using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters.Binary;
using RuntimeUnityEditor.Core.Networking.Remoting;
using Client; // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)

namespace RemotingTestClient
{
    public partial class Form1 : Form
    {
        #region[Declarations]

        private RemotingComms _ServerTalk = null;      // This object lives on the server
        private CallbackSink _CallbackSink = null;  // This object lives here on the client

        private GameConfiguration gameConfiguration = new GameConfiguration(); // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)

        #endregion

        #region[Constructor]

        public Form1()
        {
            InitializeComponent();

            // Just for debugging automation
            btnRegister_Click(null, null); 
            _ServerTalk.SendMessageToServer(new CommsInfo("Wakeup Fucker!", gameConfiguration));
        }

        #endregion

        #region[Client Registration]

        private void btnRegister_Click(object sender, EventArgs e)
        {
            // Creates a client object that 'lives' here on the client.
            _CallbackSink = new CallbackSink();

            // Hook into the event exposed on the Sink object so we can transfer a server message through back to this class.
            _CallbackSink.OnHostToClient += new delegateCommsInfo(CallbackSink_OnHostToClient);

            // Register a client channel so the server can communicate back - it needs a channel opened in order to make
            // the callback to the CallbackSink object that is anchored on the client!
            HttpChannel channel = new HttpChannel(9003);
            ChannelServices.RegisterChannel(channel, false);

            // Now create a transparent proxy to the server component
            object obj = Activator.GetObject(typeof(RemotingComms), "http://127.0.0.1:9000/RUETalk");

            // Cast returned object
            _ServerTalk = (RemotingComms)obj;

            // Register the client on the server with info on our callback to the client sink.
            var clientGuid = System.Guid.NewGuid();
            _ServerTalk.RegisterHostToClient(clientGuid.ToString(), new delegateCommsInfo(_CallbackSink.HandleToClient));

            // Make sure we can't register again!
            btnRegister.Enabled = false;
            btnRegister.Text = "Registered";
        }

        #endregion

        #region[Receive Message]

        /// <summary>
        /// Proxy / Receive a message from the server and send it to our callback
        /// </summary>
        /// <param name="info"></param>
        void CallbackSink_OnHostToClient(CommsInfo info)
        {
            
            // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
            gameConfiguration = info.GConfig;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RailsCost=" + gameConfiguration.RailsCost.ToString());
            sb.AppendLine("RailsRefund=" + gameConfiguration.RailsRefund.ToString());
            sb.AppendLine("NumBaseCars=" + gameConfiguration.EngineMaxVagonsBase.ToString());
            sb.AppendLine("NumCarsPerGen=" + gameConfiguration.EngineMaxVagonsPerGeneration.ToString());
            sb.AppendLine("EnginePrice=" + gameConfiguration.EnginePriceBase.ToString());
            sb.AppendLine("EnginePricePerGen=" + gameConfiguration.EnginePricePerGeneration.ToString());
            sb.AppendLine("EnginePriceUpgrade=" + gameConfiguration.EnginePricePerUpgrade.ToString());
            sb.AppendLine("EngineFullLoadSpeed=" + gameConfiguration.EngineSpeedFullLoad.ToString());
            sb.AppendLine("EngineFullLoadAccel=" + gameConfiguration.EngineAccelerationFullLoad.ToString());
            txtToServer.Text = sb.ToString();            

            if (this.txtFromServer.InvokeRequired)
            {
                this.txtFromServer.Invoke(new delegateCommsInfo(CallbackSink_OnHostToClient), new object[] { info });
            }
            else
            {
                if (info.Message != "NOTLOADED")
                {
                    this.txtFromServer.Text = "[Received Message]: " + info.Message + "\r\n    " + sb.ToString().Replace("\r\n", "\r\n    ") + "\r\n" + this.txtFromServer.Text;
                }
                else
                {
                    txtToServer.Text = sb.ToString();
                    this.txtFromServer.Text = "Load a Level Before Sending!";
                }
            }
            
            //----------------------------------------------------------------------------------------------------------------------------------
        }

        #endregion

        #region[Send Message]

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {            
            // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
            string[] tmp = txtToServer.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            
            foreach (string line in tmp)
            {
                if (line != String.Empty)
                {
                    sb.AppendLine("    " + line);

                    string[] keypair = line.Split('=');
                    string key = keypair[0].Trim();
                    string value = keypair[1].Trim();

                    switch (key)
                    {
                        case "RailsCost":
                            gameConfiguration.RailsCost = int.Parse(value);
                            break;
                        case "RailsRefund":
                            gameConfiguration.RailsRefund = int.Parse(value);
                            break;
                        case "NumBaseCars":
                            gameConfiguration.EngineMaxVagonsBase = int.Parse(value);
                            break;
                        case "NumCarsPerGen":
                            gameConfiguration.EngineMaxVagonsPerGeneration = int.Parse(value);
                            break;
                        case "EnginePrice":
                            gameConfiguration.EnginePriceBase = int.Parse(value);
                            break;
                        case "EnginePricePerGen":
                            gameConfiguration.EnginePricePerGeneration = int.Parse(value);
                            break;
                        case "EnginePriceUpgrade":
                            gameConfiguration.EnginePricePerUpgrade = int.Parse(value);
                            break;
                        case "EngineFullLoadSpeed":
                            gameConfiguration.EngineSpeedFullLoad = float.Parse(value);
                            break;
                        case "EngineFullLoadAccel":
                            gameConfiguration.EngineAccelerationFullLoad = float.Parse(value);
                            break;
                    }
                }
            }
            
            _ServerTalk.SendMessageToServer(new CommsInfo("GameConfig", gameConfiguration));
            this.txtFromServer.Text = "[Sent Message]: GameConfig\r\n" + sb.ToString() + "\r\n" + this.txtFromServer.Text;
            
        }

        #endregion
    }
}