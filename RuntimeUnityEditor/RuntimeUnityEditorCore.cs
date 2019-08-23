using System;
using System.IO;
using RuntimeUnityEditor.Core.Gizmos;
using RuntimeUnityEditor.Core.ObjectTree;
using RuntimeUnityEditor.Core.REPL;
using RuntimeUnityEditor.Core.Debugging;
using RuntimeUnityEditor.Core.Networking;
using RuntimeUnityEditor.Core.Networking.IPCServer;
using UnityEngine;

namespace RuntimeUnityEditor.Core
{
    public class RuntimeUnityEditorCore
    {
        public const string Version = "1.5.0.1";
        public const string GUID = "RuntimeUnityEditor";

        public Inspector.Inspector Inspector { get; }
        public ObjectTreeViewer TreeViewer { get; }
        public ReplWindow Repl { get; }
        
        public KeyCode ShowHotkey { get; set; } = KeyCode.F12;

        internal static RuntimeUnityEditorCore Instance { get; private set; }
        internal static MonoBehaviour PluginObject { get; private set; }
        internal static ILoggerWrapper Logger { get; private set; }

        internal static GizmoDrawer GizmoDrawer { get; private set; }

        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;

        //Wh010ne Fork -----------------------------------------------------------------------------------------------------------
        public TCPServer _tcpServer = new TCPServer();
        public static bool _enableDebug { get; set; }

        public KeyCode IPCOnOff { get; set; } = KeyCode.F11;
        public KeyCode TelnetOnOff { get; set; } = KeyCode.F10;
        //END EDIT ---------------------------------------------------------------------------------------------------------------

        public RuntimeUnityEditorCore(MonoBehaviour pluginObject, ILoggerWrapper logger, bool enableDebugDump)
        {
            if (Instance != null)
                throw new InvalidOperationException("Can only create one instance of the Core object");

            PluginObject = pluginObject;
            Logger = logger;
            Instance = this;

            //Wh010ne Fork -----------------------------------------------------------------------------------------------------------
            
            _enableDebug = enableDebugDump;
            if (_enableDebug) { Logger.Log(LogLevel.Info, "Enabling enhanced debug logging. Load time will be affected!"); DebugHelpers.SetDebugPath(); }

            //Auto-start Networking Servers
            //if (!_tcpServer.isRunning)
                //_tcpServer.Start();

            //if (!ServerUtils.isRunning)
                //ServerUtils.StartServer();

            //END EDIT ---------------------------------------------------------------------------------------------------------------

            Inspector = new Inspector.Inspector(targetTransform => TreeViewer.SelectAndShowObject(targetTransform));

            TreeViewer = new ObjectTreeViewer(pluginObject);
            TreeViewer.InspectorOpenCallback = items =>
            {
                Inspector.InspectorClear();
                foreach (var stackEntry in items)
                    Inspector.InspectorPush(stackEntry);
            };

            if (Utils.UnityFeatureHelper.SupportsVectrosity)
            {
                GizmoDrawer = new GizmoDrawer(pluginObject);
                TreeViewer.TreeSelectionChangedCallback = transform => GizmoDrawer.UpdateState(transform);
            }

            if (Utils.UnityFeatureHelper.SupportsCursorIndex && Utils.UnityFeatureHelper.SupportsXml)
            {
                try
                {
                    Repl = new ReplWindow();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, "Failed to load REPL - " + ex.Message);
                }
            }
        }

        public void OnGUI()
        {
            if (Show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Inspector.DisplayInspector();
                TreeViewer.DisplayViewer();
                Repl?.DisplayWindow();
            }
        }

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
                        Cursor.lockState = _previousCursorLockState;
                        Cursor.visible = _previousCursorVisible;
                    }
                }

                TreeViewer.Enabled = value;

                if (GizmoDrawer != null)
                {
                    GizmoDrawer.Show = value;
                    GizmoDrawer.UpdateState(TreeViewer.SelectedTransform);
                }

                if (value)
                {
                    SetWindowSizes();

                    TreeViewer.UpdateCaches();
                }
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
            //END EDIT ---------------------------------------

            Inspector.InspectorUpdate();
        }

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
                screenRect.yMin + inspectorHeight + replPadding,
                centerWidth,
                screenRect.height - inspectorHeight - replPadding));
        }

        //Wh010ne Fork --------------------------------------------------------------------------------------------------------------------
        
        public bool TelnetState
        {
            get => _tcpServer.isRunning;
            set
            {
                if (value)
                {
                    _tcpServer.StartTCPServer();
                }
                else
                {
                    _tcpServer.StopTCPServer();
                }
            }
        }

        public bool IPCState
        {
            get => ServerUtils.isRunning;
            set
            {
                if(value)
                {
                    ServerUtils.StartServer();
                }
                else
                {
                    ServerUtils.StopServer();
                }
            }
        }

        //END EDIT ------------------------------------------------------------------------------------------------------------------------
    }
}
