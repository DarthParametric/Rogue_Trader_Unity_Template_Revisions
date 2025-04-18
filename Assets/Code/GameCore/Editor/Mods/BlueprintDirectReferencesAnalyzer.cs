using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.Editor.Utility;
using Kingmaker.ResourceLinks;
using Kingmaker.Utility.CodeTimer;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utility;
using OwlcatModification.Editor.Utility;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor.Mods
{
	public class BlueprintDirectReferencesAnalyzer
	{
		public BlueprintUnityObjectReferenceInfoList DataList = new();
		public BlueprintUnityObjectReferenceInfoList SharedStringList = new();

		private long m_SharedStringAssetFileId = 11400000L;
		private JsonSerializer m_Serializer = JsonSerializer.Create
		(new JsonSerializerSettings
			{Formatting = Formatting.Indented}
		);

		private const string SaveFileName = "BlueprintUnityObjectReferences"; 
		private const string StringsSaveFileName = "BlueprintSharedStringsReferences"; 
		private string SuggestedFileName = $"{SaveFileName}.json";
		private string SuggestedStringsFileName = $"{StringsSaveFileName}.json";
		private string SuggestedScriptDataFileName = "SharedStringScriptData.json";
		private const string MetaFilesArchive = "BlueprintReferencedAssetsMetaFiles";
		
		[MenuItem("Tools/Mods/Cache Blueprint References Info")]
		private static void CreateReferencesCache()
		{
			CreateReferencesCacheInternal();
		}

		public static void CreateReferenceCache(bool auto = false)
		{
			CreateReferencesCacheInternal();
		}

		private static void CreateReferencesCacheInternal(bool auto = false)
		{
			var analyzer = new BlueprintDirectReferencesAnalyzer();
			analyzer.AnalyzeLibrary();
		}

		public void AnalyzeLibrary(bool auto = false)
		{
			DataList = new BlueprintUnityObjectReferenceInfoList();
			SharedStringList = new BlueprintUnityObjectReferenceInfoList();
			List<(string Guid, string Path, bool isShadowDeleted, bool ContainsShadowDeletedBlueprints)> ps;
			using (ProfileScope.New("search"))
				ps = BlueprintsDatabase.SearchByType(typeof(SimpleBlueprint));
            
			var pw = new ProgressWrapper<(string, string, bool, bool)>(ps, "Analyzing blueprint dependencies");
			foreach (var p in pw)
			{
				SimpleBlueprint bp;
				using (ProfileScope.New("load"))
					bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(p.Item1);

				if (bp == null)
				{
					PFLog.Build.Error($"Failed to load blueprint with {p.Item1} for analysis. Possible duplicate IDs!");
					continue;
				}
                
				pw.Info = bp.name;
				using (ProfileScope.New("one blueprint"))
				{
					AnalyzeBlueprint(bp);
				}
			}
			SaveDataToFiles(auto);
		}

		private void SaveDataToFiles(bool auto = false)
		{
			string autoModeDirectory = Path.Combine(Application.dataPath, "../", "ModTemplateCaches");
			string autoModeMetaDirectory = Path.Combine(autoModeDirectory, MetaFilesArchive);
			string autoModeFilePath = Path.Combine(autoModeDirectory, SuggestedFileName);
			string autoModeStringsFilePath = Path.Combine(autoModeDirectory, SuggestedStringsFileName);
			string autoModsScriptDataFilePath = Path.Combine(autoModeDirectory, SuggestedScriptDataFileName);
			
			if (!Directory.Exists(autoModeDirectory))
				Directory.CreateDirectory(autoModeDirectory);

			if (!Directory.Exists(autoModeMetaDirectory))
				Directory.CreateDirectory(autoModeMetaDirectory);

			if(File.Exists(autoModeFilePath))
				File.Delete(autoModeFilePath);
			

			if (File.Exists(autoModeStringsFilePath))
				File.Delete(autoModeStringsFilePath);
			

			if (File.Exists(autoModsScriptDataFilePath))
				File.Delete(autoModsScriptDataFilePath);
			

			if (DataList == null || DataList.dataList.Count <= 0)
			{
				PFLog.Mods.Error($"Empty DataList while caching blueprint references. Something went completely wrong");
			}
			else
			{
				using (var fs = File.Open(autoModeFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var sw = new StreamWriter(fs))
				{
					m_Serializer.Serialize(sw, DataList);
					sw.Flush();
				}
				
				CreateMetaFilesArchive(autoModeMetaDirectory);
			}
			
			

			if (SharedStringList == null || SharedStringList.dataList.Count <= 0)
			{
				PFLog.Mods.Error($"Empty SharedStringList while caching blueprint strings data. Something went completely wrong");
			}
			else
			{
				//Write only script data (fileId and Guid). We don't need all SharedString info
				//at the moment of fixing SharedString scriptableObjects in mod template.
				//So we must avoid reading SharedString info data as it's huge.
				var randomSharedString = SharedStringList.dataList[0];
				var scriptData = new ScriptableObjectScriptData(randomSharedString.ScriptGuid, randomSharedString.ScriptFileId);
				
				using (var fs = File.Open(autoModsScriptDataFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var sw = new StreamWriter(fs))
				{
					m_Serializer.Serialize(sw, scriptData);
					sw.Flush();
				}
				
				//Write all the SharedString info data about all SharedStrings in project.`
				using (var fs = File.Open(autoModeStringsFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				using (var sw = new StreamWriter(fs))
				{
					m_Serializer.Serialize(sw, SharedStringList);
					sw.Flush();
				}
			}

			if(!auto && File.Exists(autoModeFilePath))
				EditorUtility.RevealInFinder(autoModeFilePath);
			else if(!auto && File.Exists(autoModeStringsFilePath))
				EditorUtility.RevealInFinder(autoModeStringsFilePath);		
		}

		private void CreateMetaFilesArchive(string pathToFolder)
		{
			foreach (var dataItem in DataList.dataList)
			{
				string pathToFile = dataItem.ObjectPath;
				
				//We are interested only in textures
				if (!pathToFile.EndsWith(".jpg") && !pathToFile.EndsWith(".png"))
					continue;
				
				string fileName = $"{Path.GetFileName(pathToFile)}.meta";
				string source = $"{pathToFile}.meta";
				string destination = Path.Combine(pathToFolder, fileName);
				if (File.Exists(destination))
				{
					PFLog.Mods.Error($"File {destination} already exists. Skipping.");
					continue;
				}
				File.Copy(source, destination, false);
			}
			
			ZipFile.CreateFromDirectory(pathToFolder, Path.Combine(pathToFolder, "../", $"{MetaFilesArchive}.zip"));
			if (Directory.Exists(pathToFolder))
				Directory.Delete(pathToFolder, true);
		}

		public void AnalyzeBlueprint(SimpleBlueprint bp)
		{
			if (bp?.GetType().HasAttribute<ExcludeBlueprintAttribute>()??true)
				return; // skip clockwork
            
			//AnalyzeObject(BlueprintEditorWrapper.Wrap(bp));
			using (ProfileScope.New("analyze")) 
				AnalyzeWithReflection(bp, bp.name);
		}
		
		public void AnalyzeWithReflection(object obj, string source="")
		{
			BlueprintFieldsTraverser.Traverse(obj,
				(field, value, index) =>
				{
					if (value is not WeakResourceLink wrl)
					{
						if (value is UnityEngine.Object uobj)
						{
							//TODO: path to asset to get .meta for .jpg, .png
							string path = AssetDatabase.GetAssetPath(uobj);
							string bpName = ((SimpleBlueprint)obj).name;
							string bpGuid = ((SimpleBlueprint)obj).AssetGuid;

							string scriptGuid = null;
							long scriptFileId = 0;
							if (uobj is ScriptableObject scriptableObject)
							{
								var monoScript = MonoScript.FromScriptableObject(scriptableObject);
								AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript,
									out scriptGuid, out scriptFileId);
							}

							AssetDatabase.TryGetGUIDAndLocalFileIdentifier(uobj, out string testGuid, out long testFileId);
							var info = new BlueprintUnityObjectReferenceInfo(
								bpName, bpGuid,
								field.Name, path, testGuid,
								testFileId, scriptGuid, scriptFileId);
							
							if(testFileId == m_SharedStringAssetFileId)
								SharedStringList.Add(info);
							else
								DataList.Add(info);
						}

						return true;
					}

					return false;
				});
		}
	}
}