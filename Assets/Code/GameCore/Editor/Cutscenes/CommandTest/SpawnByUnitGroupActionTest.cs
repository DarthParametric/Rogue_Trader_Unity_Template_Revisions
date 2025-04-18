using System;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.View.Spawners;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class SpawnByUnitGroupActionTest : IGameActionTest
	{
		public Action? GetForGameAction(GameAction action)
		{
			if (action is not SpawnByUnitGroup spawnByUnitGroup)
			{
				return null;
			}
			return () => Test(spawnByUnitGroup);
		}

		private static void Test(SpawnByUnitGroup spawnByUnitGroup)
		{
			var unitGroupView = spawnByUnitGroup.m_Group.FindViewInEditor() as UnitGroupView;
			if (unitGroupView == null)
			{
				PFLog.Cutscene.Warning("Unit group is undefined for unit group spawn action.");
				return;
			}

			var spawners = unitGroupView.GetComponentsInChildren<UnitSpawner>();
			CommandTestHelpers.SpawnUnitsFromSpawners(spawners);
		}
	}
}