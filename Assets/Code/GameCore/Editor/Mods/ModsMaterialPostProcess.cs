using Kingmaker.Modding;
using Kingmaker.Utility.Serialization;
using Kingmaker.Utility.UnityExtensions;
using Newtonsoft.Json;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.EditorMods
{
	public static class ModsMaterialPostProcess
	{
		private static MaterialsShaderData s_MaterialsData;
		
		
		[MenuItem("Assets/Modification Tools/Try fix material")]
		private static void OpenMaterialEdit(MenuCommand menuCommand)
		{
			#if OWLCAT_MODS
			foreach (var material in Selection.objects)
			{
				if (material is Material mat)
				{
					TryToFixMaterial(mat);
				}
			}
			#endif
		}
		
		private static void TryToFixMaterial(Material material)
		{
			if (s_MaterialsData != null && s_MaterialsData.Data != null && s_MaterialsData.Data.Count > 0)
			{
				RestoreShaderByCache(material);
				return;
			}

			string fileName = "MaterialShaderCache";
			string[] searchResult = AssetDatabase.FindAssets($"{fileName}");
			if (searchResult == null || searchResult.Length != 1)
				throw new Exception("Materials shader info file not found. Please copy it from the main project");
			
			string fileGuid = AssetDatabase.FindAssets($"{fileName}")[0];
			if (fileGuid.IsNullOrEmpty())
				throw new Exception("Materials shader info file not found. Please copy it from the main project");
			string path = AssetDatabase.GUIDToAssetPath(fileGuid);
			if (!File.Exists(path))
				throw new Exception($"{fileName} not found by guid {fileGuid}");
			
			string content = File.ReadAllText(path);
			JsonSerializer serializer = JsonSerializer.Create
			(new JsonSerializerSettings
				{Formatting = Formatting.Indented}
			);

			s_MaterialsData = serializer.DeserializeObject<MaterialsShaderData>(content);
			if (s_MaterialsData == null)
				throw new Exception($"Failed to deserialize Materials Data");
			
			RestoreShaderByCache(material);
		}

		private static void RestoreShaderByCache(Material material)
		{
			string materialName = material.name;

			if (material.shader == null)
			{
				Debug.LogError($"{materialName} shader is null");
				return;
			}

			var shader = Shader.Find(s_MaterialsData.Data[materialName]);
			if (shader == null)
			{
				Debug.LogError($"Failed to find shader for {s_MaterialsData.Data[materialName]}");
				return;
			}

			material.shader = shader;
			Debug.Log($"{materialName} successfully fixed to using shader {s_MaterialsData.Data[materialName]}");

		}
	}
}