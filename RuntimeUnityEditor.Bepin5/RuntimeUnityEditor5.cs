using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RuntimeUnityEditor.Core;
<<<<<<< HEAD
using RuntimeUnityEditor.Core.Networking.TCPServer;
using RuntimeUnityEditor.Core.Networking.IPCServer;
using RuntimeUnityEditor.Core.Networking.Remoting;
=======
using UnityEngine;
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
using LogLevel = RuntimeUnityEditor.Core.LogLevel;

namespace RuntimeUnityEditor.Bepin5
{
    [BepInPlugin(RuntimeUnityEditorCore.GUID, "Runtime Unity Editor", RuntimeUnityEditorCore.Version)]
    public class RuntimeUnityEditor5 : BaseUnityPlugin
    {
<<<<<<< HEAD
        #region[Declarations]

        public ConfigWrapper<string> DnSpyPath { get; private set; }
=======
        public ConfigEntry<string> DnSpyPath { get; private set; }
        public ConfigEntry<string> DnSpyArgs { get; private set; }
        public ConfigEntry<bool> ShowRepl { get; private set; }
        public ConfigEntry<bool> EnableMouseInspector { get; private set; }
        public ConfigEntry<KeyboardShortcut> Hotkey { get; private set; }
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153

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
<<<<<<< HEAD
            // Wh010ne Fork -----------------------------------------------------------------------------------------------------------
            EnableDebug = Config.Wrap("DEBUGGING", "enableDebugDump", "Enables additional debugging checks and logging. Warning: Will delay game load time.", bool.FalseString);
            // END EDIT ---------------------------------------------------------------------------------------------------------------

            Instance = new RuntimeUnityEditorCore(this, new Logger5(Logger), bool.Parse(EnableDebug.Value)); // Wh010ne Fork Edited
=======
            Instance = new RuntimeUnityEditorCore(this, new Logger5(Logger), Paths.ConfigPath);
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153

            DnSpyPath = Config.Bind("Inspector", "Path to dnSpy.exe", string.Empty, "Full path to dnSpy that will enable integration with Inspector. When correctly configured, you will see a new ^ buttons that will open the members in dnSpy.");
            DnSpyPath.SettingChanged += (sender, args) => DnSpyHelper.DnSpyPath = DnSpyPath.Value;
            DnSpyHelper.DnSpyPath = DnSpyPath.Value;

<<<<<<< HEAD
            // Wh010ne Fork --------------------------------
            //Instance.TelnetState = false;
            //Instance.IPCState = true;
            Instance.RemotingState = true;
            // END EDIT ------------------------------------
=======
            DnSpyArgs = Config.Bind("Inspector", "Optional dnSpy arguments", string.Empty, "Additional parameters that are added to the end of each call to dnSpy.");
            DnSpyArgs.SettingChanged += (sender, args) => DnSpyHelper.DnSpyArgs = DnSpyArgs.Value;
            DnSpyHelper.DnSpyArgs = DnSpyArgs.Value;

            if (Instance.Repl != null)
            {
                ShowRepl = Config.Bind("General", "Show REPL console", true);
                ShowRepl.SettingChanged += (sender, args) => Instance.ShowRepl = ShowRepl.Value;
                Instance.ShowRepl = ShowRepl.Value;
            }

            EnableMouseInspector = Config.Bind("General", "Enable Mouse Inspector", true);
            EnableMouseInspector.SettingChanged += (sender, args) => Instance.EnableMouseInspect = EnableMouseInspector.Value;
            Instance.EnableMouseInspect = EnableMouseInspector.Value;

            Hotkey = Config.Bind("General", "Open/close runtime editor", new KeyboardShortcut(KeyCode.F12));
            Hotkey.SettingChanged += (sender, args) => Instance.ShowHotkey = Hotkey.Value.MainKey;
            Instance.ShowHotkey = Hotkey.Value.MainKey;

            Instance.SettingsChanged += (sender, args) =>
            {
                Hotkey.Value = new KeyboardShortcut(Instance.ShowHotkey);
                if (ShowRepl != null) ShowRepl.Value = Instance.ShowRepl;
                DnSpyArgs.Value = DnSpyHelper.DnSpyArgs;
            };
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
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

        private sealed class Logger5 : ILoggerWrapper
        {
            private readonly ManualLogSource _logger;

            public Logger5(ManualLogSource logger)
            {
                _logger = logger;
            }

            public void Log(LogLevel logLogLevel, object content)
            {
                _logger.Log((BepInEx.Logging.LogLevel)logLogLevel, content);
            }
        }

        #endregion
    }
}
