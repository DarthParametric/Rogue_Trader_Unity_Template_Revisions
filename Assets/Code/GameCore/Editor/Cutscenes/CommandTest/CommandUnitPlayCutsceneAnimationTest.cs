using System;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.AreaLogic.Cutscenes.Commands;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine.Playables;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class CommandUnitPlayCutsceneAnimationTest : ICommandTest
	{
		public Action? GetForCommand(CommandBase command)
		{
			if (command is not CommandUnitPlayCutsceneAnimation playCommand)
			{
				return null;
			}

			return () => Test(playCommand);
		}

		private static void Test(CommandUnitPlayCutsceneAnimation playCommand)
		{
			if (playCommand.ClipWrapperForEditor == null || playCommand.ClipWrapperForEditor.AnimationClip == null)
			{
				PFLog.Cutscene.Warning("No animation clip defined.");
				return;
			}

			var (unit, spawner) = UnitGetter.Get(playCommand.UnitEvaluator);
			if (unit == null || spawner == null)
			{
				return;
			}

			var unitVisual = CommandTestHelpers.SpawnUnitVisualIfNeeded(unit, spawner);
			if (unitVisual == null)
			{
				return;
			}

			var clip = playCommand.ClipWrapperForEditor.AnimationClip;
			bool isIdleNamed = clip.name.ToLower().Contains("idle");

			var animationManager = unitVisual.GetComponentInChildren<UnitAnimationManager>();
			CommandTestHelpers.PlayAnimation(
				animationManager.gameObject,
				clip,
				isIdleNamed ? DirectorWrapMode.Loop : DirectorWrapMode.Hold);
		}
	}
}