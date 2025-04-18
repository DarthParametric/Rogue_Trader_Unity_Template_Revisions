using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit;
using Kingmaker.View.Spawners;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Conditions
{
	[UsedImplicitly]
	public class UnitIsDeadFromUnitSpawnerFactory : ElementFactory<UnitIsDead, UnitSpawner>
	{
		private readonly UnitFromSpawnerFromUnitSpawnerFactory m_UnderlyingFactory =
			new UnitFromSpawnerFromUnitSpawnerFactory();

		protected override void Populate(SimpleBlueprint owner, UnitIsDead element, UnitSpawner source)
		{
			element.Target = m_UnderlyingFactory.Create(owner, source);
		}
	}
}