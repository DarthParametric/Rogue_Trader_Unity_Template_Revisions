using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class UnlockFlagFromBlueprintUnlockableFlagFactory : ElementFactory<UnlockFlag, BlueprintUnlockableFlag>
	{
		protected override void Populate(SimpleBlueprint owner, UnlockFlag element, BlueprintUnlockableFlag source)
		{
			element.flag = source;
		}
	}
}