using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public static class UnitGetter
	{
		private static readonly IUnitSpawnerGetter[] Getters =
		{
			new NamedParameterUnitGetter(),
			new UnitFromSpawnerGetter(),
		};

		public static (BlueprintUnit?, GameObject?) Get(AbstractUnitEvaluator? abstractUnitEvaluator)
		{
			if (abstractUnitEvaluator == null)
			{
				PFLog.Cutscene.Warning("Unit evaluator is undefined.");
				return (null, null);
			}

			var (blueprintUnit, spawner) = Getters
				.Select(getter => getter.GetUnitAndSpawnerFromEvaluator(abstractUnitEvaluator))
				.FirstOrDefault(tuple => tuple.Item1 != null && tuple.Item2 != null);

			if (blueprintUnit == null || spawner == null)
			{
				PFLog.Cutscene.Warning($"Unsupported unit evaluator type: {abstractUnitEvaluator.GetType()}");
				return (null, null);
			}

			return (blueprintUnit, spawner);
		}
	}
}