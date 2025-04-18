using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
	public static class AssetPathUtility
	{
        public static string GetFilePath(ScriptableObject asset)
        {
            string rootPath = Application.dataPath.Replace("Assets", "");
            string assetPath = AssetDatabase.GetAssetPath(asset);
            return Path.Combine(rootPath, assetPath).Replace("\\", "/");
        }
        public static string GetFilePath(SimpleBlueprint asset)
        {
            string assetPath = BlueprintsDatabase.GetAssetPath(asset);
            return new FileInfo(assetPath).FullName.Replace("\\","/");
        }
        public static void EnsurePathExists(string path)
        {
            if (!Directory.Exists(path))
            {
                var parent = Path.GetDirectoryName(path);
                if(parent!=null)
                    EnsurePathExists(parent);
                Directory.CreateDirectory(path);
            }
        }
	}
}