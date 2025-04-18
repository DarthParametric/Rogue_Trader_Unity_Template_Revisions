using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class DeleteFromSummonPoolFromBlueprintSummonPoolFactory : ElementFactory<DeleteUnitFromSummonPool, BlueprintSummonPool>
	{
		protected override void Populate(SimpleBlueprint owner, DeleteUnitFromSummonPool element, BlueprintSummonPool source)
		{
			element.SummonPool = source.ToReference<BlueprintSummonPoolReference>();
		}
	}
}