using System;
using System.Text;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Settings;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnitDescription;

namespace Code.GameCore.Editor.Blueprints.BlueprintUnitEditorChecker
{
	public enum ActualGameDifficultyOption
	{
		Custom = -1,
		Story = 0,
		Normal = 2,
		Core = 4,
		Hard = 5,
		Unfair = 6
	}

	[Serializable]
	public class BlueprintUnitCheckerInEditor
	{
		public static void ShowUnitStats(BlueprintUnit unit, ActualGameDifficultyOption difficulty)
		{
			SetDifficulty(difficulty);

			BlueprintUnitCheckerInEditorWindow.DisplayResults(CollectResults(unit));
		}

		public static void SetDifficulty(ActualGameDifficultyOption difficulty)
		{
			var convertedDifficulty = GetDifficulty(difficulty);
			BlueprintRoot.Instance.SettingsValues.DifficultiesPresets.Difficulties.TryFind(x => x.Preset
				.GameDifficulty == convertedDifficulty, out var preset);
			SettingsController.Instance.InitializeControllers(BlueprintRoot.Instance.SettingsValues
				.DifficultiesPresets, BlueprintRoot.Instance.SettingsValues.GraphicsPresetsList);
			SettingsController.Instance.DifficultyPresetsController.SetDifficultyPreset(preset.Preset, true);
		}

		private static string CollectResults(BlueprintUnit unit)
		{
			var desc = UnitDescriptionHelper.GetDescriptionForEditorCheck(unit);
			var stringBuilder = new StringBuilder();
			
			stringBuilder.AppendLine($"HP: {desc.HP.ToString()}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"InitialMovementPoints: {desc.InitialMovementPoints.ToString()}");
			stringBuilder.AppendLine($"InitialActionPoints: {desc.InitialActionPoints.ToString()}");
			stringBuilder.AppendLine();
			stringBuilder.AppendLine($"Stats for {unit.name}");
			foreach (string stat in desc.Stats.All)
			{
				stringBuilder.AppendLine(stat);
			}

			return stringBuilder.ToString();
		}
		
		private static GameDifficultyOption GetDifficulty(ActualGameDifficultyOption difficulty)
		{
			return difficulty switch
			{
				ActualGameDifficultyOption.Custom => GameDifficultyOption.Custom,
				ActualGameDifficultyOption.Story => GameDifficultyOption.Story,
				ActualGameDifficultyOption.Normal => GameDifficultyOption.Normal,
				ActualGameDifficultyOption.Core => GameDifficultyOption.Core,
				ActualGameDifficultyOption.Hard => GameDifficultyOption.Hard,
				ActualGameDifficultyOption.Unfair => GameDifficultyOption.Unfair,
				_ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
			};
		}
	}
}