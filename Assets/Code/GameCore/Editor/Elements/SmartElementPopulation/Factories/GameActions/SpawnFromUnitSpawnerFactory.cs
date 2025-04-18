using Code.GameCore.EntitySystem.Entities;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.View.Spawners;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Conditions
{
	[UsedImplicitly]
	public class SpawnFromUnitSpawnerFactory : ElementFactory<Spawn, UnitSpawner>
	{
		protected override void Populate(SimpleBlueprint owner, Spawn element, UnitSpawner source)
		{
			element.Spawners = new Kingmaker.Blueprints.EntityReference[1];
			element.Spawners[0] = EntityViewBaseExtension.ToEditorEntityReference(source);
		}
	}
}