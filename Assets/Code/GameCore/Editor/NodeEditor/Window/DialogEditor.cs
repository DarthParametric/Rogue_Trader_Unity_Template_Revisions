using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Nodes;
using Kingmaker.Editor.Utility;
using Kingmaker.GameModes;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.NodeEditor.Window
{
	public class DialogEditor : NodeEditorBase
	{
		[CanBeNull]
		private BlueprintCue m_GameCue;

		[CanBeNull]
		private BlueprintDialog m_GameDialog;

		private static bool ShowSharedStringMode = true;

		public DialogEditor()
		{
			titleContent = new GUIContent("Dialog Editor");
		}

		[MenuItem("Design/Dialog Editor", false, 2003)]
		public static void ShowWindow()
		{
			GetWindow<DialogEditor>().Show();
		}

        [BlueprintContextMenu("Open in Dialog Editor", BlueprintType = typeof(BlueprintDialog))]
        [BlueprintContextMenu("Open in Dialog Editor", BlueprintType = typeof(BlueprintCueBase))]
        [BlueprintContextMenu("Open in Dialog Editor", BlueprintType = typeof(BlueprintAnswerBase))]
        [BlueprintContextMenu("Open in Dialog Editor", BlueprintType = typeof(BlueprintCheck))]
		public static void OpenAssetInDialogEditor(SimpleBlueprint bp)
		{
			FocusAsset(null, BlueprintEditorWrapper.Wrap(bp));
			ShowSharedStringMode = true;
		}

		public void Update()
		{
			bool focus = false;
			var newGameDialog = DialogDebug.Dialog;
			if (m_GameDialog != newGameDialog)
			{
				m_GameDialog = newGameDialog;
				focus = true;
			}

			if (Application.isPlaying)
			{
				var newGameCue = Game.Instance.DialogController.CurrentCue;
				if (m_GameCue != newGameCue)
				{
					m_GameCue = newGameCue;
					focus = true;
				}
			}

			if (focus)
			{
				FocusAsset(m_GameDialog, m_GameCue, false);
				Repaint();
			}
		}

        public static void FocusAsset([CanBeNull] BlueprintDialog dialog, BlueprintScriptableObject bp, bool focus = true)
        {
            bp = bp ?? dialog;
            if(!bp)
                return;

            if (dialog == null)
            {
                string assetPath = BlueprintsDatabase.GetAssetPath(bp);
                string directory = Path.GetDirectoryName(assetPath);
                var dialogs = BlueprintsDatabase.LoadAllOfType<BlueprintDialog>(directory);
                dialog = dialogs.FirstOrDefault();
            }

            if (dialog == null)
                return;

			var window = GetWindow<DialogEditor>("Dialog Editor", false);
			window.OpenAsset(BlueprintEditorWrapper.Wrap(dialog), BlueprintEditorWrapper.Wrap(bp), focus);
        }

		public static void FocusAsset([CanBeNull] BlueprintDialog dialog, Object asset, bool focus = true)
		{
            if(asset is BlueprintEditorWrapper bew)
            {
                FocusAsset(dialog, bew.Blueprint as BlueprintScriptableObject, focus);
                return;
            }
            
			if (asset == null && dialog == null)
				return;

			if (asset == null)
				asset = BlueprintEditorWrapper.Wrap(dialog);

			if (dialog == null)
			{	
            }

            if (dialog == null)
				return;
            
            FocusAsset(dialog, dialog, focus);
		}

		protected override void ExtraHUDButtons()
		{
			if (GUILayout.Button("SVG Export", GUILayout.ExpandWidth(false)))
			{
				if (Graph != null)
				{
					string fileName = EditorUtility.SaveFilePanel("Export dialogue to SVG", "", Graph.RootAsset.name + ".svg", "svg");
					EditorCoroutine.Start(SvgExportCoroutine(fileName, Graph.ShowExtendedMarkers));
				}
			}
			
			if (GUILayout.Button("JSON Export", GUILayout.ExpandWidth(false)))
			{
				if (Graph != null)
				{
					var pathTemplate = AssetPathUtility.GetFilePath(Graph.RootAsset)
						.Replace("/Blueprints/", $"/DialogMaps/")
						.Replace(".jbp", "");
					
					string fileName = EditorUtility.SaveFilePanel("Export dialogue to JSON", pathTemplate, Graph.RootAsset.name + ".json", "json");
					fileName = new Uri(fileName).LocalPath.Replace("\\", "/");
					JsonExport(fileName);
				}
			}

			if (Graph != null)
			{
				if (GUILayout.Button("Update Comments", GUILayout.ExpandWidth(false)))
				{
					foreach (var node in Graph.Nodes)
					{
						if (node is CheckNode || node is AnswersListNode ||
						    node is CueSequenceExitNode || node is DialogNode)
							continue;
						
						var asset = node.GetAsset() as BlueprintEditorWrapper;
						LocalizationUtility.AddCommentsToJsons(asset);
					}
				}
				
				if (Application.isPlaying && Game.Instance.IsModeActive(GameModeType.Dialog))
				{
					if (GUILayout.Button("Stop Debug Player"))
					{
						StopDebugPlayer();
					}
				}
				else
				{
					if (GUILayout.Button("Start Debug Player"))
					{
						StartDebugPlayer();
					}
				}
				if (Graph.ShowAllVirtualLinks)
				{
					if (GUILayout.Button("Hide Virtual"))
					{
						Graph.ShowAllVirtualLinks = false;
					}
				}
				else
				{
					if (GUILayout.Button("Show Virtual"))
					{
						Graph.ShowAllVirtualLinks = true;
					}
				}

				if (Graph.ShowRelations)
				{
					if (GUILayout.Button("Hide Relations"))
					{
						Graph.ShowRelations = false;
					}
				}
				else
				{
					if (GUILayout.Button("Show Relations"))
					{
						Graph.ShowRelations = true;
					}
				}

                if (Graph.ShowTagButtons)
                {
                    if (GUILayout.Button("Hide tag buttons"))
                    {
                        Graph.ShowTagButtons = false;
                    }
                }
                else
                {
                    if (GUILayout.Button("Show tag buttons"))
                    {
                        Graph.ShowTagButtons = true;
                    }
                }

                if (GUILayout.Button("Check references"))
                {
                    var badRefs = Graph.SearchForOutsideReferences().ToList();
                    if (badRefs.Any())
                    {
                        var msg = "";
                        foreach (var badRef in badRefs)
                        {
                            var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(badRef.Item2)).Substring(7);
                            msg += $"{badRef.Item1.name} references {badRef.Item2.name}\n\tin {path}\n";
                        }
                        EditorUtility.DisplayDialog("Bad references found", msg+"\nSee the UberConsole logs", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No problem", "All references are correct", "OK");
                    }
                }

                DrawSharedStringVisualizerButtons();
               
			}
			base.ExtraHUDButtons();
		}

		private void DrawSharedStringVisualizerButtons()
		{
			if (ShowSharedStringMode)
			{
				if (GUILayout.Button("Show all shared strings"))
				{
					ShowAllSharedStrings();
				}
				return;
			}
			
			if (GUILayout.Button("Hide shared strings"))
			{
				HideSharedStrings();
			}
		}

		private void ShowAllSharedStrings()
		{
			if (Graph != null)
			{
				var notSharedStringNodes = new List<EditorNode>();
				foreach (var node in Graph.Nodes)
				{
					foreach (EditorNode virtualChild in node.VirtualChildren.Values)
						notSharedStringNodes.Add(virtualChild);
					
					var w = node.GetAsset() as BlueprintEditorWrapper;
					var blueprint = w.Blueprint;
					if (blueprint == null)
						continue;

					if (blueprint is BlueprintCue cue && cue.Text.Shared)
					{
						continue;
					}

					if (blueprint is BlueprintAnswer answer && answer.Text.Shared)
					{
						continue;
					}

					notSharedStringNodes.Add(node);
				}

				foreach (var node in notSharedStringNodes)
				{
					node.FadeOut = true;
				}

				ShowSharedStringMode = false;
			}
		}

		private void HideSharedStrings()
		{
			foreach (var node in Graph.Nodes)
			{
				foreach (EditorNode virtualChild in node.VirtualChildren.Values)
					virtualChild.FadeOut = false;
				
				var w = node.GetAsset() as BlueprintEditorWrapper;
				var blueprint = w.Blueprint;
				if (blueprint == null)
					continue;

				if (blueprint is BlueprintCue cue && cue.Text.Shared)
				{
					continue;
				}

				if (blueprint is BlueprintAnswer answer && answer.Text.Shared)
				{
					continue;
				}

				node.FadeOut = false;
			}

			ShowSharedStringMode = true;
		}

		private void StartDebugPlayer()
		{
            var player = FindObjectOfType<DebugDialogPlayer>();
            if (!Application.isPlaying)
            {
                if (!player)
                {
                    const string scene = "5e148b44b3a962e409edbec776a57568";
                    EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(scene), OpenSceneMode.Single);
                    player = FindObjectOfType<DebugDialogPlayer>();
                }

                if (player)
                {
                    player.Dialog = BlueprintEditorWrapper.Unwrap<BlueprintDialog>(RootAsset);
                    EditorApplication.isPlaying = true;
                }
            }
            else
            {
                if (player)
                {
                    player.Dialog = BlueprintEditorWrapper.Unwrap<BlueprintDialog>(RootAsset);
                    player.StartDialog();
                }
            } 
        }

		private void StopDebugPlayer()
		{
			if (!Application.isPlaying)
			{
				return;
			}

			var player = FindObjectOfType<DebugDialogPlayer>();
			if (player)
			{
				player.StopDialog();
			}
		}

		protected override Type GetOpenType()
		{
			return typeof(BlueprintDialog);
		}
	}
}