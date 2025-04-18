using Kingmaker.Modding;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Code.GameCore.EditorMods
{
	public class MaterialShaderInfoCache
	{
		[MenuItem("Tools/Mods/Cache Shader Info From Materials")]
		private static void CacheShaderInfo()
		{
			CacheShaderInfoInternal(false);
		}

		public static void CacheShaderInfoInternal(bool auto = true)
		{
			MaterialsShaderData data = new MaterialsShaderData();

			string[] guids = AssetDatabase.FindAssets("t:material");
			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if(assetPath.Contains("!Obsolete"))
					continue;
				string materialName = Path.GetFileNameWithoutExtension(assetPath);
				Material material = (Material)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Material));
				data.Data[materialName] = material.shader.name;
			}
			
			string defaultPath = Path.Combine( Application.dataPath, "../" );
			defaultPath = Path.GetFullPath(defaultPath);
			JsonSerializer serializer = JsonSerializer.Create
			(new JsonSerializerSettings
				{Formatting = Formatting.Indented}
			);

			string materialShaderCacheFileName = "MaterialShaderCache";
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
				: EditorUtility.
				OpenFolderPanel("Select Directory To Save",
					defaultPath,"");
			string finalFilePath = $"{Path.Combine(selectedPath, suggestedFileName)}";
			using (var fs = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var sw = new StreamWriter(fs))
			{
				serializer.Serialize(sw, data);
				sw.Flush();
			}
			
			if(!auto)
				EditorUtility.RevealInFinder(finalFilePath);
		}
	}
}
