using System;
<<<<<<< HEAD
=======
using System.Collections;
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
using System.IO;
using RuntimeUnityEditor.Core.Gizmos;
using RuntimeUnityEditor.Core.ObjectTree;
using RuntimeUnityEditor.Core.REPL;
<<<<<<< HEAD
using RuntimeUnityEditor.Core.Debugging;
using RuntimeUnityEditor.Core.Networking.TCPServer;
using RuntimeUnityEditor.Core.Networking.IPCServer;
using RuntimeUnityEditor.Core.Networking.Remoting;
=======
using RuntimeUnityEditor.Core.UI;
using RuntimeUnityEditor.Core.Utils;
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
using UnityEngine;

namespace RuntimeUnityEditor.Core
{
    public class RuntimeUnityEditorCore
    {
<<<<<<< HEAD
        #region[Declarations]

        public const string Version = "1.6.0.1";
=======
        public const string Version = "2.2.1";
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
        public const string GUID = "RuntimeUnityEditor";

        public Inspector.Inspector Inspector { get; }
        public ObjectTreeViewer TreeViewer { get; }
<<<<<<< HEAD
        public ReplWindow Repl { get; }
        
        public KeyCode ShowHotkey { get; set; } = KeyCode.F12;
=======
        public ReplWindow Repl { get; private set; }

        public event EventHandler SettingsChanged;
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153

        public KeyCode ShowHotkey
        {
            get => _showHotkey;
            set
            {
                if (_showHotkey != value)
                {
                    _showHotkey = value;
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowRepl
        {
            get => Repl != null && Repl.Show;
            set
            {
                if (Repl != null && Repl.Show != value)
                {
                    Repl.Show = value;
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool EnableMouseInspect
        {
            get => MouseInspect.Enable;
            set
            {
                if (MouseInspect.Enable != value)
                {
                    MouseInspect.Enable = value;
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ShowInspector
        {
            get => Inspector != null && Inspector.Show;
            set
            {
                if (Inspector != null && Inspector.Show != value)
                {
                    Inspector.Show = value;
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public static RuntimeUnityEditorCore Instance { get; private set; }
        internal static MonoBehaviour PluginObject { get; private set; }
        internal static ILoggerWrapper Logger { get; private set; }

        private readonly GizmoDrawer _gizmoDrawer;
        private readonly GameObjectSearcher _gameObjectSearcher = new GameObjectSearcher();

        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;
        private KeyCode _showHotkey = KeyCode.F12;

<<<<<<< HEAD
        // Wh010ne Fork -----------------------------------------------------------------------------------------------------------
        private TelnetServer _tcpServer = new TelnetServer();
        private IPCServer _ipcServer = new IPCServer();
        private RemotingServer _remotingServer = new RemotingServer();
        public static bool _enableDebug { get; set; }

        public KeyCode IPCOnOff { get; set; } = KeyCode.F11;
        public KeyCode TelnetOnOff { get; set; } = KeyCode.F10;
        public KeyCode RemotingOnOff { get; set; } = KeyCode.F9;
        // END EDIT ---------------------------------------------------------------------------------------------------------------

        #endregion

        public RuntimeUnityEditorCore(MonoBehaviour pluginObject, ILoggerWrapper logger, bool enableDebugDump)
=======
        internal RuntimeUnityEditorCore(MonoBehaviour pluginObject, ILoggerWrapper logger, string configPath)
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
        {
            if (Instance != null)
                throw new InvalidOperationException("Can only create one instance of the Core object");

            PluginObject = pluginObject;
            Logger = logger;
            Instance = this;

            //Wh010ne Fork -----------------------------------------------------------------------------------------------------------           
            _enableDebug = enableDebugDump;
            if (_enableDebug) { Logger.Log(LogLevel.Info, "Enabling enhanced debug logging. Load time will be affected!"); DebugHelpers.SetDebugPath(); }
            //END EDIT ---------------------------------------------------------------------------------------------------------------

            Inspector = new Inspector.Inspector(targetTransform => TreeViewer.SelectAndShowObject(targetTransform));

            TreeViewer = new ObjectTreeViewer(pluginObject, _gameObjectSearcher);
            TreeViewer.InspectorOpenCallback = items =>
            {
                for (var i = 0; i < items.Length; i++)
                {
                    var stackEntry = items[i];
                    Inspector.Push(stackEntry, i == 0);
                }
            };

            if (UnityFeatureHelper.SupportsVectrosity)
            {
                _gizmoDrawer = new GizmoDrawer(pluginObject);
                TreeViewer.TreeSelectionChangedCallback = transform => _gizmoDrawer.UpdateState(transform);
            }

<<<<<<< HEAD
            if (Utils.UnityFeatureHelper.SupportsCursorIndex && Utils.UnityFeatureHelper.SupportsXml)
=======
            if (UnityFeatureHelper.SupportsRepl)
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
            {
                try
                {
                    Repl = new ReplWindow(Path.Combine(configPath, "RuntimeUnityEditor.Autostart.cs"));
                    PluginObject.StartCoroutine(DelayedReplSetup());
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, "Failed to load REPL - " + ex.Message);
                    Repl = null;
                }
            }
        }

<<<<<<< HEAD
        #region[Unity Workflow]

        public void OnGUI()
=======
        private IEnumerator DelayedReplSetup()
        {
            yield return null;
            try
            {
                Repl.RunEnvSetup();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, "Failed to load REPL - " + ex.Message);
                Repl = null;
            }
        }

        internal void OnGUI()
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
        {
            if (Show)
            {
                var originalSkin = GUI.skin;
                GUI.skin = InterfaceMaker.CustomSkin;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Inspector.DisplayInspector();
                TreeViewer.DisplayViewer();
                Repl?.DisplayWindow();
                
                MouseInspect.OnGUI();

                // Restore old skin for maximum compatibility
                GUI.skin = originalSkin;
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(ShowHotkey))
                Show = !Show;

            //Wh010ne Fork -----------------------------------
            if (Input.GetKeyDown(IPCOnOff))
                IPCState = !IPCState;

            if (Input.GetKeyDown(TelnetOnOff))
                TelnetState = !TelnetState;

            if (Input.GetKeyDown(RemotingOnOff))
                RemotingState = !RemotingState;
            //END EDIT ---------------------------------------

            Inspector.InspectorUpdate();
        }

        #endregion

        #region[Accessors/States]

        public bool Show
        {
            get => TreeViewer.Enabled;
            set
            {
                if (Show != value)
                {
                    if (value)
                    {
                        _previousCursorLockState = Cursor.lockState;
                        _previousCursorVisible = Cursor.visible;
                    }
                    else
                    {
                        if (!_previousCursorVisible || _previousCursorLockState != CursorLockMode.None)
                        {
                            Cursor.lockState = _previousCursorLockState;
                            Cursor.visible = _previousCursorVisible;
                        }
                    }
                }

                TreeViewer.Enabled = value;

                if (_gizmoDrawer != null)
                {
                    _gizmoDrawer.Show = value;
                    _gizmoDrawer.UpdateState(TreeViewer.SelectedTransform);
                }

                if (value)
                {
                    SetWindowSizes();

                    RefreshGameObjectSearcher(true);
                }
            }
        }

<<<<<<< HEAD
        // Wh010ne Fork --------------------------------------------------------------------------------------------------------------------        
        public bool TelnetState
=======
        internal void Update()
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
        {
            get => _tcpServer.isRunning;
            set
            {
                if (value)
                {
                    _tcpServer.Start();
                }
                else
                {
                    _tcpServer.StopTCPServer();
                }
            }
        }

<<<<<<< HEAD
        public bool IPCState
        {
            get => _ipcServer.isRunning;
            set
            {
                if(value)
                {
                    _ipcServer.StartServer();
                }
                else
                {
                    _ipcServer.StopServer();
                }
            }
=======
            if (Show)
            {
                RefreshGameObjectSearcher(false);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                TreeViewer.Update();

                MouseInspect.Update();
            }
        }

        internal void LateUpdate()
        {
            if (Show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void RefreshGameObjectSearcher(bool full)
        {
            bool GizmoFilter(GameObject o) => o.name.StartsWith(GizmoDrawer.GizmoObjectName);
            var gizmosExist = _gizmoDrawer != null && _gizmoDrawer.Lines.Count > 0;
            _gameObjectSearcher.Refresh(full, gizmosExist ? GizmoFilter : (Predicate<GameObject>)null);
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
        }

        public bool RemotingState
        {
            get => _remotingServer.isRunning;
            set
            {
                if (value)
                {
                    if (!_remotingServer.isRunning)
                        _remotingServer.Start();
                }
                else
                {
                    if (_remotingServer.isRunning)
                        _remotingServer.StopServer();
                }
            }
        }
        // END EDIT ------------------------------------------------------------------------------------------------------------------------

        #endregion

        #region[Helpers]

        private void SetWindowSizes()
        {
            const int screenOffset = 10;

            var screenRect = new Rect(
                screenOffset,
                screenOffset,
                Screen.width - screenOffset * 2,
                Screen.height - screenOffset * 2);

            var centerWidth = (int)Mathf.Min(850, screenRect.width);
            var centerX = (int)(screenRect.xMin + screenRect.width / 2 - Mathf.RoundToInt((float)centerWidth / 2));

            var inspectorHeight = (int)(screenRect.height / 4) * 3;
            Inspector.UpdateWindowSize(new Rect(
                centerX,
                screenRect.yMin,
                centerWidth,
                inspectorHeight));

            var rightWidth = 350;
            var treeViewHeight = screenRect.height;
            TreeViewer.UpdateWindowSize(new Rect(
                screenRect.xMax - rightWidth,
                screenRect.yMin,
                rightWidth,
                treeViewHeight));

            var replPadding = 8;
            Repl?.UpdateWindowSize(new Rect(
                centerX,
                screenRect.yMin + replPadding, //inspectorHeight + replPadding,
                centerWidth,
                screenRect.height - replPadding)); //inspectorHeight / 2 - replPadding));
        }

        #endregion
    }
}
