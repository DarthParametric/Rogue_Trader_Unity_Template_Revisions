using Code.GameCore.Editor;
using Code.GameCore.Editor.Mods;
using Kingmaker.Utility.Serialization;
using Newtonsoft.Json;
using OwlcatModification.Editor.Utility;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.EditorMods
{
    public static class ScriptableObjectConfigsRepair
    {
        [MenuItem("Modification Tools/Repair RT configs")]
        public static void Repair()
        {
            #if OWLCAT_MODS
            RepairInternal();
            SharedStringAssetRepair.Repair();
            #endif
        }

        private static void RepairInternal()
        {
            const string cacheFileName = "RenderConfigsInfoCacheData";
            var guids = AssetDatabase.FindAssets(cacheFileName);
            if (guids == null || guids.Length != 1)
            {
                Debug.Log($"Error while trying to repair so configs. Couldn't find cache file {cacheFileName}.json");
                return;
            }

            var cacheFileGuid = guids[0];
            var cacheFilePath = AssetDatabase.GUIDToAssetPath(cacheFileGuid);
            Debug.Log($"{cacheFilePath}");

            if (!File.Exists(cacheFilePath))
            {
                Debug.Log($"Cache file not found at path: {cacheFilePath};");
                return;
            }

            var cacheFileContent = File.ReadAllText(cacheFilePath);

            if (string.IsNullOrEmpty(cacheFileContent))
            {
                Debug.Log($"Error while reading data from {cacheFilePath}");
                return;
            }
            
            JsonSerializer serializer = JsonSerializer.Create
            (new JsonSerializerSettings
                {Formatting = Formatting.Indented}
            );

            var soTransferData = serializer.DeserializeObject<ScriptableObjectTransferDataCache>(cacheFileContent);

            int count = 0;
            foreach (var data in soTransferData.Data)
            {
                TryFixScriptableObjectConfig(data);
                
                count++;
                if (count == 10)
                {
                    count = 0;
                    GC.Collect();
                }
            }
            GC.Collect();
            AssetDatabase.Refresh();
        }
        
        private static void TryFixScriptableObjectConfig(ScriptableObjectTransferData data)
        {
            var guids = AssetDatabase.FindAssets(data.FileName);
            string targetGuid = null;
            if (guids == null || guids.Length == 0)
            {
                Debug.Log($"Troubles finding config in project with name {data.FileName}. Skipping {data.FileName}...");
                return;
            }
            
            if (guids.Length > 1)
            {
                foreach (var guid in guids)
                {
                    var testPath = AssetDatabase.GUIDToAssetPath(guid);
                    var fileNameWithExtension = Path.GetFileName(testPath);
                    if (fileNameWithExtension.Equals($"{data.FileName}.asset", StringComparison.Ordinal))
                    {
                        Debug.Log($"Found asset: {testPath}");
                        if (string.IsNullOrEmpty(targetGuid))
                        {
                            targetGuid = guid;
                        }
                        else
                        {
                            Debug.Log($"Multiple .assets found for {data.FileName}. Skipping...");
                            return;
                        }
                    }
                }
            }
            else
            {
                targetGuid = guids[0];
            }

            var configPath = AssetDatabase.GUIDToAssetPath(targetGuid);
            if (!File.Exists(configPath))
            {
                Debug.LogError($"Troubles finding config in project with name {configPath}. Skipping {data.FileName}...");
                return;
            }

            Type configScriptType;
            try
            {
                configScriptType = Type.GetType(data.TypeName);
            }
            catch (Exception _)
            {
                Debug.LogError($"Error while trying to get type {data.TypeName}. Skipping {data.FileName}...");
                return;
            }

            if (configScriptType == null)
            {
                Debug.LogError($"Error while trying to get type {data.TypeName}. Skipping {data.FileName}...");
                return; 
            }

            string newGuid = ScriptsGuidUtil.GetScriptGuid(configScriptType);
            int newFileId = FileIDUtil.Compute(configScriptType);

            Debug.Log($"New {configScriptType.ToString()} guid is {newGuid}");
            if (string.IsNullOrEmpty(newGuid))
            {
                Debug.LogError($"Error fetching new guid for {configScriptType}. Skipping {data.FileName}...");
                return;
            }

            string newScriptUnityData = $"fileID: {newFileId}, guid: {newGuid}";
            string oldScriptUnityData = $"fileID: {data.OldFileId}, guid: {data.OldGuid}";
            
            var configContents = File.ReadAllText(configPath);
            if (string.IsNullOrEmpty(configContents))
            {
                Debug.LogError($"Error while reading config file. Skipping {data.FileName}...");
                return;
            }

            if (!configContents.Contains(oldScriptUnityData))
            {
                Debug.LogError($"Old fileId or Guid not found in config. Skipping {data.FileName}...");
                return;
            }
            
            configContents = configContents.Replace(oldScriptUnityData, newScriptUnityData);
            
            File.WriteAllText(configPath, configContents);
            Debug.Log($"Config {data.FileName} successfully repaired.");
        }
    }
}