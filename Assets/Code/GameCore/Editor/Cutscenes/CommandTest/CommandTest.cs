using System;
using System.Linq;
using Kingmaker.AreaLogic.Cutscenes;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public static class CommandTest
	{
		private static readonly ICommandTest[] CommandTests =
		{
			new CommandActionTest(),
			new CommandControlCameraTest(),
			new CommandMoveUnitTest(),
			new CommandUnitPlayCutsceneAnimationTest(),
		};

		public static Action? GetCommandTest(CommandBase command)
		{
			return CommandTests
				.Select(commandTest => commandTest.GetForCommand(command))
				.FirstOrDefault(test => test != null);
		}
	}
}