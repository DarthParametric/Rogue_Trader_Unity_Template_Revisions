using System;
using System.Linq;
using Kingmaker.ElementsSystem;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public static class FloatGetter
	{
		private static readonly IFloatGetter[] Getters =
		{
			new LocatorOrientationGetter(),
		};

		public static Func<float>? Get(FloatEvaluator? floatEvaluator)
		{
			if (floatEvaluator == null)
			{
				PFLog.Cutscene.Warning("Float evaluator is undefined.");
				return null;
			}

			var getter = Getters
				.Select(getter => getter.GetFloat(floatEvaluator))
				.FirstOrDefault(getter => getter != null);

			if (getter == null)
			{
				PFLog.Cutscene.Warning($"Unsupported float evaluator type: {floatEvaluator.GetType()}");
				return null;
			}
			return getter;
		}
	}
}