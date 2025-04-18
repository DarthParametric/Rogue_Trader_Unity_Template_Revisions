using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.ElementsSystem;
using Kingmaker.View.Spawners;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class UnitFromSpawnerGetter : IUnitSpawnerGetter
	{
		public (BlueprintUnit?, GameObject?) GetUnitAndSpawnerFromEvaluator(AbstractUnitEvaluator abstractUnitEvaluator)
		{
			if (abstractUnitEvaluator is not UnitFromSpawner unitFromSpawner)
			{
				return (null, null);
			}

			var unitSpawner = unitFromSpawner.Spawner.FindViewInEditor() as UnitSpawnerBase;
			if (unitSpawner == null)
			{
				PFLog.Cutscene.Warning("Cannot find UnitFromSpawner in scene.");
				return (null, null);
			}

			return (unitSpawner.Blueprint, unitSpawner.gameObject);
		}
	}
}