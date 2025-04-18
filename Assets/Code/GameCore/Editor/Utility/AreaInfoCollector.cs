using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Enums;
using Kingmaker.Utility.UnityExtensions;

namespace Kingmaker.Editor.Utility
{
	public class AreaInfo
	{
		public BlueprintArea Area;
		public BlueprintAreaPart[] Parts;
		public SceneReference StaticScene;
		public SceneReference FoliageScene;
		public SceneReference[] LightScenes;
		public SceneReference[] AudioScenes;
		public SceneReference[] MechanicScenes;
		public BlueprintAreaEnterPoint[] EnterPoints;
		public BlueprintDialog[] Dialogs;
		public Cutscene[] Cutscenes;
		public BlueprintScriptableObject[] Others;
	}

	public class AreaDependenciesInfo
	{
		public HashSet<string> BlueprintPaths = new HashSet<string>();
		public List<AlignmentComponent> ReinforcedAlignments = new List<AlignmentComponent>();
		public List<AlignmentComponent> CheckedAlignments = new List<AlignmentComponent>();
	}

	public static class AreaInfoCollector
	{
		public static AreaInfo Collect(BlueprintArea area)
		{
			var areaPath = GetPath(area);
			var areaDirectory = Path.GetDirectoryName(areaPath);
			var areaName = area.AssetName;

			return new AreaInfo
			{
				Area = area,
				Parts = area.GetParts().ToArray(),
				StaticScene = area.MainStaticScene,
				LightScenes = area.LightScenes.ToArray(),
				AudioScenes = area.AudioScenes.ToArray(),
				MechanicScenes = new[] {area.DynamicScene}.ToArray(),
				EnterPoints = LoadAssets<BlueprintAreaEnterPoint>(areaDirectory).Where(ep => ep.Area == area).ToArray(),
				Dialogs = ExtractDialogs<BlueprintDialog>(areaName),
				Cutscenes = LoadAssets<Cutscene>(PathUtils.BlueprintPath($"World/Cutscenes/{areaName}")).ToArray(),
				Others = LoadAssets<BlueprintScriptableObject>(PathUtils.BlueprintPath($"World/Encounters/{areaName}")).ToArray()
			};
		}

		public static List<BlueprintScriptableObject> CollectBlueprints(BlueprintArea area)
		{
			var areaPath = GetPath(area);
			var areaDirectory = Path.GetDirectoryName(areaPath);
			var areaName = area.AssetName;
			var areaDirectoryName = Path.GetFileName(areaDirectory);

			return ExtractDialogs<BlueprintScriptableObject>(areaName)
				.Concat(ExtractDialogs<BlueprintScriptableObject>(areaDirectoryName))
				.Concat(LoadAssets<BlueprintScriptableObject>(areaDirectory))
				.Concat(LoadAssets<BlueprintScriptableObject>(PathUtils.BlueprintPath($"World/Cutscenes/{areaName}")))
				.Concat(LoadAssets<BlueprintScriptableObject>(PathUtils.BlueprintPath($"World/Encounters/{areaName}")))
				.Concat(LoadAssets<BlueprintScriptableObject>(PathUtils.BlueprintPath($"World/Cutscenes/{areaDirectoryName}")))
				.Concat(LoadAssets<BlueprintScriptableObject>(PathUtils.BlueprintPath($"World/Encounters/{areaDirectoryName}")))
				.ToList();
		}

		public static AreaDependenciesInfo CollectDependencies(BlueprintArea area)
		{			
			var result = new AreaDependenciesInfo();		

			result.CheckedAlignments = result.CheckedAlignments.Distinct().ToList();
			result.CheckedAlignments.Sort();

			result.ReinforcedAlignments = result.ReinforcedAlignments.Distinct().ToList();
			result.ReinforcedAlignments.Sort();

			result.BlueprintPaths = new HashSet<string>();
			//result.BlueprintPaths = dependencies
			//	.Where(d => d != null)
			//	.Where(d => !dialogBlueprints.Contains(d))
			//	.Concat(info.EnterPoints)
			//	.Select(AssetDatabase.GetAssetPath);

			var pregens = LoadAssets<BlueprintUnit>("Assets/Mechanics/Blueprints/Units/Pregens");
			foreach (var pregen in pregens)
			{
				var prototypePath = GetPath(pregen.PrototypeLink as BlueprintScriptableObject);
				if (result.BlueprintPaths.Contains(prototypePath))
				{
					result.BlueprintPaths.Add(GetPath(pregen));
				}
			}

			return result;
		}

		private static T[] ExtractDialogs<T>(string areaName) where T : BlueprintScriptableObject
		{
			var result = new List<T>();
			var dialogsDirectory = PathUtils.BlueprintPath("World/Dialogs");
			var chaptersPaths = Directory.GetDirectories(dialogsDirectory);
			foreach (var path in chaptersPaths)
			{
				var dialogsPath = Path.Combine(path, areaName);
				if (Directory.Exists(dialogsPath))
				{
					result.AddRange(LoadAssets<T>(dialogsPath));
				}
			}
			return result.ToArray();
		}

		private static IEnumerable<T> LoadAssets<T>(string path) where T : SimpleBlueprint
		{
			if (!Directory.Exists(path))
			{
				return Enumerable.Empty<T>();
			}
            return BlueprintsDatabase.LoadAllOfType<T>(path);
        }
        
        private static string GetPath(SimpleBlueprint bp)
        {
            return BlueprintsDatabase.GetAssetPath(bp);
		}
	}
}