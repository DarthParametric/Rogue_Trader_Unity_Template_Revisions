using System;
using System.Collections.Generic;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.View.Spawners;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class SpawnActionTest : IGameActionTest
	{
		public Action? GetForGameAction(GameAction action)
		{
			if (action is not Spawn spawn)
			{
				return null;
			}
			return () => Test(spawn);
		}

		private static void Test(Spawn spawn)
		{
			var spawners = new List<UnitSpawnerBase>(spawn.Spawners.Length);
			foreach (var entityReference in spawn.Spawners)
			{
				if (entityReference.FindViewInEditor() is UnitSpawnerBase spawner)
				{
					spawners.Add(spawner);
				}
				else
				{
					PFLog.Cutscene.Warning("Failed to find unit spawner for spawn action.");
				}
			}

			CommandTestHelpers.SpawnUnitsFromSpawners(spawners);
		}
	}
}