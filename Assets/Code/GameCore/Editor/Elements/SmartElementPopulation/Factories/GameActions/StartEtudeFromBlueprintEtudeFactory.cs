using JetBrains.Annotations;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class StartEtudeFromBlueprintEtudeFactory : ElementFactory<StartEtude, BlueprintEtude>
	{
		protected override void Populate(SimpleBlueprint owner, StartEtude element, BlueprintEtude source)
		{
			element.Etude = source.ToReference<BlueprintEtudeReference>();
		}
	}
}