using Kingmaker.Utility.EditorPreferences;
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Editor.BundleSceneViewerStarter
{

    [InitializeOnLoad]
    public class EditorQuitHandler
    {
        static EditorQuitHandler()
        {
            EditorApplication.quitting += BeforeQuit;
        }

        private static void BeforeQuit()
        {
            BundleSceneServerStarter.KillServer();
        }
    }
    
    public class BundleSceneServerStarter
    {
        private const string LibraryPath = "Library";
        private const string ControlFileName = "BundleServerControl";

        private static string GetServerExecutablePath()
        {
            #if UNITY_EDITOR_WIN
            return @"RogueTraderUnityToolkit\out\AssetServer.exe";
            //return @"RogueTraderUnityToolkit\AssetServer\bin\Debug\net8.0\AssetServer.exe";
            #elif UNITY_EDITOR_OSX
            throw new NotSupportedException("Platform other than Windows is not supported.");
            #else
            throw new NotSupportedException("Platform other than Windows or MacOS is not supported.");
            #endif
        }

        [MenuItem("Modification Tools/ Configure Game Path")]
        private static void ConfigureGamePath()
        {
            var selectedPath = EditorUtility.OpenFolderPanel("Select Game Installation Folder", "", "");
            EditorPreferences.Instance.ModsGameBuildPath = selectedPath;
            EditorPreferences.Instance.Save();
        }
        
        public static void EnsureBundleSceneServerStarted()
        {
            if (GetServerProcess() != null)
                return;
            if (!GamePathConfigured())
                ConfigureGamePath();
            StartServerProcess();
        }

        private static bool GamePathConfigured()
        {
            var gamePath = EditorPreferences.Instance.ModsGameBuildPath;
            if (string.IsNullOrEmpty(gamePath))
                return false;
            if (!Directory.Exists(gamePath))
                return false;
            return true;
        }

        public static void KillServer()
        {
            var process = GetServerProcess();
            process?.Kill();
            var pipePath = Path.Combine(LibraryPath, ControlFileName);
            if (File.Exists(pipePath))
            {
                File.Delete(pipePath);
            }
        }

        [MenuItem("Modification Tools/ Restart Bundle Scene Server")]
        public static void RestartServerProcess()
        {
            KillServer();
            StartServerProcess();
        }

        [MenuItem("Modification Tools/ Start Bundle Scene Server")]
        public static void StartServerProcess()
        {
            var controlFilePath = Path.Combine(LibraryPath, ControlFileName);
            if (File.Exists(controlFilePath))
            {
                Debug.LogError("An instance of Bundle Scene Server is already running.");
                return;
            }
            var pathToGame = EditorPreferences.Instance.ModsGameBuildPath;
            if (string.IsNullOrEmpty(pathToGame))
            {
                Debug.LogError("Path to game is not configured. Set path to game first before starting the server.");
                return;
            }

            if (!Directory.Exists(pathToGame))
            {
                Debug.LogError($"Couldn't find game files in cached game path: {pathToGame}. Please, re-configure the game path.");
                return;
            }
            
            // run server process
            Debug.Log("Starting bundle scene viewer server process");

            ProcessWindowStyle windowStyle = EditorPreferences.Instance.BlueprintIndexingServerProcessWindowStyle;
            var pathToServer = GetServerExecutablePath();
            if (!File.Exists(pathToServer))
            {
                pathToServer = GetServerExecutablePath();
                if (!File.Exists(pathToServer))
                {
                    Debug.LogError($"AssetServer exec file not found neither " +
                                        $"on regular path {GetServerExecutablePath()}");
                    return;
                }
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(pathToServer)
            {
                WindowStyle = windowStyle,
                Arguments = $"\"{pathToGame}\" \"{LibraryPath}\"",
            };
            Process.Start(startInfo);
        }
        
        private static Process GetServerProcess()
        {
            var pipePath = Path.Combine(LibraryPath, ControlFileName);
            if (!File.Exists(pipePath))
            {
                // there is no server mark file, thus no server process
                return null;
            }
            else
            {
                // there is a mark file, but the process MAY have been terminated already
                // and failed to remove the mark. The mark should have the process ID, lets
                // try to find it
                int pid;
                using (var sr = new StreamReader(pipePath))
                {
                    int.TryParse(sr.ReadLine(), out pid);
                }

                try
                {
                    var process = Process.GetProcessById(pid);
                    if(process.HasExited) // why does GetProcessById return exited process??
                    {
                        File.Delete(pipePath);
                        return null;
                    }
                    return process;
                }
                catch (ArgumentException)
                {
                    // Process not found. Delete the mark file and start a new server
                    File.Delete(pipePath);
                }
            }

            return null;
        }
    }
}
