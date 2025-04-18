using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class SummonPoolUnitsFromBlueprintSummonPoolFactory : ElementFactory<SummonPoolUnits, BlueprintSummonPool>
	{
		protected override void Populate(SimpleBlueprint owner, SummonPoolUnits element, BlueprintSummonPool source)
		{
			element.SummonPool = source;
		}
	}
}