using System;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class CommandMoveUnitTest : ICommandTest
	{
		public Action? GetForCommand(CommandBase command)
		{
			if (command is not CommandMoveUnit commandMoveUnit)
			{
				return null;
			}

			return () => Test(commandMoveUnit);
		}

		private static void Test(CommandMoveUnit commandMoveUnit)
		{
			var (unit, spawner) = UnitGetter.Get(commandMoveUnit.Unit);
			if (unit == null || spawner == null)
			{
				return;
			}

			var unitVisual = CommandTestHelpers.SpawnUnitVisualIfNeeded(unit, spawner);
			if (unitVisual == null)
			{
				return;
			}

			var followTransform = unitVisual.GetComponent<FollowTransform>();
			if (followTransform == null)
			{
				followTransform = unitVisual.AddComponent<FollowTransform>();
			}

			followTransform.UpdatePosition = Vector3Getter.Get(commandMoveUnit.Target);

			var floatGetter = FloatGetter.Get(commandMoveUnit.Orientation);
			followTransform.UpdateRotation = floatGetter == null
				? null
				: () => Quaternion.Euler(0, floatGetter(), 0);
		}
	}
}