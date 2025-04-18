using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace OwlcatModification.Editor
{
    public static class CreateModificationHelper
    {
        [MenuItem("Assets/Modification Tools/Create Blueprints' Patches Config")]
        private static void CreateBlueprintPatchedConfig()
        {
            var newModification = ScriptableObject.CreateInstance<BlueprintPatches>();
            string selectedPath;

            var folderSearchResult = TryGetActiveFolderPath(out selectedPath);
            if (folderSearchResult)
            {
                selectedPath = $"{Application.dataPath}/{selectedPath.Split("Assets/")[1]}";
            }
            else
            {
                selectedPath = EditorUtility.OpenFolderPanel("Select Directory To Save",
                    Application.dataPath, "");

                if (!selectedPath.Contains("Assets"))
                {
                    Debug.LogError("Save path must be inside Assets folder.");
                    return;
                }
            }

            var pathInsideAssets = selectedPath.Split("Assets/")[1];
            var modAssetPath =  $"Assets/{pathInsideAssets}/NewBlueprintPatches.asset";
            AssetDatabase.CreateAsset(newModification, modAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newModification;
        }

        [MenuItem("Assets/Modification Tools/Create Mod Directory")]
        private static void CreateModDirectory()
        {
            var newModification = ScriptableObject.CreateInstance<Modification>();
            string selectedPath;

            var folderSearchResult = TryGetActiveFolderPath(out selectedPath);
            if (folderSearchResult)
            {
                selectedPath = $"{Application.dataPath}/{selectedPath.Split("Assets/")[1]}";
            }
            else
            {
                selectedPath = EditorUtility.OpenFolderPanel("Select Directory To Save",
                    Application.dataPath, "");

                if (!selectedPath.Contains("Assets"))
                {
                    Debug.LogError("Save path must be inside Assets folder.");
                    return;
                }
            }

            var pathInsideAssets = selectedPath.Split("Assets/")[1];
            var modAssetPath =  $"Assets/{pathInsideAssets}/NewModification.asset";
            Debug.Log($"Created Mod Asset at path: {modAssetPath}");
            AssetDatabase.CreateAsset(newModification, modAssetPath);
            Directory.CreateDirectory(Path.Combine(selectedPath, "Scripts"));
            Directory.CreateDirectory(Path.Combine(selectedPath, "Content"));
            Directory.CreateDirectory(Path.Combine(selectedPath, "Blueprints"));
            Directory.CreateDirectory(Path.Combine(selectedPath, "Localization"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newModification;
        }
        
        private static bool TryGetActiveFolderPath( out string path )
        {
            var _tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod( "TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic );

            object[] args = new object[] { null };
            bool found = (bool)_tryGetActiveFolderPath.Invoke( null, args );
            path = (string)args[0];

            return found;
        }
    }
}