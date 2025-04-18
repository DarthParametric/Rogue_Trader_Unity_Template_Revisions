using System.IO;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor
{
	public class ShadersModAggregator
	{
		[MenuItem("Tools/Mods/Aggregate Shaders From Project To One Folder")]
		private static void AggregateShaders()
		{
			AggregateShadersInternal();
		}

		/// <summary>
		/// Find all shaders in RT project inside Assets/ folder.
		/// Copy them to a separate folder Assets/../RtShaders
		/// for further ModTemplate setup.
		///
		/// There are more shaders in RT, for instance
		/// inside Assets/Plugins/External/
		/// (they are ignored because of possible legal issues,
		/// for instance Assets/Plugins/External/AStar
		/// and Assets/Plugins/External/2dxfx - used only for UI)
		/// and Packages/
		/// (they are copied manually because their includes are
		/// path sensitive).
		/// We need shaders for bundles parsing - maps and prefabs.
		/// </summary>
		public static void AggregateShadersInternal(bool auto = false)
		{
			//All folders in project that contain shader files.
			//Avoid copying everything from Assets/Plugins/External
			//because of possible legal issues. For example AStar.
			string[] shaderAssets = AssetDatabase.FindAssets("t:Shader t:shaderinclude t:subgraphasset", new[]
				{
					"Assets/FX",
					"Assets/Plugins/External/RootMotion",
					"Assets/Plugins/External/unity-ui-extensions",
					"Assets/UI",
					"Assets/Art",
					"Assets/Mechanics",
					"Assets/Code/Visual",
					"Assets/Code/Owlcat",
					"Assets/Code/UI",
					"Assets/UI/InGame/Unit/CombatHud/"
				});
			if (shaderAssets == null || shaderAssets.Length == 0)
			{
				Debug.LogError("Something went wrong getting shader assets info");
				return;
			}
			
			string defaultPath = Path.Combine( Application.dataPath, "../", "RtShaders");
			defaultPath = Path.GetFullPath(defaultPath);
			if (Directory.Exists(defaultPath))
			{
				var dirInfo = new DirectoryInfo(defaultPath);
				dirInfo.Delete(true);
			}
			Directory.CreateDirectory(defaultPath);

			foreach (string guid in shaderAssets)
			{
				string shaderPath = AssetDatabase.GUIDToAssetPath(guid);
				string shaderFileName = Path.GetFileName(shaderPath);
				string shaderFileDestination = Path.Combine(defaultPath, shaderFileName);
				if (File.Exists(shaderFileDestination))
				{
					if(!auto)
						Debug.LogError($"Shader file name collapse at {shaderPath} Skipping...");
					continue;
				}

				string shaderMetaPath = $"{shaderPath}.meta";
				string shaderMetaDestination = $"{shaderFileDestination}.meta";
				File.Copy(shaderPath, shaderFileDestination);
				File.Copy(shaderMetaPath, shaderMetaDestination);
				
			}
			
			if(!auto)
				EditorUtility.RevealInFinder(defaultPath);
			
			if(!auto)
				Debug.Log($"Finished shader aggregating for mod template. Total : {shaderAssets.Length} shaders");
		}
	}
}