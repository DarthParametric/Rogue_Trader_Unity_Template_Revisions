using System;
using Kingmaker.ElementsSystem;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public interface IVector3Getter
	{
		public Func<Vector3>? GetVector3(PositionEvaluator positionEvaluator);
	}
}