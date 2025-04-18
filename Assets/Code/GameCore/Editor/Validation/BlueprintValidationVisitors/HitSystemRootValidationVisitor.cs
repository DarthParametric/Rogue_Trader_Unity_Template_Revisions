using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Visual.HitSystem;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class HitSystemRootValidationVisitor : 
		BlueprintValidatorVisitor<HitSystemRoot>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			base.OnValidate(target);
			target.Deinit();
		}
	}
}