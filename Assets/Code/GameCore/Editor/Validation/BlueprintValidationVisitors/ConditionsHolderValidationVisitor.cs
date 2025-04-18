using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using Owlcat.QA.Validation;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class ConditionsHolderValidationVisitor :
		BlueprintValidatorVisitor<ConditionsHolder>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			if (!BlueprintValidationHelper.AllowOnValidate)
			{
				return;
			}

			target.Cleanup();
		}
	}
}