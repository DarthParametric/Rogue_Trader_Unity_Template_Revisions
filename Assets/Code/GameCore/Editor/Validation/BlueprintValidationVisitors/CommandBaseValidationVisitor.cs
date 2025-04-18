using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Owlcat.QA.Validation;
using UnityEditor;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class CommandBaseValidationVisitor :
		BlueprintValidatorVisitor<CommandBase>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			if (!BlueprintValidationHelper.AllowOnValidate)
			{
				return;
			}

			if (!EditorApplication.isPlayingOrWillChangePlaymode)
			{
				target.Cleanup();
			}
		}
	}
}