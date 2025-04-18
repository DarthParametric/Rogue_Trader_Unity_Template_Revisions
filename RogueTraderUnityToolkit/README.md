Warhammer 40 000: Rogue Trader Mod Templalate tool only.
Should not be used inside WHRT Unity project.
Tools for inspecting and manipulating asset bundles from Rogue Trader. Currently, it can export all of the asset bundles as text. End goal: produce a working Unity project export. It fully supports asset bundles from 2022.3.7f1 (all other tools fail on at least a few assets). Heavy emphasis on performance. The entire game imports and processes in under 15 seconds on my machine. Exporting all assets to disk takes 80~ seconds (50gb on disk).

Entry point for developers is RogueTraderUnityToolkit\AssetServer\Program.cs
AssetServer compiles into console application. It's kinda local server for parsing asset bundles from RT game.
Starting the server requires 2 parameters: 
	- 1st is path to RT game root folder. AssetServer explores the contents of game build to be able to decompile bundles.
	- 2nd is path to Mod Template "Library" folder. AssetServer leavs a file inside Library called "BundleServerControl". This file contains process id of AssetServer.
	
By default AssetServer is started automatically from the Mod Template Bundle Scene Viewer.
See 'Modification Tools -> SceneViewer Window'. This opend a file tree tab in Unity Editor and launches AssetServer.
After AssetRipper is done initializing, SceneViewer will automatiacally connect to AssetServer. 
AssetRipper is implemented using named pipe. Closing SceneViewer window tab in Unity Editor will shut down AssetServer too.
Exiting Unity will shut down AssetServer too.

As for the Mod Templalate code, the develpor's entry point is BundleSceneServerStarter class and SceneViewGui class.
BundleSceneServerStarter incapsulates all the job of setting up and launching AssetServer. 
SceneViewGui calls BundleSceneServerStarter method EnsureBundleSceneServerStarted() as a part of Mod Templalate toolset initialization.

AssetServer project and all the other projects inside RogueTraderUnityToolkit solution can be compiled using backend
not earlier than .net 8.0 (Rider IDE virsion < 2023.3 won't fully work with the solution, because of .net 8.0 and c# 12)

Pro tip: don't run it through your IDE if you care about perf. It disables optimizations and it takes a gazillion times longer.
Credits: UnityDataTools for some types. AssetRipper and AssetStudio for references when I was learning about the file structures.

The original source of this toolset is taken from:
https://github.com/cstamford/RogueTraderUnityToolkit/
A huge thanks to Chris Stamford.

The original code doesn't have:
	- params to start AssetServer (path to game build was hard-coded).
	- linking with SceneViewer (SceneViewer is not present at the repo) (SceneViewer won't start AssetServer).
	- "BundleServerControl" file inside Library folder.
	

----------------------
Example object output:

```
$[MonoBehaviour]:m_GameObject:m_FileID$S32 = 0
$[MonoBehaviour]:m_GameObject:m_PathID$S64 = -650809873070036417
$[MonoBehaviour]:m_Enabled$U8 = 1
$[MonoBehaviour]:m_Script:m_FileID$S32 = 0
$[MonoBehaviour]:m_Script:m_PathID$S64 = -8990362273567350643
$[MonoBehaviour]:m_Name$String<0> = ""
$[MonoBehaviour]:m_Group:guid$String<32> = "a7045947f9a84d23a81d38e8a37287b7"
$[MonoBehaviour]:particleMap:m_FileID$S32 = 0
$[MonoBehaviour]:particleMap:m_PathID$S64 = 0
$[MonoBehaviour]:Data:Name$String<22> = "Locator_Torso_Upper_01"
$[MonoBehaviour]:Data:ParticleSize$F32 = 0.32
$[MonoBehaviour]:Data:LocalOffset:x$F32 = 0
$[MonoBehaviour]:Data:LocalOffset:y$F32 = 0
$[MonoBehaviour]:Data:LocalOffset:z$F32 = 0
$[MonoBehaviour]:Data:Rotate$U8 = 0
$[MonoBehaviour]:Data:CameraOffset$F32 = 0.23
$[MonoBehaviour]:Data:Flags$S32 = 0
$[MonoBehaviour]:bonesMap:m_FileID$S32 = 0
$[MonoBehaviour]:bonesMap:m_PathID$S64 = 0
```