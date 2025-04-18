using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root.Fx;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class FxRootValidationVisitor :
		BlueprintValidatorVisitor<FxRoot>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			base.OnValidate(instance);
			target.Deinit();
		}
	}
}