using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class SpawnBySummonPoolFromBlueprintSummonPoolFactory : ElementFactory<SpawnBySummonPool, BlueprintSummonPool>
	{
		protected override void Populate(SimpleBlueprint owner, SpawnBySummonPool element, BlueprintSummonPool source)
		{
			element.Pool = source;
		}
	}
}