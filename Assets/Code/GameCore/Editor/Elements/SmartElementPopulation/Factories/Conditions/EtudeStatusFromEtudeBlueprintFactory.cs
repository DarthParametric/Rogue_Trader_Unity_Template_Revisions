using JetBrains.Annotations;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class EtudeStatusFromEtudeBlueprintFactory : ElementFactory<EtudeStatus, BlueprintEtude>
	{
		protected override void Populate(SimpleBlueprint owner, EtudeStatus element, BlueprintEtude source)
		{
			element.Etude = source;
			element.Playing = true;
		}
	}
}