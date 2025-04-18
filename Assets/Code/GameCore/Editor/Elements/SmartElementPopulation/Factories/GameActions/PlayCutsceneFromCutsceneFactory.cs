using JetBrains.Annotations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.GameActions
{
	[UsedImplicitly]
	public class PlayCutsceneFromCutsceneFactory : ElementFactory<PlayCutscene, Cutscene>
	{
		protected override void Populate(SimpleBlueprint owner, PlayCutscene element, Cutscene source)
		{
			element.Cutscene = source.ToReference<CutsceneReference>();
		}
	}
}