This folder contains source code of UnityModManager adaptation for WH40k:RT.
It's based on UMM version 0.25.0 but simplified and has references to PfLog class.
UMM sources are taken from https://github.com/newman55/unity-mod-manager/
All sources in this folder are compiled into UnityModManager.dll, which is placed
in StreamingAssets folder inside OwlcatUnityModManager.zip archieve.
This archieve also contains 2 other dll files: dnlib.dll and Ionic.Zip.dll.
The 2 dlls are required dependencies for UnityModManager.dll.

This is done to avoid possible conflic if someone will install official UnityModManager in the game.
In this case there could be 2 copies of all the 3 dlls in the AppDomain which will cause 
exception due to conflicts. 

Our UMM also has in-game built wrapper UnityModManagerBridge.cs.
This wrapper checks for Doorstop signs in application (which is the main sign of official UMM usage).
If official UMM is detected, our UMM won't be launched and no signs of it could be found in AppDomain.