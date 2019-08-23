# Runtime Unity Editor / Debugging Tools
In-game inspector, editor and interactive console for applications made with Unity3D game engine. It's designed for debugging and modding Unity games, but can also be used as an universal trainer.

### Features
- Works on most Unity games supported by BepInEx
- GameObject and component browser
- Object inspector that allows modifying values of objects in real time
- REPL C# console
- All parts are integrated together (e.g. REPL console can access inspected object, inspector can focus objects on GameObject list, etc.)
- Telnet Server (Functional Foundation WIP - basic features)
- IPC Named Pipes Server - Native Win32 API (Functional Foudation WIP - basic features)
- Extended Debug dumps for Game AppDomain and Types (i.e. Dumping methods, properties, events, etc. for each type)

![preview](https://user-images.githubusercontent.com/39247311/53586063-a8324000-3b87-11e9-8209-57e660d2949d.png)
![preview](https://user-images.githubusercontent.com/39247311/49837301-2d3a6400-fda6-11e8-961a-9a85f1247705.PNG)

### How to use
- This is a BepInEx plugin. It requires BepInEx v4 or later. Grab it from [here](https://github.com/BepInEx/BepInEx
) and follow installation instructions.
- Download the latest orignal builds from the [Releases](https://github.com/ManlyMarco/RuntimeUnityEditor/releases) page.
- To install place the .dll in the BepInEx directory inside your game directory (BepInEx/Plugins for BepInEx 5).
- To turn on press the F12 key when in-game. A window should appear on top of the game. If it doesn't appear, check logs for errors.
- To enable/disable the Telnet server press the F10 key when in-game.
- To enable/disable the IPC server press the F11 key when in-game.
- To enable/disable extended debug dumps edit RuntimeUnityEditor.cfg in in the BepInEx/Config folder, setting enableDebugDump to True/False. 
Extended logfiles will appear in the 'DEBUG_LOGS' folder within the main game folder.

Note: If the plugin fails to load under BepInEx 4 with a type load exception, move RuntimeUnityEditor.Core.dll to BepInEx/core folder.

### Telnet Server is hosted at 127.0.0.1:1755

### IPC Server Endpoint: "\\.\pipe\RUEPipe"