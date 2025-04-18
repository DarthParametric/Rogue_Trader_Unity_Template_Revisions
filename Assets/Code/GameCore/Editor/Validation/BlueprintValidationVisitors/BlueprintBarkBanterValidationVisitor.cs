using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class BlueprintBarkBanterValidationVisitor :
		BlueprintValidatorVisitor<BlueprintBarkBanter>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			base.OnValidate(instance);
			UpdateSpeakers(target);
		}
		
		public static void UpdateSpeakers(BlueprintBarkBanter instance)
		{
			#if EDITOR_FIELDS
			var so = new SerializedObject(BlueprintEditorWrapper.Wrap(instance));
			const string prefix = "Blueprint.";
			for (int i = 0; i < instance.FirstPhrase.Length; i++)
			{
				var s = instance.FirstPhrase[i];
				var p = so.FindProperty($"{prefix}FirstPhrase.Array.data[{i}]");
				string speakerName = instance.Unit != null ? instance.Unit.AssetName : "";
				string speakerGender = instance.Unit != null ? instance.Unit.Gender.ToString() : "";
				s.UpdateSpeaker(p, speakerName, speakerGender);
			}

			for (int i = 0; i < instance.Responses.Length; i++)
			{
				var r = instance.Responses[i];
				var p = so.FindProperty($"{prefix}Responses.Array.data[{i}].Response");
				string speakerName = r.Unit != null ? r.Unit.AssetName : "";
				string speakerGender = r.Unit != null ? r.Unit.Gender.ToString() : "";
				r.Response.UpdateSpeaker(p, speakerName, speakerGender);
			}
			#endif
		}
	}
}