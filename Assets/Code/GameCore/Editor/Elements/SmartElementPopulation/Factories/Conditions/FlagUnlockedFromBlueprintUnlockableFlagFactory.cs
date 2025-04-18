using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class FlagUnlockedFromBlueprintUnlockableFlagFactory : ElementFactory<FlagUnlocked, BlueprintUnlockableFlag>
	{
		protected override void Populate(SimpleBlueprint owner, FlagUnlocked element, BlueprintUnlockableFlag source)
		{
			element.ConditionFlag = source;
		}
	}
}