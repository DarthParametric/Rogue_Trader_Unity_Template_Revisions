using Code.GameCore.EntitySystem.Entities;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.View;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit
{
	[UsedImplicitly]
	public class LocatorPositionFromLocatorViewFactory : ElementFactory<LocatorPosition, LocatorView>
	{
		protected override void Populate(SimpleBlueprint owner, LocatorPosition element, LocatorView source)
		{
			element.Locator = EntityViewBaseExtension.ToEditorEntityReference(source);
		}
	}
}