using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor
{
	public static class RenderConfigsInfoCache
	{
		[MenuItem("Tools/Mods/Cache Render Configs Info")]
		private static void CreateRenderConfigsCache()
		{
			CreateRenderConfigsCacheInternal(false);
		}

		public static void CreateRenderConfigsCacheInternal(bool auto = true)
		{
			string[] soList = AssetDatabase.FindAssets("t:ScriptableObject",
				new[] {"Assets/Resources", "Assets/RenderSettings", "Assets/Editor Default Resources" });
			if (soList == null || soList.Length == 0)
			{
				Debug.LogError("Something went wrong while creating render so cache");
				return;
			}

			var cache = new ScriptableObjectTransferDataCache();

			foreach (string guid in soList)
			{
				string soPath = AssetDatabase.GUIDToAssetPath(guid);
				string fileName = soPath.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
				
				if (string.IsNullOrEmpty(fileName))
				{
					Debug.LogError($"Failed to get file name of scriptableObject config {soPath}");
					continue;
				}

				var data = CreateScriptableObjectTransferData(soPath);
				if (data == null)
					continue;
				
				cache.Data.Add(data);
			}

			// PrepareRenderConfigsFromVisualPackageToMove(cache);
			
			string defaultPath = Path.Combine( Application.dataPath, "../" );
			defaultPath = Path.GetFullPath(defaultPath);
			JsonSerializer serializer = JsonSerializer.Create
			(new JsonSerializerSettings
				{Formatting = Formatting.Indented}
			);

			string materialShaderCacheFileName = "RenderConfigsInfoCacheData";
			string suggestedFileName = $"{materialShaderCacheFileName}.json";

			string autoModeDirectory = Path.Combine(Application.dataPath, "../", "ModTemplateCaches");
			string autoModeFilePath = Path.Combine(autoModeDirectory,suggestedFileName);
			
			if (auto)
			{
				if (!Directory.Exists(autoModeDirectory))
				{
					Directory.CreateDirectory(autoModeDirectory);
				}
				else if(File.Exists(autoModeFilePath))
				{
					File.Delete(autoModeFilePath);
				}
			}

			string selectedPath = auto 
				? autoModeDirectory
				: EditorUtility.OpenFolderPanel("Select Directory To Save",
					defaultPath,"");
			string finalFilePath = $"{Path.Combine(selectedPath, suggestedFileName)}";
			using (var fs = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var sw = new StreamWriter(fs))
			{
				serializer.Serialize(sw, cache);
				sw.Flush();
			}
			
			if(!auto)
				EditorUtility.RevealInFinder(finalFilePath);
			
		}

		private static ScriptableObjectTransferData CreateScriptableObjectTransferData(string assetPath)
		{
			string fileName = assetPath.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
			var scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
			string soType = scriptableObject.GetType().AssemblyQualifiedName;
			if (string.IsNullOrEmpty(soType))
			{
				Debug.LogError($"Error fetching AssemblyQualifiedName of {scriptableObject.GetType()}");
				return null;
			}
			var monoScript = MonoScript.FromScriptableObject(scriptableObject);
			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(monoScript,
				    out string scriptGuid, out long scriptFileId))
			{
				Debug.LogError($"Failed to get {soType} fileId and guid.");
			}
			
			var data = new ScriptableObjectTransferData(fileName, soType);
			if (!string.IsNullOrEmpty(scriptGuid))
			{
				data.OldGuid = scriptGuid;
				data.OldFileId = scriptFileId.ToString();
			}

			return data;
		}


		[MenuItem("Tools/Mods/Prepare Configs From Visual package")]
		public static void PrepareRenderConfigsFromVisualPackageToMove()
		{
			// PrepareRenderConfigsFromVisualPackageToMove(null);
		}

		/// <summary>
		/// Look through Owlcat.Visual package, search ScriptableObject configs,
		/// copy them to RenderSettingsFromGraphicsPackage folder near to Assets folder.
		/// Then cache ScriptableObjectTransferData to a given cache.
		/// </summary>
		/// <param name="dataCache"></param>
		private static void PrepareRenderConfigsFromVisualPackageToMove([CanBeNull] ScriptableObjectTransferDataCache dataCache)
		{
			string configsFolder = Path.Combine( Application.dataPath, "../", "RenderSettingsFromGraphicsPackage" );
			if (Directory.Exists(configsFolder))
			{
				var dirInfo = new DirectoryInfo(configsFolder);
				dirInfo.Delete(true);
			}
			Directory.CreateDirectory(configsFolder);

			string[] configsGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Packages/com.owlcat.visual" });
			var dataList = new List<ScriptableObjectTransferData>();
			string newNameSuffix = "FromPackage";
			foreach (string configGuid in configsGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(configGuid);
				if(!assetPath.EndsWith(".asset"))
					continue;
				//Rename all files to avoid conflict with files out of package
				string fileNameInYaml = Path.GetFileNameWithoutExtension(assetPath);
				string fileNameWithoutExtension = $"{Path.GetFileNameWithoutExtension(assetPath)}{newNameSuffix}";
				string fileName = $"{fileNameWithoutExtension}.asset";
				string fileDestination = Path.Combine(configsFolder, fileName);
				
				//File with duplicate name. Rename it one more
				if (File.Exists(fileDestination))
				{
					fileNameWithoutExtension = $"{fileNameWithoutExtension}1";
					fileName = $"{fileNameWithoutExtension}.asset";
					fileDestination = Path.Combine(configsFolder, fileName);
				}

				File.Copy(assetPath, fileDestination);
				File.Copy($"{assetPath}.meta", $"{fileDestination}.meta");
					
				//Fix name in yaml of asset
				var contents = File.ReadAllText(fileDestination);
				var internalFileName = $"m_Name: {fileNameInYaml}";
				// var internalFixedFileName = $"{fileNameWithoutExtension}1";
				if (contents.Contains(internalFileName))
				{
					contents = contents.Replace(internalFileName, $"m_Name: {fileNameWithoutExtension}");
				}
				File.WriteAllText(fileDestination, contents);
					
				var data = CreateScriptableObjectTransferData(assetPath);
				if (data != null)
				{
					data.FileName = fileNameWithoutExtension;
					dataList.Add(data);
				}
			}
			dataCache?.Data.AddRange(dataList);
		}
	}

	[Serializable]
	public class ScriptableObjectTransferDataCache
	{
		[SerializeField]
		public List<ScriptableObjectTransferData> Data = new();
	}

	[Serializable]
	public class ScriptableObjectTransferData
	{
		/// <summary>
		/// ScriptableObject config file name 
		/// </summary>
		[SerializeField]
		public string FileName;

		/// <summary>
		/// Assembly Qualified Name of class in ScriptableObject
		/// </summary>
		[SerializeField]
		public string TypeName;

		/// <summary>
		/// FileId of ScriptableObject's corresponding .cs file. 
		/// </summary>
		[SerializeField]
		public string OldFileId;

		/// <summary>
		/// GUID of ScriptableObject's corresponding .cs file. 
		/// </summary>
		[SerializeField]
		public string OldGuid;
		
		public ScriptableObjectTransferData(string fileName, string typeName)
		{
			FileName = fileName;
			TypeName = typeName;
		}
	}
}