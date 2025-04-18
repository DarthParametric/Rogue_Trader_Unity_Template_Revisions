using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintAreaCreator : AssetCreatorBase
	{
		protected virtual string MechanicsSceneTemplate => "Assets/Scenes/!Templates/Template_Mechanics.unity";
		private const string StaticSceneTemplate = "Assets/Scenes/!Templates/Template_Static.unity";
		private const string LightSceneTemplate = "Assets/Scenes/!Templates/Template_Light.unity";

		protected virtual string MechanicsScenePath => "Assets/Scenes/{folder}/{name}/{name}_Mechanics.unity";
		private const string StaticScenePath = "Assets/Scenes/{folder}/{name}/{name}_Static.unity";
		private const string LightScenePath = "Assets/Scenes/{folder}/{name}/{name}_Light.unity";

		protected virtual string DefaultEntranceSuffix
			=> "_Enter";

		private BlueprintEnterPointCreator m_EnterPointCreator;
		private BlueprintAreaPresetCreator m_PresetCreator;
		private AreaPartBoundsCreator m_BoundsCreator;

		public override string CreatorName => "Area";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Areas/{folder}/{name}/{name}.asset";
		
        public override object CreateAsset()
        {
            return new BlueprintArea();
        }
		public override void PostProcess(object asset)
        {
            var area = (BlueprintArea)asset;

			var mechanicsPath = GetAssetPath(MechanicsScenePath, area.name);
			var staticPath = GetAssetPath(StaticScenePath, area.name);
			var lightPath = GetAssetPath(LightScenePath, area.name);

			m_EnterPointCreator = m_EnterPointCreator ? m_EnterPointCreator : CreateInstance<BlueprintEnterPointCreator>();
			m_EnterPointCreator.Area = area.ToReference<BlueprintAreaReference>();

			var enterPoint =
				NewAssetWindow.CreateWithCreator(m_EnterPointCreator, area.name + DefaultEntranceSuffix,
					Folder) as BlueprintAreaEnterPoint;
			
			m_BoundsCreator = m_BoundsCreator ? m_BoundsCreator : CreateInstance<AreaPartBoundsCreator>();
			m_BoundsCreator.Area = area.ToReference<BlueprintAreaReference>();

			var bounds = NewAssetWindow.CreateWithCreator(m_BoundsCreator, area.name + "_Bounds",
				Folder) as AreaPartBounds;
			area.Bounds = bounds;

			Directory.CreateDirectory(Path.GetDirectoryName(mechanicsPath));
			Directory.CreateDirectory(Path.GetDirectoryName(staticPath));
			AssetDatabase.CopyAsset(MechanicsSceneTemplate, mechanicsPath);
			AssetDatabase.CopyAsset(StaticSceneTemplate, staticPath);
			AssetDatabase.CopyAsset(LightSceneTemplate, lightPath);
			EditorSceneManager.OpenScene(mechanicsPath, OpenSceneMode.Single);
			EditorSceneManager.OpenScene(staticPath, OpenSceneMode.Additive);
			EditorSceneManager.OpenScene(lightPath, OpenSceneMode.Additive);
			var mechanicsScene = SceneManager.GetSceneByPath(mechanicsPath);

			foreach (var go in mechanicsScene.GetRootGameObjects())
			{
				go.GetComponentsInChildren<AreaEnterPoint>().ForEach(p => p.Blueprint = enterPoint);
			}

			area.DynamicScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(mechanicsPath));
			area.StaticScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(staticPath));

			m_PresetCreator = m_PresetCreator ? m_PresetCreator : CreateInstance<BlueprintAreaPresetCreator>();
			m_PresetCreator.Area = area.ToReference<BlueprintAreaReference>();

			var preset =
				NewAssetWindow.CreateWithCreator(m_PresetCreator, "Default",
					Folder) as BlueprintAreaPreset;
			if (preset != null)
			{
				preset.Area = area;
				preset.EnterPoint = enterPoint;
				area.DefaultPreset = preset;
			}


			var pathfinder = AstarPath.active;
			if (pathfinder != null)
			{
				pathfinder.data.file_cachedStartup = CreateEmptyNavmeshCacheFile();
				EditorUtility.SetDirty(pathfinder);
			}
			EditorSceneManager.SaveScene(mechanicsScene);

	        #if !OWLCAT_MODS
			area.FixLightScenes();
			#endif
	        
			var scenes = new List<SceneReference> { area.DynamicScene, area.StaticScene };
			scenes.AddRange(area.LightScenes);
			scenes = scenes.Distinct().ToList();

			var buildScenes = scenes
				.Select(s => s.ScenePath)
				.Where(p => EditorBuildSettings.scenes.All(s => s.path != p))
				.Select(p => new EditorBuildSettingsScene(p, true))
				.ToArray();

			EditorBuildSettings.scenes =
				EditorBuildSettings.scenes.Concat(buildScenes).ToArray();

			enterPoint.SetDirty();
			EditorUtility.SetDirty(bounds);
			preset.SetDirty();
			area.SetDirty();

			AssetDatabase.SaveAssets();
            BlueprintsDatabase.SaveAllDirty();
			Selection.activeObject = BlueprintEditorWrapper.Wrap(area);

		}

		private TextAsset CreateEmptyNavmeshCacheFile()
		{
			string scenePath = SceneManager.GetActiveScene().path;
			
			string sceneName = SceneManager.GetActiveScene().name;
			int underTypeIndex = sceneName.LastIndexOf("_", StringComparison.Ordinal);
			if (underTypeIndex > 0)
				sceneName = sceneName.Substring(0, underTypeIndex);
			
			int underscoreIndex = scenePath.LastIndexOf("/", StringComparison.Ordinal);
			if (underscoreIndex > 0)
				scenePath = scenePath.Substring(0, underscoreIndex);
			
			scenePath += "/Navmesh/";
			Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
			
			scenePath += sceneName + ".bytes";
			var path = AssetDatabase.GenerateUniqueAssetPath(scenePath);
			
			var fileInfo = new FileInfo(path);
			if (fileInfo.Exists && fileInfo.IsReadOnly)
				fileInfo.IsReadOnly = false;
			
			File.WriteAllBytes(path,Array.Empty<byte>());

			AssetDatabase.Refresh();
			return AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
		}

		string GetAssetPath(string template, string name)
		{
			return template.Replace("{name}", name).Replace("{folder}", Folder);
		}
	}
}