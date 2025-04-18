using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Editor;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor.Blueprints.BlueprintUnitEditorChecker
{
	public class BlueprintUnitCheckerInEditorWindow : KingmakerWindowBase
	{
		private static BlueprintUnit Unit { get; set; }
		private static int CR { get; set; }
		private static ActualGameDifficultyOption Difficulty { get; set; }

		[CanBeNull]
		public static string Results { get; private set; }

		private Vector2 m_ScrollLeft;

		[MenuItem("Tools/GameDesign/BlueprintUnitCheckerInEditor", false, priority: 4)]
		public static void ShowWindow()
		{
			Show();
		}

		public static void Show()
		{
			var window = GetWindow<BlueprintUnitCheckerInEditorWindow>("BlueprintUnitCheckerInEditor");

			window.ShowAuxWindow();
			window.Focus();
		}
		

		protected override void OnGUI()
		{
			if (Application.isPlaying)
			{
				EditorGUILayout.HelpBox("BlueprintUnitCheckerInEditor doesn't work while the game is running",
					MessageType.Info);
				return;
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope())
				{
					GUILayout.Label("Settings", GUILayout.ExpandWidth(false));


					BlueprintPicker.ShowObjectField(Unit,
						bp => { Unit = bp as BlueprintUnit; },
						new GUIContent("Unit"), typeof(BlueprintUnit));

					CR = EditorGUILayout.IntField("Area Cr", CR);
					Difficulty = (ActualGameDifficultyOption)EditorGUILayout.EnumPopup("Game Difficulty", Difficulty);

					if (Unit != null
					    && GUILayout.Button(new GUIContent("Check"), EditorStyles.toolbarButton, GUILayout
						    .ExpandWidth(true)))
					{
						Results = null;

						using (BlueprintUnitCheckerInEditorContextData.Request().Setup(CR))
						{
							BlueprintUnitCheckerInEditor.ShowUnitStats(Unit, Difficulty);
						}

					}
				}
			}

			using (new EditorGUILayout.VerticalScope())
			{
				using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollLeft))
				{
					m_ScrollLeft = scrollScope.scrollPosition;

					if (Results != null)
					{
						GUILayout.Label("Results", GUILayout.ExpandWidth(true));
						Repaint();

						EditorGUILayout.TextArea($"{Results}");
					}

					GUILayout.Space(15);
				}
			}
		}
		

		public static void DisplayResults(string results)
		{
			Results = results;
		}

		private void OnDestroy()
		{
			BlueprintUnitCheckerInEditorContextData.Current?.Dispose();
			
		}
	}
}