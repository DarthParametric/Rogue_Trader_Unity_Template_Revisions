using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using UnityEditor;

public class LocalizationUtility : EditorWindow
{
    [MenuItem("Tools/Localization/Update localization comments")]
	public static void AddCommentsToJsons()
	{
		if (Selection.objects == null || Selection.objects.Length == 0)
			return;
		EditorUtility.DisplayProgressBar("Loading everything", "", 0);
		var selected = Selection.objects
			.Select(_ => _ as BlueprintEditorWrapper)
			.Where(_ => _ != null).ToList();
		foreach (BlueprintEditorWrapper selectedObj in new ProgressWrapper<BlueprintEditorWrapper>(selected, "Handling blueprints"))
		{
			if (!(selectedObj.Blueprint is BlueprintAnswer) && !(selectedObj.Blueprint is BlueprintCue))
				continue;
			AddCommentsToJsons(selectedObj);
		}
		EditorUtility.DisplayProgressBar("Saving blueprints", "", 0);
		BlueprintsDatabase.SaveAllDirty();
		EditorUtility.ClearProgressBar();
	}
        
	public static void AddCommentsToJsons(BlueprintEditorWrapper asset)
	{
#if UNITY_EDITOR && EDITOR_FIELDS
		var comment = (asset.Blueprint as BlueprintScriptableObject)?.Comment;
		if (string.IsNullOrEmpty(comment))
			return;
	        
		var so = new SerializedObject(asset);
		var p = so.GetIterator();
		p.Next(true);
		do
		{
			if (p.propertyType == SerializedPropertyType.Generic && !p.isArray && p.type == nameof(LocalizedString))
			{
				var ls = FieldFromProperty.GetFieldValue(p) as LocalizedString;
				if (ls == null)
					continue;
				//ls.Init(p);
				if (!ls.Shared && ls.JsonPath != "" && ls.GetData()?.Languages.Count > 0)
				{
					var data = ls.GetData();
					var nativeLanguage = data?.GetOrCreateLocaleData(Locale.dev);
					bool? modified = data?.UpdateTranslationComment(nativeLanguage, comment);

					if (LocalizationManager.Instance.CurrentLocale == Locale.ruRU)
					{
						var ruLang = data?.GetOrCreateLocaleData(Locale.ruRU);
						data?.UpdateTranslationComment(ruLang, comment);
					}

					if (modified.HasValue && modified.Value)
						ls.SaveJson(p);
				}
			}
		} while (p.Next((p.propertyType == SerializedPropertyType.Generic ||
		                 p.propertyType == SerializedPropertyType.ManagedReference) &&
		                p.propertyPath != "Blueprint.Components"));

		so.ApplyModifiedPropertiesWithoutUndo();
#endif
	}
}
