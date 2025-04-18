using Code.GameCore.Blueprints.BlueprintReflectionValidator;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Code.GameCore.Editor.Validation
{
	[UsedImplicitly]
	public class BlueprintEncyclopediaNodeValidationVisitor :
		BlueprintValidatorVisitor<BlueprintEncyclopediaNode>
	{
		public override void OnValidate(BlueprintScriptableObject instance)
		{
			var target = ConvertTarget(instance);
			base.OnValidate(instance);
			foreach (var child in target.ChildPages)
			{
				BlueprintEncyclopediaPage page = child?.Get();
				if (page == null) continue;
				if (page.ParentAsset == instance) continue;
				page.ParentAsset = target;
				BlueprintsDatabase.Save(page.AssetGuid);
			}
		}
	}
}