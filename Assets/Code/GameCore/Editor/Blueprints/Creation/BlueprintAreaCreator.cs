using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.Creation.Naming;
using Kingmaker.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.EditorPreferences;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintAreaCreator : NamingCreatorBase
	{
		private const string MechanicsScenePostfix = "_Mechanics.unity";
		private const string StaticScenePostfix = "_Static.unity";
		private const string LightScenePostfix = "_Light.unity";

		private const string ScenesRoot = "Assets/Scenes/";
		private const string TemplateScenesPrefix = ScenesRoot + "!Templates/Template";

		private const string DefaultMechanicsTemplateScenePath = TemplateScenesPrefix + MechanicsScenePostfix;
		private const string DefaultStaticTemplateScenePath = TemplateScenesPrefix + StaticScenePostfix;
		private const string DefaultLightTemplateScenePath = TemplateScenesPrefix + LightScenePostfix;

		protected override string NameTokenNotEmpty
			=> "_" + NameToken;

		protected virtual string MechanicsTemplateScenePath
			=> Templates.instance == null
				? DefaultMechanicsTemplateScenePath
				: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.MechanicsTemplateScene);

		private static string StaticTemplateScenePath
			=> Templates.instance == null
				? DefaultStaticTemplateScenePath
				: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.StaticTemplateScene);

		private static string LightTemplateScenePath
			=> Templates.instance == null
				? DefaultLightTemplateScenePath
				: AssetDatabase.GetAssetPath(Templates.instance.BlueprintArea.LightTemplateScene);

		private const string DefaultSceneTemplate = ScenesRoot + "{Chapter}/{Location}/{Location}{name}";
		protected virtual string DefaultMechanicsSceneTemplate => DefaultSceneTemplate + MechanicsScenePostfix;
		private const string DefaultStaticSceneTemplate = DefaultSceneTemplate + StaticScenePostfix;
		private const string DefaultLightSceneTemplate = DefaultSceneTemplate + LightScenePostfix;

		protected virtual string MechanicsSceneTemplate
			=> Templates.instance == null
				? DefaultMechanicsSceneTemplate
				: Templates.instance.BlueprintArea.MechanicsSceneTemplate;

		private static string StaticSceneTemplate
			=> Templates.instance == null
				? DefaultStaticSceneTemplate
				: Templates.instance.BlueprintArea.StaticSceneTemplate;

		private static string LightSceneTemplate
			=> Templates.instance == null
				? DefaultLightSceneTemplate
				: Templates.instance.BlueprintArea.LightSceneTemplate;

		protected virtual string DefaultEntranceSuffix
			=> "Enter";

		private BlueprintEnterPointCreator? m_EnterPointCreator;
		private BlueprintAreaPresetCreator? m_PresetCreator;
		private AreaPartBoundsCreator? m_BoundsCreator;

		protected override string DefaultFolder
			=> "Blueprints/World/Areas/";

		protected override string DefaultTemplate
			=> "Blueprints/World/Areas/{Chapter}/{Location}/{Location}{name}.jbp";

		protected override string Template
			=> Templates.instance == null
				? DefaultTemplate
				: Templates.instance.BlueprintArea.BlueprintAreaTemplate;

		public override string CreatorName => "Area";

        public override object CreateAsset()
        {
            return new BlueprintArea();
        }

        public override string ProcessTemplate(string? assetName = null)
        {
	        string result = base.ProcessTemplate(assetName);
	        if (IsFolderOverridden)
	        {
		        string? path = Path.GetDirectoryName(result)?.Replace("\\", "/");
		        string folderName = Path.GetFileName(path);
		        string filename = Path.GetFileName(result);
		        result = $"{path}/{folderName}{filename}";
	        }
	        return result;
        }

        private string GetScenePathFromTemplate(
	        BlueprintArea area, string sceneTemplate, string defaultSceneTemplate, string scenePostfix)
        {
	        if (IsFolderOverridden)
	        {
		        string assetPath = BlueprintsDatabase.GetAssetPath(area);
		        string? areaFolder = Path.GetDirectoryName(assetPath);
		        string? areaFolderName = Path.GetFileName(areaFolder);
		        string? parentFolder = Path.GetDirectoryName(areaFolder);
		        string? parentFolderName = Path.GetFileName(parentFolder);
		        return $"{ScenesRoot}{parentFolderName}/{areaFolderName}/{area.name}{scenePostfix}";
	        }

	        return UpdateTemplateResult(sceneTemplate, defaultSceneTemplate);
        }

        public override void PostProcess(object asset)
        {
            var area = (BlueprintArea)asset;

            string mechanicsPath = GetScenePathFromTemplate(
	            area,MechanicsSceneTemplate, DefaultMechanicsSceneTemplate, MechanicsScenePostfix);
			string staticPath = GetScenePathFromTemplate(
				area, StaticSceneTemplate, DefaultStaticSceneTemplate, StaticScenePostfix);
			string lightPath = GetScenePathFromTemplate(
				area, LightSceneTemplate, DefaultLightSceneTemplate, LightScenePostfix);

			BlueprintAreaEnterPoint? enterPoint = null;
			m_EnterPointCreator = m_EnterPointCreator ? m_EnterPointCreator : CreateInstance<BlueprintEnterPointCreator>();
			if (m_EnterPointCreator != null)
			{
				m_EnterPointCreator.Area = area.ToReference<BlueprintAreaReference>();
				enterPoint = NewAssetWindow.CreateWithCreator(
					m_EnterPointCreator,
					DefaultEntranceSuffix,
					Folder) as BlueprintAreaEnterPoint;
			}

			AreaPartBounds? bounds = null;
			m_BoundsCreator = m_BoundsCreator ? m_BoundsCreator : CreateInstance<AreaPartBoundsCreator>();
			if (m_BoundsCreator != null)
			{
				m_BoundsCreator.Area = area.ToReference<BlueprintAreaReference>();
				bounds = NewAssetWindow.CreateWithCreator(
					m_BoundsCreator,
					area.name + "_Bounds",
					Folder) as AreaPartBounds;
				area.Bounds = bounds;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(mechanicsPath) ?? string.Empty);
			Directory.CreateDirectory(Path.GetDirectoryName(staticPath) ?? string.Empty);
			Directory.CreateDirectory(Path.GetDirectoryName(lightPath) ?? string.Empty);

			AssetDatabase.CopyAsset(MechanicsTemplateScenePath, mechanicsPath);
			AssetDatabase.CopyAsset(StaticTemplateScenePath, staticPath);
			AssetDatabase.CopyAsset(LightTemplateScenePath, lightPath);

			var mechanicsScene = EditorSceneManager.OpenScene(mechanicsPath, OpenSceneMode.Single);
			EditorSceneManager.OpenScene(staticPath, OpenSceneMode.Additive);
			EditorSceneManager.OpenScene(lightPath, OpenSceneMode.Additive);

			if (EditorPreferences.Instance.LdDesigner)
			{
				SceneManager.SetActiveScene(mechanicsScene);
			}

			foreach (var go in mechanicsScene.GetRootGameObjects())
			{
				// Fix copied unique ids
				go.GetComponentsInChildren<EntityViewBase>()
					.ForEach(e => e.UniqueId = Guid.NewGuid().ToString());

				go.GetComponentsInChildren<AreaEnterPoint>()
					.ForEach(p => p.Blueprint = enterPoint);
			}

			area.DynamicScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(mechanicsPath));
			area.StaticScene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(staticPath));

			BlueprintAreaPreset? preset = null;
			m_PresetCreator = m_PresetCreator ? m_PresetCreator : CreateInstance<BlueprintAreaPresetCreator>();
			if (m_PresetCreator != null)
			{
				m_PresetCreator.Area = area.ToReference<BlueprintAreaReference>();
				preset = NewAssetWindow.CreateWithCreator(
					m_PresetCreator,
					"Default",
					Folder) as BlueprintAreaPreset;
				if (preset != null)
				{
					preset.Area = area;
					preset.EnterPoint = enterPoint;
					area.DefaultPreset = preset;
				}
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

			EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(buildScenes).ToArray();

			enterPoint?.SetDirty();
			EditorUtility.SetDirty(bounds);
			preset?.SetDirty();
			area.SetDirty();

			AssetDatabase.SaveAssets();
            BlueprintsDatabase.SaveAllDirty();
			Selection.activeObject = BlueprintEditorWrapper.Wrap(area);
		}

		private static TextAsset? CreateEmptyNavmeshCacheFile()
		{
			string scenePath = SceneManager.GetActiveScene().path;

			string sceneName = SceneManager.GetActiveScene().name;
			int underTypeIndex = sceneName.LastIndexOf("_", StringComparison.Ordinal);
			if (underTypeIndex > 0)
				sceneName = sceneName[..underTypeIndex];

			int underscoreIndex = scenePath.LastIndexOf("/", StringComparison.Ordinal);
			if (underscoreIndex > 0)
				scenePath = scenePath[..underscoreIndex];

			scenePath += "/Navmesh/";
			Directory.CreateDirectory(Path.GetDirectoryName(scenePath) ?? string.Empty);

			scenePath += sceneName + ".bytes";
			string path = AssetDatabase.GenerateUniqueAssetPath(scenePath);

			var fileInfo = new FileInfo(path);
			if (fileInfo.Exists && fileInfo.IsReadOnly)
				fileInfo.IsReadOnly = false;

			File.WriteAllBytes(path,Array.Empty<byte>());

			AssetDatabase.Refresh();
			return AssetDatabase.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
		}
	}
}