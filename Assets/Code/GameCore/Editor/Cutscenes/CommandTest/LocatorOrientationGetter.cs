using System;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.ElementsSystem;
using Kingmaker.View;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class LocatorOrientationGetter : IFloatGetter
	{
		public Func<float>? GetFloat(FloatEvaluator floatEvaluator)
		{
			if (floatEvaluator is not LocatorOrientation locatorOrientation)
			{
				return null;
			}

			LocatorView? locatorView = null;
			if (locatorOrientation.LocatorEval is LocatorReference locatorReference)
			{
				locatorView = locatorReference.Locator.FindViewInEditor() as LocatorView;
			}

			if (locatorView == null)
			{
				locatorView = locatorOrientation.Locator.FindViewInEditor() as LocatorView;
				if (locatorView == null)
				{
					PFLog.Cutscene.Warning("Cannot find locator in scene.");
					return null;
				}
			}

			return () => locatorView.ViewTransform.rotation.eulerAngles.y;
		}
	}
}