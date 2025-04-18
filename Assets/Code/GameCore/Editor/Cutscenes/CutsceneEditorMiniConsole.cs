using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Owlcat.Editor.Logging;
using Owlcat.Runtime.Core.Logging;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Cutscenes
{
	public class CutsceneEditorMiniConsole
	{
		public enum ShowMode
		{
			PauseReason = 1,
			ErrorsLog = 2,
			ErrorCallstack = 3,
			Unknown = 0
		}

		private CutsceneEditorWindow m_EditorWindow;

		private ShowMode m_ShowMode = ShowMode.Unknown;
		public ShowMode ConsoleShowMode
		{
			get => m_ShowMode;
			set
			{
				if (value != ShowMode.ErrorCallstack)
					ShowErrorLogCommand = null;
				if (value != m_ShowMode)
					NeedRepaint = true;
				m_ShowMode = value;
			}
		}

		private Cutscene m_CurrentCutscene;
		private CutscenePlayerData m_CurrentPlayerData;

		//if we change mode or close console CutsceneEditorWindow should call Repaint()
		public bool NeedRepaint = false;
		
		//TODO: changing size
		private const int ConsoleHeightInLines = 4;
		private Vector2 m_ScrollPausePosition;
		private Vector2 m_ScrollErrorPosition;
		private Vector2 m_ScrollFailedCommandPosition;

		//styles for ShowMode.ErrorsLog
		private GUIStyle m_EntryStyleDefaultEven = null;
		private GUIStyle m_EntryStyleDefaultOdd = null;
		private GUIStyle m_EntryStyleSelected = null;
		private Texture2D m_ErrorIcon = null;

		//for ShowMode.ErrorCallstack if we double-clicked on failed command
		public CommandBase ShowErrorLogCommand = null;
		//for ShowMode.ErrorCallstack if we double-clicked on error in log
		private int m_SelectedErrorIndex = -1;
		private LogInfo m_SelectedErrorInfo;

		public CutsceneEditorMiniConsole(CutsceneEditorWindow editorWindow)
		{
			m_EditorWindow = editorWindow;
		}

		public void DrawHelpfulInfo()
		{
			m_CurrentCutscene = m_EditorWindow.Cutscene;
			m_CurrentPlayerData = m_EditorWindow.DebuggedPlayer != null ? m_EditorWindow.DebuggedPlayer.PlayerData : null;
			
			if (ConsoleShowMode == ShowMode.Unknown)
				ConsoleShowMode = ShowMode.PauseReason;
			
			if (m_EntryStyleDefaultEven == null)
				InitStyles();

			DrawToolbar();
			DrawMainBody();
		}

		private void InitStyles()
		{
			GUIStyle unitySmallLogLine = null;
			foreach (var style in GUI.skin.customStyles)
				if (style.name == "CN StatusInfo") unitySmallLogLine = style;

			m_EntryStyleDefaultEven = new GUIStyle(unitySmallLogLine);
			m_EntryStyleDefaultEven.margin = new RectOffset(0, 0, 0, 0);
			m_EntryStyleDefaultEven.border = new RectOffset(0, 0, 0, 0);
			m_EntryStyleDefaultEven.fixedHeight = 0;
			
			m_EntryStyleDefaultOdd = new GUIStyle(m_EntryStyleDefaultEven);
			m_EntryStyleDefaultOdd.normal.background = Texture2D.grayTexture;

			m_EntryStyleSelected = new GUIStyle(m_EntryStyleDefaultEven);
			m_EntryStyleSelected.normal.textColor = Color.red;
			m_EntryStyleSelected.active = m_EntryStyleSelected.focused = m_EntryStyleSelected.hover = m_EntryStyleSelected.normal;
			
			m_ErrorIcon = EditorGUIUtility.FindTexture("d_console.erroricon.sml");
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Width(m_EditorWindow.position.width)))
			{
				if (GUILayout.Button("Pause Reason", EditorStyles.toolbarButton))
				{
					ConsoleShowMode = ShowMode.PauseReason;
				}
				if (GUILayout.Button("Errors Log", EditorStyles.toolbarButton))
				{
					ConsoleShowMode = ShowMode.ErrorsLog;
				}
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(
					new GUIContent {text = "Trace", tooltip = "Show command trace in Uber Console Log"},
					EditorStyles.toolbarButton))
				{
					ShowTraceLogInUberConsole();
				}
				if (GUILayout.Button(
					new GUIContent {text = "Export", tooltip = "Export command trace log to file"},
					EditorStyles.toolbarButton))
				{
					ExportTraceLog();
				}
				if (GUILayout.Button("Close", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
				{
					m_EditorWindow.MiniConsoleActive = false;
					NeedRepaint = true;
				}
			}
		}

		private static void ShowTraceLogInUberConsole()
		{
			if (!EditorWindow.HasOpenInstances<UberLoggerEditorWindow>())
			{
				return;
			}

			var window = EditorWindow.GetWindow<UberLoggerEditorWindow>(false, null, false);
			window.ExcludedChannels.Clear();
			window.ShowByDefault = false;
			window.ToggleChannel(PFLog.Cutscene.Name);
		}

		private void ExportTraceLog()
		{
			var renderLogs = new List<LogInfo>();
			if (m_CurrentPlayerData != null)
			{
				renderLogs = m_CurrentPlayerData.LogList;
			}
			else
			{
				if (!EditorUtility.DisplayDialog(
					"Export failed",
					"No command trace logs were recorded.\nExport from Uber Console Log instead?",
					"Yes",
					"No"))
				{
					return;
				}
				var logs = EditorLogStorage.Instance.CopyLogInfo();
				renderLogs.AddRange(logs.Where(l => l.Channel == PFLog.Cutscene && l.Severity == LogSeverity.Message));
			}

			if (renderLogs.Count <= 0)
			{
				EditorUtility.DisplayDialog(
					"Export failed",
					"No command trace logs were found.",
					"Ok",
					"");
				return;
			}

			var sb = new StringBuilder();
			foreach (var logInfo in renderLogs)
			{
				sb.AppendLine(logInfo.GetSingleString());
			}

			string path = EditorUtility.SaveFilePanel("Export to file", "", "trace.log", "log");
			if (string.IsNullOrEmpty(path))
				return;

			File.WriteAllText(path, sb.ToString());
		}

		private void DrawMainBody()
		{
			switch (ConsoleShowMode)
			{
				case ShowMode.PauseReason :
					DrawPauseReason();
					break;
				case ShowMode.ErrorsLog:
					DrawErrorsLog();
					break;
				case ShowMode.ErrorCallstack:
					DrawCommandError();
					break;
				default: 
					Debug.Log("Warning! Something wrong with CutsceneEditorMiniConsole mode");
					break;
			}
			
		}

		private void DrawPauseReason()
		{
			using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(
				scrollPosition: m_ScrollPausePosition,
				alwaysShowHorizontal: false,
				alwaysShowVertical: false,
				horizontalScrollbar: GUIStyle.none,
				verticalScrollbar: GUI.skin.verticalScrollbar,
				background: GUIStyle.none,
				GUILayout.Width(m_EditorWindow.position.width), 
				GUILayout.Height(EditorGUIUtility.singleLineHeight * ConsoleHeightInLines)))
			{
				m_ScrollPausePosition = scrollViewScope.scrollPosition;
				string message = "";
				bool isThisAButton = false;
				AbstractUnitEntity controlledUnit = null;
				
				if (m_CurrentPlayerData != null && m_CurrentPlayerData.Paused)
				{
					message = $"Cutscene <b>{m_CurrentCutscene.name}</b> ({m_CurrentCutscene.AssetGuid})";
					switch (m_CurrentPlayerData.LastHandledReason)
					{
						case CutscenePauseReason.HasNoActiveAnchors:
							message += "has <b>no active anchors</b>";
							break;
						case CutscenePauseReason.UnitSpawnerDoesNotControlAnyUnit:
							message += " has <b>problem with UnitSpawner</b>. It doesn't control any unit";
							break;
						case CutscenePauseReason.ManualPauseFromEditor:
							message += " was <b>manually paused</b>";
							break;
						case CutscenePauseReason.GameModePauseBackgroundCutscenes:
							message += " was <b>paused by GameMode</b>";
							break;
						case CutscenePauseReason.MarkedUnitControlledByOtherCutscene:
							var currentPlayerData = m_EditorWindow.DebuggedPlayer.PlayerData;
							var controlledUnits = currentPlayerData.GetCurrentControlledUnits();
							Cutscene otherCutscene = null;
							foreach (var unit in controlledUnits)
							{
								var playerDataInControl = CutsceneControlledUnit.GetControllingPlayer(unit);
								if (playerDataInControl.Cutscene != null && playerDataInControl != currentPlayerData)
								{
									otherCutscene = playerDataInControl.Cutscene;
									controlledUnit = unit;
									break;
								}
							}
							if (controlledUnit != null) {
								message += $" has unit <b>{controlledUnit.Blueprint}</b>";
								isThisAButton = true;
							}
							else
								message += $" has unit [null]";

							if (otherCutscene != null)
								message += $" <b>controlled by cutscene</b> {otherCutscene} ({otherCutscene.AssetGuid})";
							else
								message += $" <b>controlled by cutscene</b> [null]";
							break;
					}
				}
				else
				{
					message = "No reasons to pause... ";
				}
				GUIStyle style = new GUIStyle(m_EntryStyleDefaultEven);
				style.richText = true;
				style.wordWrap = true;
				GUILayout.Label(message, style, GUILayout.Width(m_EditorWindow.position.width));
				var labelRect = GUILayoutUtility.GetLastRect();
				if (isThisAButton)
				{
					if (GUI.Button(labelRect, GUIContent.none, GUIStyle.none))
						Selection.activeObject = controlledUnit.View;
				}
				GUILayout.FlexibleSpace();
			}
		}

		private void DrawErrorsLog()
		{
			float singleLineHeight = EditorGUIUtility.singleLineHeight;
			using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(
				scrollPosition: m_ScrollErrorPosition,
				alwaysShowHorizontal: false,
				alwaysShowVertical: false,
				horizontalScrollbar: GUIStyle.none,
				verticalScrollbar: GUI.skin.verticalScrollbar,
				background: GUIStyle.none,
				GUILayout.Width(m_EditorWindow.position.width),
				GUILayout.Height(singleLineHeight * ConsoleHeightInLines)))
			{
				m_ScrollErrorPosition = scrollViewScope.scrollPosition;
				if (m_CurrentPlayerData == null)
				{
					GUILayout.Label("Cutscene doesn't play", EditorStyles.wordWrappedLabel, GUILayout.Width(m_EditorWindow.position.width));
				}
				else
				{
					var renderLogs = m_CurrentPlayerData.LogList;

					for (int i = 0; i < renderLogs.Count; ++i)
					{
						var logInfo = renderLogs[i];
						var entryStyle = (i % 2 == 0) ? m_EntryStyleDefaultEven : m_EntryStyleDefaultOdd;
						entryStyle = (i == m_SelectedErrorIndex) ? m_EntryStyleSelected : entryStyle;
						if (logInfo.Severity == LogSeverity.Error)
							GUILayout.Label(new GUIContent(logInfo.GetSingleString(true), m_ErrorIcon), entryStyle, GUILayout.Width(m_EditorWindow.position.width));
						else
							GUILayout.Label(logInfo.GetSingleString(true), entryStyle, GUILayout.Width(m_EditorWindow.position.width));
						var labelRect = GUILayoutUtility.GetLastRect();
						if (GUI.Button(labelRect, GUIContent.none, GUIStyle.none))
						{
							//first click - select entry
							if (m_SelectedErrorIndex != i)
							{
								m_SelectedErrorIndex = i;
							}
							//second click - change mode and show log with callstack
							else
							{
								ConsoleShowMode = ShowMode.ErrorCallstack;
								m_SelectedErrorInfo = logInfo;
								m_SelectedErrorIndex = -1;
							}
						}
					}
				}
			}
		}

		private void DrawCommandError()
		{
			using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(
				scrollPosition: m_ScrollFailedCommandPosition,
				alwaysShowHorizontal: false,
				alwaysShowVertical: false,
				horizontalScrollbar: GUIStyle.none,
				verticalScrollbar: GUI.skin.verticalScrollbar,
				background: GUIStyle.none,
				GUILayout.Width(m_EditorWindow.position.width),
				GUILayout.Height(EditorGUIUtility.singleLineHeight * ConsoleHeightInLines)))
			{
				m_ScrollFailedCommandPosition = scrollViewScope.scrollPosition;
				LogInfo logInfo = null;
				if (ShowErrorLogCommand == null && m_SelectedErrorInfo == null)
				{
					GUILayout.Label("Console doesn't know what to show. Something went wrong", EditorStyles.wordWrappedLabel, GUILayout.Width(m_EditorWindow.position.width));
					return;
				}
				if (ShowErrorLogCommand != null && !CutsceneLogSink.Instance.TryGetCommandErrorInfo(ShowErrorLogCommand, out logInfo)) 
				{
					GUILayout.Label($"LogSink doesn't know error for {ShowErrorLogCommand}. Something went wrong", EditorStyles.wordWrappedLabel, GUILayout.Width(m_EditorWindow.position.width));
					return;
				}
				if (logInfo == null)
				{
					logInfo = m_SelectedErrorInfo;
				}
				
				GUIStyle style = new GUIStyle(m_EntryStyleDefaultEven);
				style.richText = true;
				style.wordWrap = true;
				GUILayout.Label(new GUIContent($"<b><color=red>{logInfo.GetFormattedMessage()}</color></b>", m_ErrorIcon), style, GUILayout.Width(m_EditorWindow.position.width));
				if (logInfo.Callstack != null)
				{
					foreach (var frame in logInfo.Callstack)
					{
						string methodName = frame.GetFormattedMethodName();
						if (!String.IsNullOrEmpty(methodName))
							GUILayout.Label(methodName, GUILayout.Width(m_EditorWindow.position.width));
					}
				}
			}
		}
	}
}