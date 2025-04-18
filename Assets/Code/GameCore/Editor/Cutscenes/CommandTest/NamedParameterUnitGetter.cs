using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.NamedParameters;
using Kingmaker.ElementsSystem;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class NamedParameterUnitGetter : IUnitSpawnerGetter
	{
		public (BlueprintUnit?, GameObject?) GetUnitAndSpawnerFromEvaluator(AbstractUnitEvaluator abstractUnitEvaluator)
		{
			if (abstractUnitEvaluator is not NamedParameterUnit namedParameterUnit)
			{
				return (null, null);
			}

			var unitSpawner = EditorCutsceneParams.GetUnit(namedParameterUnit.Parameter);
			if (unitSpawner == null)
			{
				PFLog.Cutscene.Warning($"Cannot find namedParameterUnit in EditorCutsceneParams for '{namedParameterUnit.Parameter}'");
				return (null, null);
			}

			return (unitSpawner.Blueprint, unitSpawner.gameObject);
		}
	}
}