using System.ComponentModel;
using BepInEx;
using RuntimeUnityEditor.Core;
using RuntimeUnityEditor.Core.Networking.TCPServer;
using RuntimeUnityEditor.Core.Networking.IPCServer;
using RuntimeUnityEditor.Core.Networking.Remoting;

namespace RuntimeUnityEditor.Bepin4
{
    [BepInPlugin(RuntimeUnityEditorCore.GUID, "Runtime Unity Editor (Legacy)", RuntimeUnityEditorCore.Version)]
    public class RuntimeUnityEditor4 : BaseUnityPlugin
    {
        #region[Declarations]

        [DisplayName("Path to dnSpy.exe")]
        [Description("Full path to dnSpy that will enable integration with Inspector.\n\n" + "When correctly configured, you will see a new ^ buttons that will open the members in dnSpy.")]

        public ConfigWrapper<string> DnSpyPath { get; private set; }

        //Wh010ne Fork -----------------------------------------------------------------------------------------------------------
        public ConfigWrapper<string> EnableDebug { get; set; }

        private TelnetServer telnetServer = new TelnetServer();

        private IPCServer ipcServer = new IPCServer();

        private RemotingServer remotingServer = new RemotingServer();
        //END EDIT ---------------------------------------------------------------------------------------------------------------

        public static RuntimeUnityEditorCore Instance { get; set; }

        #endregion

        #region[Unity Workflow]

        private void OnGUI()
        {
            Instance.OnGUI();

            // Wh010ne Fork --------------------------------
            telnetServer.OnGUI();
            ipcServer.OnGUI();
            remotingServer.OnGUI();
            // END EDIT ------------------------------------
        }

        private void Start()
        {
<<<<<<< HEAD
            //Wh010ne Fork -----------------------------------------------------------------------------------------------------------
            EnableDebug = new ConfigWrapper<string>("enableDebugDump", "DEBUGGING", bool.FalseString);       
            //END EDIT ---------------------------------------------------------------------------------------------------------------

            Instance = new RuntimeUnityEditorCore(this, new Logger(), bool.Parse(EnableDebug.Value)); //Wh010ne Fork Edited

=======
            Instance = new RuntimeUnityEditorCore(this, new Logger(), Paths.PluginPath);
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
            DnSpyPath = new ConfigWrapper<string>(nameof(DnSpyPath), this);
            DnSpyPath.SettingChanged += (sender, args) => DnSpyHelper.DnSpyPath = DnSpyPath.Value;
            DnSpyHelper.DnSpyPath = DnSpyPath.Value;

            // Wh010ne Fork --------------------------------
            //Instance.TelnetState = false;
            //Instance.IPCState = true;
            //Instance.RemotingState = true;
            // END EDIT ------------------------------------
        }

        private void Update()
        {
            Instance.Update();

            // Wh010ne Fork --------------------------------
            telnetServer.Update();
            ipcServer.Update();
            remotingServer.Update();
            // END EDIT ------------------------------------
        }

<<<<<<< HEAD
        #endregion

        #region[Logging]
=======
        private void LateUpdate()
        {
            Instance.LateUpdate();
        }
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153

        private sealed class Logger : ILoggerWrapper
        {
            public void Log(LogLevel logLogLevel, object content)
            {
                BepInEx.Logger.Log((BepInEx.Logging.LogLevel) logLogLevel, "[RuntimeEditor]" + content);
            }
        }

        #endregion
    }
}
