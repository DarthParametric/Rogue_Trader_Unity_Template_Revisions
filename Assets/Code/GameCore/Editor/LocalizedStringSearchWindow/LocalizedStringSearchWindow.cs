using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.DialogSystem.Interfaces;
using Kingmaker.Editor;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor.LocalizedStringSearchWindow
{
	public class LocalizedStringSearchWindow : KingmakerWindowBase
	{
		private BlueprintScriptableObject m_Target;
		private List<BlueprintScriptableObject> Results { get; } = new();
		private Vector2 m_Scroll;

		private bool m_HideFindButton = false;
		
		[MenuItem("Tools/Narrative and Text/LocalizedString Search In Cues and Answers", false)]
		public static void ShowWindow()
		{
			Show();
		}
		
		public static void Show()
		{
			var window = GetWindow<LocalizedStringSearchWindow>("LocalizedStringSearchInCuesAndAnswers");

			window.ShowAuxWindow();
			window.Focus();
		}
		
		protected override void OnGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope())
				{
					GUILayout.Label("This tool helps you search for usages of SharedString in BlueprintCue and BlueprintAnswer", EditorStyles.centeredGreyMiniLabel);
					GUILayout.Label("Insert bp with SharedString in it and press Find", EditorStyles.centeredGreyMiniLabel);
					
					BlueprintPicker.ShowObjectField(m_Target,
						bp => { m_Target = bp as BlueprintScriptableObject; }, new GUIContent("Blueprint"), typeof(BlueprintScriptableObject));
					
					if ( m_Target is not ILocalizedStringHolder)
					{
						EditorGUILayout.HelpBox("Blueprint must be only BlueprintAnswer or BlueprintCue", MessageType.Info);
						m_HideFindButton = true;
					}
					else
					{
						m_HideFindButton = false;
					}

					if (m_Target is ILocalizedStringHolder blueprint && !blueprint.LocalizedStringText.Shared)
					{
						EditorGUILayout.HelpBox("This blueprint doesn't contain SharedString in Text field", MessageType.Info);
					}

					if (!m_HideFindButton && GUILayout.Button("Find"))
					{
						FindTarget();
					}
				}
			}
			
			DisplayResults();
		}

		private void FindTarget()
		{
			Results.Clear();
			if (m_Target is BlueprintCue)
			{
				var allCues = BlueprintsDatabase.LoadAllOfType<BlueprintCue>("Blueprints");
				
				foreach (var blueprint in allCues.NotNull())
				{
					if (!blueprint.Text.Shared)
					{
						continue;
					}

					if (blueprint.Text.Shared.Equals((m_Target as ILocalizedStringHolder)?.LocalizedStringText.Shared))
					{
						Results.Add(blueprint);
					}
				}

				return;
			}
			
			if (m_Target is BlueprintAnswer)
			{
				var allAnswers = BlueprintsDatabase.LoadAllOfType<BlueprintAnswer>("Blueprints");

				foreach (var blueprint in allAnswers.NotNull())
				{
					if (!blueprint.Text.Shared)
					{
						continue;
					}

					if (blueprint.Text.Shared.Equals((m_Target as ILocalizedStringHolder)?.LocalizedStringText.Shared))
					{
						Results.Add(blueprint);
					}
				}
			}
		}

		private void DisplayResults()
		{
			using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll))
			{
				m_Scroll = scope.scrollPosition;

				if (Results.Empty())
				{
					GUILayout.Label("Results are empty");
					return;
				}

				foreach (var result in Results.EmptyIfNull())
				{
					if (!result)
					{
						GUILayout.Label("null?");
						continue;
					}

					bool selected = Selection.Contains(BlueprintEditorWrapper.Wrap(result));
					var bg = GUI.backgroundColor;
					GUI.backgroundColor = selected ? GUI.skin.settings.selectionColor : bg;
					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button(result.name, GUI.skin.box, GUILayout.ExpandWidth(true)))
						{
							Selection.activeObject = BlueprintEditorWrapper.Wrap(result);
						}
					}
				}
			}
		}
	}
}