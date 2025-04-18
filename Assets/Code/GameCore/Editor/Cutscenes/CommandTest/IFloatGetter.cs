using System;
using Kingmaker.ElementsSystem;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public interface IFloatGetter
	{
		public Func<float>? GetFloat(FloatEvaluator floatEvaluator);
	}
}