using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.View.Spawners;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit
{
	[UsedImplicitly]
	public class UnitPositionFromUnitSpawnerFactory : ElementFactory<UnitPosition, UnitSpawner>
	{
		private readonly UnitFromSpawnerFromUnitSpawnerFactory m_UnderlyingFactory =
			new UnitFromSpawnerFromUnitSpawnerFactory();

		protected override void Populate(SimpleBlueprint owner, UnitPosition element, UnitSpawner source)
		{
			element.Unit = m_UnderlyingFactory.Create(owner, source);
		}
	}
}