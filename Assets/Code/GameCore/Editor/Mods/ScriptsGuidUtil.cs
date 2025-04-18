using System;
using UnityEditor;
using UnityEngine;

namespace OwlcatModification.Editor.Utility
{
    public static class ScriptsGuidUtil
    {
        public static string GetScriptGuid(Type type)
        {
            string returnValue = null;
            var assemblyName = type.Assembly.GetName().Name;
            Debug.Log($"Assembly name is {assemblyName}");
            var guids = AssetDatabase.FindAssets($"{assemblyName}", new[] { "Assets/RogueTraderAssemblies", "Assets/visual" });

            foreach (var guid in guids)
            {
                Debug.Log($"{AssetDatabase.GUIDToAssetPath(guid)}");
                var file = AssetDatabase.GUIDToAssetPath(guid);
                if (file.EndsWith(".dll"))
                {
                    Debug.Log(guid);
                    returnValue = guid;
                }
            }
            return returnValue;
        }
    }
}