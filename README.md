# Runtime Unity Editor / Debugging Tools
In-game inspector, editor and interactive console for applications made with Unity3D game engine. It's designed for debugging and modding Unity games, but can also be used as a universal trainer.

### Features
- Works on most Unity games supported by [BepInEx](https://github.com/BepInEx/BepInEx)
- GameObject and component browser
- Object inspector that allows modifying values of objects in real time
- REPL C# console
- All parts are integrated together (e.g. REPL console can access inspected object, inspector can focus objects on GameObject list, etc.)
- Telnet Server (Functional Foundation WIP - basic features)
- IPC Named Pipes Server - Native Win32 API (Functional Foudation WIP - basic features)
- Extended Debug dumps for Game AppDomain and Types (i.e. Dumping methods, properties, events, etc. for each type)

![preview](https://user-images.githubusercontent.com/39247311/64476158-ce1a4c00-d18b-11e9-97d6-084452cdbf0a.PNG)

### How to use
<<<<<<< HEAD
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

### Credits:
Special thanks to ManlyMarco for his hard work in developing the original project this fork is based on. You can still support his work
through his Patreon page at: https://www.patreon.com/ManlyMarco
=======
1. Install BepInEx v4.x or v5.x if you don't have it already. You can download it [here](https://github.com/BepInEx/BepInEx
).
2. Download the latest build from the [Releases](https://github.com/ManlyMarco/RuntimeUnityEditor/releases) page. Make sure to get the correct version for your BepInEx.
3. Extract the BepInEx folder from the archive directly into your game directory (you should already have a BepInEx folder there from previous step). Replace files if asked.
4. To turn on press the F12 key when in-game. A window should appear on top of the game. If it doesn't appear, check logs for errors.

Note: If the plugin fails to load under BepInEx 4 with a type load exception, move RuntimeUnityEditor.Core.dll to BepInEx/core folder.

### How to build
1. Get Visual Studio 2019 (recommended) or the latest version of Visual Studio 2017.
2. Clone the repository recursively (`git clone --recursive https://github.com/ManlyMarco/RuntimeUnityEditor`). 
3. Open the solution in Visual Studio and hit Build All.

Notes:
- If you already have the repository cloned or want to update the mcs submodule you need to run `git submodule update --init --recursive` on your local repository.
- You have to reference UnityEngine.dll from Unity 5.x. The new UnityEngine.dll forwards all of the split types into their new respective dll files, therefore doing this allows runtime editor to run on any Unity version.

---

You can support development of my plugins through my Patreon page: https://www.patreon.com/ManlyMarco
>>>>>>> 0a9dab7beaa6ec526af4511ff4165769e0ca9153
