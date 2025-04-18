using System;
using System.Collections.Generic;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.ElementsSystem;
using Kingmaker.View.Spawners;
using UnityEngine.SceneManagement;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class SpawnBySummonPoolActionTest : IGameActionTest
	{
		public Action? GetForGameAction(GameAction action)
		{
			if (action is not SpawnBySummonPool spawnBySummonPool)
			{
				return null;
			}
			return () => Test(spawnBySummonPool);
		}

		private static void Test(SpawnBySummonPool spawnBySummonPool)
		{
			var pool = spawnBySummonPool.Pool;
			if (pool == null)
			{
				return;
			}

			var unitSpawners = new List<UnitSpawner>();
			for (int i = 0; i < SceneManager.sceneCount; ++i)
			{
				var scene = SceneManager.GetSceneAt(i);
				foreach (var rootObject in scene.GetRootGameObjects())
				{
					foreach (var spawner in rootObject.GetComponentsInChildren<UnitSpawner>())
					{
						var poolSettings = spawner.GetComponent<SpawnerSummonPoolSettings>();
						if (poolSettings == null || poolSettings.Pools.Length <= 0)
						{
							continue;
						}

						foreach (var poolReference in poolSettings.Pools)
						{
							if (poolReference.GetBlueprint() == pool)
							{
								unitSpawners.Add(spawner);
								break;
							}
						}
					}
				}
			}

			CommandTestHelpers.SpawnUnitsFromSpawners(unitSpawners);
		}
	}
}