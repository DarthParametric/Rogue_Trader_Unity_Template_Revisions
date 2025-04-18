using System;
using Kingmaker.ElementsSystem;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public interface IGameActionTest
	{
		Action? GetForGameAction(GameAction action);
	}
}