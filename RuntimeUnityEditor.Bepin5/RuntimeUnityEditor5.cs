using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RuntimeUnityEditor.Core;
using RuntimeUnityEditor.Core.Networking.TCPServer;
using RuntimeUnityEditor.Core.Networking.IPCServer;
using RuntimeUnityEditor.Core.Networking.Remoting;
using LogLevel = RuntimeUnityEditor.Core.LogLevel;

namespace RuntimeUnityEditor.Bepin5
{
    [BepInPlugin(RuntimeUnityEditorCore.GUID, "Runtime Unity Editor", RuntimeUnityEditorCore.Version)]
    public class RuntimeUnityEditor5 : BaseUnityPlugin
    {
        #region[Declarations]

        public ConfigWrapper<string> DnSpyPath { get; private set; }

        public static RuntimeUnityEditorCore Instance { get; private set; }

        // Wh010ne Fork -----------------------------------------------------------------------------------------------------------
        public ConfigWrapper<string> EnableDebug { get; private set; }

        private TelnetServer telnetServer = new TelnetServer();

        private IPCServer ipcServer = new IPCServer();

        private RemotingServer remotingServer = new RemotingServer();
        // END EDIT ---------------------------------------------------------------------------------------------------------------

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
            // Wh010ne Fork -----------------------------------------------------------------------------------------------------------
            EnableDebug = Config.Wrap("DEBUGGING", "enableDebugDump", "Enables additional debugging checks and logging. Warning: Will delay game load time.", bool.FalseString);
            // END EDIT ---------------------------------------------------------------------------------------------------------------

            Instance = new RuntimeUnityEditorCore(this, new Logger5(Logger), bool.Parse(EnableDebug.Value)); // Wh010ne Fork Edited

            DnSpyPath = Config.Wrap(null, "Path to dnSpy.exe", "Full path to dnSpy that will enable integration with Inspector. When correctly configured, you will see a new ^ buttons that will open the members in dnSpy.", string.Empty);
            DnSpyPath.SettingChanged += (sender, args) => DnSpyHelper.DnSpyPath = DnSpyPath.Value;
            DnSpyHelper.DnSpyPath = DnSpyPath.Value;

            // Wh010ne Fork --------------------------------
            //Instance.TelnetState = false;
            //Instance.IPCState = true;
            Instance.RemotingState = true;
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

        #endregion

        #region[Logging]

        private sealed class Logger5 : ILoggerWrapper
        {
            private readonly ManualLogSource _logger;

            public Logger5(ManualLogSource logger)
            {
                _logger = logger;
            }

            public void Log(LogLevel logLogLevel, object content)
            {
                _logger.Log((BepInEx.Logging.LogLevel) logLogLevel, content);
            }
        }

        #endregion
    }
}
