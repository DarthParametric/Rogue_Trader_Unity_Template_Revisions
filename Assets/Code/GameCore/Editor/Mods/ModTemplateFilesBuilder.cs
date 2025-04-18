using Code.GameCore.EditorMods;
using UnityEditor;

namespace Code.GameCore.Editor.Mods
{
	/// <summary>
	/// Starting point of all Mod Template Caches creation while building.
	/// </summary>
	public static class ModTemplateFilesBuilder
	{
		[MenuItem("Tools/Mods/Generate All Mod Template Files")]
		public static void BuildModTemplateFiles()
		{
			RenderConfigsInfoCache.CreateRenderConfigsCacheInternal(true);
			ShadersModAggregator.AggregateShadersInternal(true);
			MaterialShaderInfoCache.CacheShaderInfoInternal(true);
			BlueprintDirectReferencesAnalyzer.CreateReferenceCache(true);
		}
	}
}