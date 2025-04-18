using Code.GameCore.EntitySystem.Entities;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.View.Spawners;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit
{
	[UsedImplicitly]
	public class UnitFromSpawnerFromUnitSpawnerFactory : ElementFactory<UnitFromSpawner, UnitSpawner>
	{
		protected override void Populate(SimpleBlueprint owner, UnitFromSpawner element, UnitSpawner source)
		{
			element.Spawner = EntityViewBaseExtension.ToEditorEntityReference(source);
		}
	}
}