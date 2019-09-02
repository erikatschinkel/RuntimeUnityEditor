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

        private GameConfiguration gameConfiguration = null; // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)

        #endregion

        #region[Constructor]

        public Form1()
        {
            InitializeComponent();
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
            _ServerTalk.RegisterHostToClient(this.txtClientID.Text, new delegateCommsInfo(_CallbackSink.HandleToClient));

            // Make sure we can't register again!
            btnRegister.Enabled = false;   
        }

        #endregion

        #region[Receive Message]

        /// <summary>
        /// Proxy / Receive a message from the server and send it to our callback
        /// </summary>
        /// <param name="info"></param>
        void CallbackSink_OnHostToClient(CommsInfo info)
        {
            gameConfiguration = info.GConfig;

            if (this.txtFromServer.InvokeRequired)
                this.txtFromServer.Invoke(new delegateCommsInfo(CallbackSink_OnHostToClient), new object[] { info });
            else
                this.txtFromServer.Text = "[Received Message]:\r\n    " + info.Message + "\r\n        RailsCost: " + gameConfiguration.RailsCost.ToString() + Environment.NewLine + this.txtFromServer.Text;
        }

        #endregion

        #region[Sent Message]

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            // EXAMPLE (Each game is Different, check your Assembly-CSharp.dll)
            if (txtToServer.Text.Contains("RailsCost="))
            {
                string[] keypair = txtToServer.Text.Split('=');
                int newValue = int.Parse(keypair[1].Trim());
                gameConfiguration.RailsCost = newValue; // modify it just for illustration, it should update on server/game
            }

            _ServerTalk.SendMessageToServer(new CommsInfo(this.txtToServer.Text, gameConfiguration));
            this.txtFromServer.Text = "[Sent Message]:\r\n    " + this.txtToServer.Text + Environment.NewLine + this.txtFromServer.Text;
        }

        #endregion
    }
}