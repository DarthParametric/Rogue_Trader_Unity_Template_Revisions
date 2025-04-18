using Code.GameCore.EntitySystem.Entities;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.View.Spawners;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit
{
	[UsedImplicitly]
	public class SpawnByUnitGroupFromUnitGroupViewFactory : ElementFactory<SpawnByUnitGroup, UnitGroupView>
	{
		protected override void Populate(SimpleBlueprint owner, SpawnByUnitGroup element, UnitGroupView source)
		{
			element.m_Group = EntityViewBaseExtension.ToEditorEntityReference(source);
		}
	}
}