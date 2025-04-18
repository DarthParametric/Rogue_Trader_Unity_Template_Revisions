using System;
using Kingmaker.AreaLogic.Cutscenes;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public interface ICommandTest
	{
		Action? GetForCommand(CommandBase command);
	}
}