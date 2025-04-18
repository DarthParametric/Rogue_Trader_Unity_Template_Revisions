using Kingmaker;
using Kingmaker.Localization;
using Kingmaker.Utility.Serialization;
using Newtonsoft.Json;
using OwlcatModification.Editor.Utility;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Code.GameCore.Editor.Mods
{
    public static class SharedStringAssetRepair
    {
        private static string OldSharedStringScriptGuid;
        private static long OldSharedStringScriptFileId;
        
        [MenuItem("Modification Tools/Repair SharedString configs")]
        public static void Repair()
        {
            #if OWLCAT_MODS
            ReadScriptData();
            
            if(string.IsNullOrEmpty(OldSharedStringScriptGuid))
                return;
            
            RepairInternal();
            AssetDatabase.Refresh();

            #endif 
        }

        private static void ReadScriptData()
        {
            const string cacheFileName = "SharedStringScriptData";
            var guids = AssetDatabase.FindAssets(cacheFileName);
            if (guids == null || guids.Length != 1)
            {
                Debug.Log($"Error while trying to repair so configs. Couldn't find cache file {cacheFileName}.json");
                return;
            }
            
            var cacheFileGuid = guids[0];
            var cacheFilePath = AssetDatabase.GUIDToAssetPath(cacheFileGuid);
            PFLog.Mods.Log($"{cacheFilePath}");

            if (!File.Exists(cacheFilePath))
            {
                PFLog.Mods.Log($"Cache file not found at path: {cacheFilePath};");
                return;
            }
            
            var cacheFileContent = File.ReadAllText(cacheFilePath);

            if (string.IsNullOrEmpty(cacheFileContent))
            {
                PFLog.Mods.Log($"Error while reading data from {cacheFilePath}");
                return;
            }
            
            JsonSerializer serializer = JsonSerializer.Create
            (new JsonSerializerSettings
                {Formatting = Formatting.Indented}
            );

            var scriptData = serializer.DeserializeObject<ScriptableObjectScriptData>(cacheFileContent);
            OldSharedStringScriptGuid = scriptData.Guid;
            OldSharedStringScriptFileId = scriptData.FileId;
        }

        private static void RepairInternal()
        {
            var sharedStringAssetsFolder = Path.Combine(Application.dataPath, "Mechanics", "Blueprints");
            
            var files = Directory.EnumerateFiles(sharedStringAssetsFolder, "*.asset", SearchOption.AllDirectories)
                .ToList();

            PFLog.Mods.Log($"Found {files.Count} SharedStringAsset files in project.");

            var newGuid = ScriptsGuidUtil.GetScriptGuid(typeof(SharedStringAsset));
            var newFileId = FileIDUtil.Compute(typeof(SharedStringAsset));

            var oldMetaString = $"fileID: {OldSharedStringScriptFileId}, guid: {OldSharedStringScriptGuid}";
            var newMetaString = $"fileID: {newFileId}, guid: {newGuid}";

            PFLog.Mods.Log($"New SharedStringAssets guid {newGuid}, fileId {newFileId}");

            Parallel.ForEach(files, 
                file =>
                {
                    RepairConfig(file, oldMetaString, newMetaString);
                });

        }

        private static void RepairConfig(string filePath, string oldMeta, string newMeta)
        {
            var contents = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(contents))
            {
                PFLog.Mods.Error($"Error while reading contents of {filePath}");
                return;
            }

            contents = contents.Replace(oldMeta, newMeta);
            File.WriteAllText(filePath, contents);
        }

    }
}