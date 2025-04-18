using System;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.View.Spawners;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class ShowUnitActionTest : IGameActionTest
	{
		public Action? GetForGameAction(GameAction action)
		{
			if (action is not HideUnit {Unhide: true} hideUnit)
			{
				return null;
			}

			return () => Test(hideUnit);
		}

		private static void Test(HideUnit hideUnit)
		{
			var (unit, spawner) = UnitGetter.Get(hideUnit.Target);
			if (unit == null || spawner == null)
			{
				return;
			}
			CommandTestHelpers.SpawnUnitsFromSpawners(new []{spawner.GetComponent<UnitSpawnerBase>()});
		}
	}
}