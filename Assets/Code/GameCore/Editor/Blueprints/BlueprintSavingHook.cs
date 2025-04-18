#if UNITY_EDITOR && EDITOR_FIELDS
using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Utility.EditorPreferences;

namespace Kingmaker.Editor.Blueprints
{
    [UsedImplicitly]
    public class BlueprintSavingHook : UnityEditor.AssetModificationProcessor
    {
        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (EditorPreferences.Instance.SaveBlueprintsWithAssets)
            {
                BlueprintsDatabase.SaveAllDirty();
            }

            return paths;
        }
    }
}
#endif