using System;
using System.Linq;
using Kingmaker.ElementsSystem;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public static class Vector3Getter
	{
		private static readonly IVector3Getter[] Getters =
		{
			new LocatorPositionGetter(),
		};

		public static Func<Vector3>? Get(PositionEvaluator? positionEvaluator)
		{
			if (positionEvaluator == null)
			{
				PFLog.Cutscene.Warning("Position evaluator is undefined.");
				return null;
			}

			var getter = Getters
				.Select(getter => getter.GetVector3(positionEvaluator))
				.FirstOrDefault(getter => getter != null);

			if (getter == null)
			{
				PFLog.Cutscene.Warning($"Unsupported position evaluator type: {positionEvaluator.GetType()}");
				return null;
			}
			return getter;
		}
	}
}