using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;
using UnityEngine;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public interface IUnitSpawnerGetter
	{
		/// <summary>
		/// Returns unit blueprint and game object of it's spawner
		/// </summary>
		public (BlueprintUnit?, GameObject?) GetUnitAndSpawnerFromEvaluator(AbstractUnitEvaluator abstractUnitEvaluator);
	}
}