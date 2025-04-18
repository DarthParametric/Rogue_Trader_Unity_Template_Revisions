using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.DialogSystem.Blueprints;
using UnityEditor;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class BlueprintCueValidationVisitor 
		: BlueprintValidatorVisitor<BlueprintCue>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			UpdateSpeaker(target);
			base.OnValidate(instance);
		}

		public static void UpdateSpeaker(BlueprintCue cue, string defaultSpeakerName = "", string defaultSpeakerGender = "")
		{
			#if EDITOR_FIELDS
			if (cue.Speaker.Blueprint == null && !cue.Speaker.NoSpeaker &&
			    (defaultSpeakerName == "" || defaultSpeakerGender == ""))
				return;
            
			string speakerName = cue.Speaker.Blueprint != null ? cue.Speaker.Blueprint.AssetName : defaultSpeakerName;
			string speakerGender = cue.Speaker.Blueprint != null ? cue.Speaker.Blueprint.Gender.ToString() : defaultSpeakerGender;
			var prop = new SerializedObject(BlueprintEditorWrapper.Wrap(cue)).FindProperty("Blueprint.Text");
			cue.Text.UpdateSpeaker(prop, speakerName, speakerGender);
			#endif
		}
	}
}