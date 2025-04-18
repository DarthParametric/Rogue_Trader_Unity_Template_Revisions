using System;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.ElementsSystem;
using Kingmaker.View;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class LocatorPositionGetter : IVector3Getter
	{
		public Func<Vector3>? GetVector3(PositionEvaluator positionEvaluator)
		{
			if (positionEvaluator is not LocatorPosition locatorPosition)
			{
				return null;
			}

			var locator = locatorPosition.Locator.FindViewInEditor() as LocatorView;
			if (locator == null)
			{
				PFLog.Cutscene.Warning("Cannot find locator in scene.");
				return null;
			}

			return () => locator.transform.position + locatorPosition.Offset;
		}
	}
}