using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.PubSubSystem;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Cutscenes
{
	public class CutsceneManagerEditorWindow : EditorWindow, ICutsceneHandler
	{
		private const string DefaultTitle = "Cutscene Manager";
		private Vector2 m_ScrollPos;
		
		//make some sort of table and calculate layout widths relative to window width
		private const float WidthCoeffForName = 0.3f;
		private const float WidthCoeffForStatus = 0.5f;
		private const float WidthCoeffOpenButton = 0.2f;
		private const int HeightInLines = 2;
		
		[MenuItem("Design/Cutscene Manager")]
		public static void OpenCutsceneEditor()
		{
			GetWindow<CutsceneManagerEditorWindow>();
		}
		
		public void OnGUI()
		{
			if (titleContent.text != DefaultTitle)
				titleContent = new GUIContent(DefaultTitle);

			var cutscenePlayerList = Application.isPlaying ? Game.Instance.State.Cutscenes : null;
			using (var scope = new EditorGUILayout.ScrollViewScope(m_ScrollPos))
			{
				m_ScrollPos = scope.scrollPosition;
				float windowWidth = position.width;
				GUIStyle style = new GUIStyle();
				style.richText = true;
				style.wordWrap = true;
				//table header
				using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Width(windowWidth), GUILayout.Height(HeightInLines * EditorGUIUtility.singleLineHeight))) 
				{
					GUILayout.Label("<b><color=grey>Running cutscenes</color></b>", style, GUILayout.Width(WidthCoeffForName * windowWidth));
					GUILayout.Label("<b><color=grey>Status</color></b>", style, GUILayout.Width(WidthCoeffForStatus * windowWidth));
					//GUILayout.FlexibleSpace();
					GUILayout.Label("<b><color=grey>Origin blueprint</color></b>", style, GUILayout.Width(WidthCoeffOpenButton * windowWidth));
					GUILayout.Label("<b><color=grey>Open in Editor</color></b>", style, GUILayout.Width(WidthCoeffOpenButton * windowWidth));
				}

				if (cutscenePlayerList == null)
					return;
				
				foreach (var cutscenePlayer in cutscenePlayerList)
				{
					using (new EditorGUILayout.HorizontalScope(GUILayout.Width(windowWidth), GUILayout.Height(HeightInLines * EditorGUIUtility.singleLineHeight)))
					{
						GUILayout.Label($"<color=grey>{cutscenePlayer.Cutscene.NameSafe()}</color>", style, GUILayout.Width(WidthCoeffForName * windowWidth));
						
						string status = "Playing";
						string color = "grey";
						if (cutscenePlayer.IsFinished && !cutscenePlayer.FailedCommands.Empty())
						{
							status = "Finished with commands failed";
							color = "red";
						} else if (cutscenePlayer.IsFinished && !cutscenePlayer.LogList.Empty())
						{
							status = "Finished with some errors";
							color = "red";
						} else if (cutscenePlayer.IsFinished)
						{
							status = "Finished successfully";
							color = "green";
						} else if (cutscenePlayer.Paused) 
						{
							status = "Paused because ";
							color = "yellow";
							switch (cutscenePlayer.LastHandledReason)
							{
								case CutscenePauseReason.HasNoActiveAnchors:
									status += "has no active anchors";
									break;
								case CutscenePauseReason.UnitSpawnerDoesNotControlAnyUnit:
									status += " has problem with UnitSpawner";
									break;
								case CutscenePauseReason.ManualPauseFromEditor:
									status += " was manually paused";
									break;
								case CutscenePauseReason.GameModePauseBackgroundCutscenes:
									status += " was paused by GameMode";
									break;
								case CutscenePauseReason.MarkedUnitControlledByOtherCutscene:
									status += " has unit controlled by other cutscene";
									break;
							}
						} else if (!cutscenePlayer.FailedCommands.Empty() || !cutscenePlayer.LogList.Empty())
						{
							status = "Playing with errors";
							color = "red";
						}

						GUILayout.Label($"<b><color={color}>{status}</color></b>", style, GUILayout.Width(WidthCoeffForStatus * windowWidth));
						GUILayout.Label($"<color=grey>{(cutscenePlayer.OriginBlueprint?.name ?? "")}</color>", style, GUILayout.Width(WidthCoeffForName * windowWidth));
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(WidthCoeffOpenButton * windowWidth)))
						{
							CutsceneEditorWindow.OpenPlayerInEditor(cutscenePlayer.View);
						}
					}
				}
			}
		}

		public void HandleCutsceneStarted(bool queued)
		{
			Repaint();
		}

		public void HandleCutsceneRestarted()
		{
			Repaint();
		}

		public void HandleCutscenePaused(CutscenePauseReason reason)
		{
			Repaint();
		}

		public void HandleCutsceneResumed()
		{
			Repaint();
		}

		public void HandleCutsceneStopped()
		{
			Repaint();
		}
		
		public void OnInspectorUpdate()
		{
			Repaint();
		}
	}
}