using System;
using System.Linq;
using Kingmaker.AreaLogic.Cutscenes;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class CommandActionTest : ICommandTest
	{
		private static readonly IGameActionTest[] GameActionTests =
		{
			new ShowUnitActionTest(),
			new SpawnActionTest(),
			new SpawnBySummonPoolActionTest(),
			new SpawnByUnitGroupActionTest(),
		};

		public Action? GetForCommand(CommandBase command)
		{
			if (command is not CommandAction action)
			{
				return null;
			}

			if (!action.Action.HasActions)
			{
				return null;
			}

			var gameAction = action.Action.Actions[0];
			return GameActionTests
				.Select(test => test.GetForGameAction(gameAction))
				.FirstOrDefault(test => test != null);
		}
	}
}