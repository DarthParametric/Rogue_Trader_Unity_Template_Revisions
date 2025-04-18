using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Controllers.Dialog;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.NodeEditor.Nodes;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Utility.CountingGuard;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.NodeEditor.Window
{
	public abstract class NodeEditorBase : EditorWindow
	{
		public static CountingGuard DrawAllNodes = new CountingGuard();

		public static CountingGuard SingleLineMarkers = new CountingGuard();

		private static int s_DrawingCount;

		public static EditorNode CurrentNode;

		public static bool Drawing
		{
			get { return s_DrawingCount > 0; }
		}
		
		[SerializeField]
		private string m_RootAssetGuid;

		protected ScriptableObject RootAsset;

		private ScriptableObject m_AssetToFocus;

		public CanvasView View = new CanvasView();

		public Graph Graph;

		private string m_SearchFilter = "";

		private List<EditorNode> m_SearchNodes = new List<EditorNode>();

		protected NodeEditorBase()
		{
			Undo.undoRedoPerformed += OnUndo;
			Selection.selectionChanged += OnSelectionChanged;
			BlueprintScriptableObject.ChangedEvent += OnBlueprintChanged;
		}
		
		private void OnEnable()
		{
			var obj = BlueprintsDatabase.LoadById<SimpleBlueprint>(m_RootAssetGuid);
			var asset = BlueprintEditorWrapper.Wrap(obj);
			if (asset && !RootAsset)
			{
				RootAsset = asset;
				Graph = new Graph(this, asset);
			}
			TryRestoreNodesFoldoutState();
		}

		private void SaveNodesFoldoutStateAndComments()
		{
			if (Graph != null)
			{
				foreach (var node in Graph.Nodes)
				{
					var w = node.GetAsset() as BlueprintEditorWrapper;
					var blueprint = w.Blueprint;
					if (blueprint == null)
						continue;

					if (blueprint is IEditorCommentHolder commentHolder && commentHolder.EditorComment.Foldout != node.Foldout)
					{
						commentHolder.EditorComment.Foldout = node.Foldout;
						BlueprintsDatabase.SetDirty(blueprint.AssetGuid);
					}
				}
				
				BlueprintsDatabase.SaveAllDirty();
			}
		}
		
		private void TryRestoreNodesFoldoutState()
		{
			if (Graph != null)
			{
				foreach (var node in Graph.Nodes)
				{
					var w = node.GetAsset() as BlueprintEditorWrapper;
					var blueprint = w.Blueprint;
					if (blueprint == null)
						continue;
					
					if (blueprint is IEditorCommentHolder commentHolder && commentHolder.EditorComment.Foldout != node.Foldout)
					{
						node.Foldout = commentHolder.EditorComment.Foldout;
					}
				}
			}
		}

		public void OpenAsset([NotNull] ScriptableObject rootAsset, [CanBeNull] Object focusAsset = null, bool focus = true)
		{
			bool newWindow = RootAsset == null;
			m_AssetToFocus = focusAsset as ScriptableObject;
			if (Graph == null || RootAsset != rootAsset)
			{
				RootAsset = rootAsset;
				m_RootAssetGuid = (RootAsset as BlueprintEditorWrapper)?.Blueprint.AssetGuid.ToString();
				bool oldExtendedMarkers = Graph != null && Graph.ShowExtendedMarkers;
				Graph = new Graph(this, rootAsset);
				TryRestoreNodesFoldoutState();
				Graph.ShowExtendedMarkers = oldExtendedMarkers;
			}

			if (newWindow)
				Show();
			else
				Repaint();
			if (focus)
				Focus();
		}

		public static void OnPropertyDrawn(SerializedProperty property)
		{
			// todo: [bp] fix InspectorEditorNode
		}

		protected virtual void OnGUI()
		{

			Profiler.BeginSample("NodeEditor.OnGUI: " + Event.current.type);
			s_DrawingCount++;
			try
			{
				View.Update(this);

				Profiler.BeginSample("HUD() 1");
				HUD();
				Profiler.EndSample();

				if (View.NeedsScale)
					EditorZoomArea.Begin(View.Scale, position);

				AssetCreationController.Update(Graph);
				AssetReorderController.Update(Graph);

				Profiler.BeginSample("Nodes");
				BeginWindows();
				if (Graph != null)
				{
					foreach (var node in Graph.Nodes)
					{
						if (node.Parent is {Foldout: true})
						{
							continue;
						}

						node.Draw(View);
					}
				}

				EndWindows();
				CurrentNode = null;
				Profiler.EndSample();

				Profiler.BeginSample("Nodes Check");
				if (Graph != null)
					if (Graph.Nodes.Any(n => n.GetAsset() == null))
						Graph.ReloadGraph();
				Profiler.EndSample();

				if (Event.current.type == EventType.Repaint)
				{
					Profiler.BeginSample("Connections");
					if (Graph != null)
						Graph.Nodes.ForEach(n => n.DrawConnections(View, n.Foldout));
					Profiler.EndSample();
				}

				Profiler.BeginSample("Debug");
				if (!Application.isPlaying)
					DialogDebug.DebugMessages.Clear();
				if (DialogDebug.DebugMessages.Count > 0)
				{
					if (Graph != null)
						Graph.Nodes.ForEach(n => n.DrawDebug(View));
				}
				Profiler.EndSample();

				DragAndDropController.UpdateStartDrag();
				NodeRemovalController.Update(Graph);

				if (View.NeedsScale)
					EditorZoomArea.End();

				Profiler.BeginSample("HUD() 2");
				HUD();
				Profiler.EndSample();

				Profiler.BeginSample("Graph Update");
				if (Graph != null)
				{
					if (Graph.IsLayoutCompleted())
						OnGraphLayoutCompleted();
					Graph.Update();
				}
				Profiler.EndSample();

				if (Event.current.type == EventType.MouseDrag)
					Repaint();
			}
			finally
			{
				Profiler.EndSample();
				s_DrawingCount--;
			}
		}

		private void HUD()
		{
			GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
			GUI.color = Color.white;
			using (GuiScopes.Horizontal())
			{
				using (GuiScopes.Vertical())
				{
					using (GuiScopes.Horizontal())
					{
						HUDButtons();
					}
					using (GuiScopes.Horizontal())
					{
						ExtraHUDButtons();
					}
				}

				GUILayout.Space(45f);
				using (GuiScopes.Vertical())
				{
					SearchHUD();
				}
			}
			GUILayout.EndArea();
		}

		private void HUDButtons()
		{
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false)))
            {
                BlueprintPicker.ShowAssetPicker(GetOpenType(), null, o=> OpenAsset(BlueprintEditorWrapper.Wrap(o)));
			}

            if (GUILayout.Button("Layout", GUILayout.ExpandWidth(false)))
				if (Graph != null)
					Graph.Layout();
			if (GUILayout.Button("Reload", GUILayout.ExpandWidth(false)))
				if (Graph != null)
					Graph.ReloadGraph();

			if (GUILayout.Button("New Window", GUILayout.ExpandWidth(false)))
			{
				var window = CreateInstance(GetType()) as NodeEditorBase;
				if (window)
				{
					window.Show();
					window.Focus();
				}
			}
			
			if (GUILayout.Button("Save Nodes Foldout State and EditorComments", GUILayout.ExpandWidth(false)))
				SaveNodesFoldoutStateAndComments();

			if (Graph != null)
			{
				if (Graph.ShowExtendedMarkers)
				{
					if (GUILayout.Button("Simple Markers"))
					{
						Graph.ShowExtendedMarkers = false;
					}
				}
				else
				{
					if (GUILayout.Button("Extended Markers"))
					{
						Graph.ShowExtendedMarkers = true;
					}
				}
			}
		}

		private void SearchHUD()
		{
			bool showNext = false;
			using (GuiScopes.Horizontal())
			{
				if (Graph == null)
				{
					return;
				}
				GUILayout.Label("Search");

				var style = new GUIStyle(GUI.skin.textField);
				if (GUI.GetNameOfFocusedControl() == "SearchFilter")
				{
					if (Event.current.type == EventType.KeyDown)
					{
						if (Event.current.character == '\n'
							|| Event.current.keyCode == KeyCode.KeypadEnter
							|| Event.current.keyCode == KeyCode.Return)
						{
							Event.current.Use();
							showNext = true;
						}
					}
					style.normal = style.focused;
				}

				GUI.SetNextControlName("SearchFilter");

				string prevSearchFilter = m_SearchFilter;
				m_SearchFilter = GUILayout.TextField(m_SearchFilter, style, GUILayout.MinWidth(400), GUILayout.MaxWidth(400));

				if (m_SearchFilter != prevSearchFilter)
				{
					UpdateSearch();
				}

				GUILayout.FlexibleSpace();
			}

			if (m_SearchFilter != "")
			{
				using (GuiScopes.Horizontal())
				{
					if (GUILayout.Button("<"))
					{
						int index = m_SearchNodes.IndexOf(Graph.SelectedNode) - 1;
						index += m_SearchNodes.Count;
						index %= m_SearchNodes.Count;

						if (index >= 0 && index < m_SearchNodes.Count)
						{
							m_AssetToFocus = m_SearchNodes[index].GetAsset();
						}
					}

					if (GUILayout.Button(">") || showNext)
					{
						int index = m_SearchNodes.IndexOf(Graph.SelectedNode) + 1;
						index += m_SearchNodes.Count;
						index %= m_SearchNodes.Count;

						if (index >= 0 && index < m_SearchNodes.Count)
						{
							m_AssetToFocus = m_SearchNodes[index].GetAsset();
						}
					}

					if (GUILayout.Button("Clear"))
					{
						m_SearchFilter = "";
						UpdateSearch();
					}

					GUILayout.FlexibleSpace();
				}
			}
		}

		private void UpdateSearch()
		{
			if (m_SearchFilter == "")
			{
				m_SearchNodes = new List<EditorNode>();
				Graph.LayoutOrderNodes.ForEach(n => n.FadeOut = false);
			}
			else
			{
				string filterLower = m_SearchFilter.ToLowerInvariant();
				m_SearchNodes = Graph.LayoutOrderNodes.Where(n => n.MatchesFilter(filterLower)).ToList();
				Graph.LayoutOrderNodes.ForEach(n => n.FadeOut = true);
				m_SearchNodes.ForEach(n => n.FadeOut = false);
				if (m_SearchNodes.Count > 0)
				{
					m_AssetToFocus = m_SearchNodes[0].GetAsset();
				}
			}
		}

		protected virtual void ExtraHUDButtons()
		{			
			if (EditorLocalizationManager.ShowTextStatus)
			{
				if (GUILayout.Button("Hide Text Status"))
				{
					EditorLocalizationManager.ShowTextStatus = false;
				}
			}
			else
			{
				if (GUILayout.Button("Show Text Status"))
				{
					EditorLocalizationManager.ShowTextStatus = true;
				}
			}
			LocalizationManager.Instance.CurrentLocale =
				(Locale)
				EditorGUILayout.EnumPopup(LocalizationManager.Instance.CurrentLocale, GUILayout.ExpandWidth(false), GUILayout.Width(50));
		}

		private void OnUndo()
		{
			if (Graph != null)
			{
				Graph.CheckForNewNodes();
				Graph.Layout();
			}
		}

		public void OnGraphLayoutCompleted()
		{
			if (m_AssetToFocus == null)
				return;

			if (Graph == null)
				return;

			var selected = m_AssetToFocus;
			m_AssetToFocus = null;
			if (!Graph.ContainsNode(selected))
				return;

			if (Graph.SelectedNode != null && Graph.SelectedNode.GetAsset() == selected)
				return;

			var node = Graph.GetNode(selected);
			Graph.SelectedNode = node;
			View.CenterOn(node.Center, position);
			Repaint();
		}

		private void OnSelectionChanged()
		{
			m_AssetToFocus = Selection.activeObject as ScriptableObject;
			Repaint();
		}

		private void OnBlueprintChanged(BlueprintScriptableObject blueprint)
		{
			try
			{
				if (Graph == null)
					return;
				if (Graph.ContainsNode(blueprint))
					Repaint();
			}
			catch
			{
				// ignored
			}
		}

		public IEnumerator SvgExportCoroutine(string fileName, bool markers)
		{
			if (Graph == null)
				yield break;
			
			try
			{
				DrawAllNodes++;
				if (markers)
				{
					SingleLineMarkers++;
					Graph.Layout();
					yield return null;
				}

				Repaint();
				yield return null;
			}
			finally
			{
				if (markers)
				{
					SingleLineMarkers--;
				}
				DrawAllNodes--;
			}

			if (Graph == null)
				yield break;

			var svg = new SvgWriter();
			foreach (var node in Graph.Nodes)
			{
				svg.DrawNode(node, markers);
				foreach (var child in node.VirtualNodes)
				{
					svg.DrawNode(child, false);
				}
			}

			svg.Save(fileName);
		}

		protected abstract Type GetOpenType();
	}

	public static class NodeEditorBaseExtensions
	{
		public static void OpenCloseAllChildren(this EditorNode node, bool foldoutState)
		{
			node.Foldout = foldoutState;

			foreach (var child in node.GetReferencedNodes())
			{
				child.OpenCloseAllChildren(foldoutState);
			}
		}
	}
}