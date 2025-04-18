using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Cutscenes;
using Kingmaker.Editor.DragDrop;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.View;
using Owlcat.Editor.Core.Utility;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEngine;
using AreaInfo = Kingmaker.Editor.Utility.AreaInfo;
using AreaInfoCollector = Kingmaker.Editor.Utility.AreaInfoCollector;
using Object = UnityEngine.Object;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Workspace
{
	public class WorkspaceCanvasWindow : KingmakerWindowBase
	{
		private const int InfoPanelWidth = 400;

		private List<WorkspaceItemBase> m_Items;
		private bool m_IsDraggingSelection;
		private Vector2 m_SelectionFrom;
		private Vector2 m_SelectionTo;
		private Vector2 m_CanvasSize;

		[SerializeField]
		private string m_SavedPreset;

		[SerializeField]
		private bool m_SnapToGrid;

		private Vector2 m_ScrollPos;
		private Vector2 m_InfoScrollPos;

		private AreaInfo m_AreaInfo;

		[SerializeField]
		private bool m_ShowAreaInfo = true;

		[SerializeField]
		private WorkspacePreset m_Preset;

		private readonly Dictionary<string, bool> m_Foldouts = new Dictionary<string, bool>();

		public BlueprintArea ActiveArea { get; set; }

		public static event Action<Object, Rect> WorkspaceWindowItemOnGUI;

		public static Object FirstTarget
			=> s_FirstTarget?.GetDraggedObject();

		public static Object SecondTarget
			=> s_SecondTarget?.GetDraggedObject();

		[CanBeNull]
		private static WorkspaceItemBase s_MouseOverItem;
		[CanBeNull]
		private static WorkspaceItemBase s_FirstTarget;
		[CanBeNull]
		private static WorkspaceItemBase s_SecondTarget;

		[MenuItem("Design/Workspace", false, 1000)]
		public static void ShowTool()
		{
			GetWindow<WorkspaceCanvasWindow>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_Preset)
			{
				m_SavedPreset = m_Preset.XmlData;
			}

			if (string.IsNullOrEmpty(m_SavedPreset))
			{
				m_Items = new List<WorkspaceItemBase>();
			}
			else
			{
				LoadItems(m_SavedPreset);
				Debug.Log("Loaded: " + m_SavedPreset);
			}
		}

		protected override void OnGUI()
		{
			titleContent = titleContent ?? new GUIContent("");
			titleContent.text = m_Preset ? m_Preset.name : "Workspace";

			base.OnGUI();

			// handle delete button (before everything else to prevent it from clearing workspace field)
			if (Event.current.isKey && Event.current.keyCode == KeyCode.Delete)
			{
				m_Items.RemoveAll(i => i.Selected);
				UpdatePreset();
				Event.current.Use();
			}

			// remove selection
			else if (Event.current.type == EventType.MouseUp && !DragManager.Instance.DragInProgress)
			{
				m_Items.ForEach(i => i.Selected = false);
			}

			DrawToolbar();

			using (GuiScopes.Horizontal())
			{
				var r = GUILayoutUtility.GetRect(0, 0, 0, 0);
				using (var scroll = new EditorGUILayout.ScrollViewScope(m_ScrollPos))
				{
					m_CanvasSize = Vector2.zero;
					m_ScrollPos = scroll.scrollPosition;

					// only handle mouse events (like drag) if mouse is actually INSIDE the scroll scope!
					var currentMousePosition = Event.current.mousePosition - m_ScrollPos;
					int infoPanelWidth = m_ShowAreaInfo ? InfoPanelWidth : 0;
					if (!Event.current.isMouse ||
						new Rect(r.position, position.size - r.position - new Vector2(infoPanelWidth, 0)).Contains(currentMousePosition))
					{
						DrawSnapGrid();
						DrawItems();
						DrawDraggedItems();
						DrawSelectionRect();
						AcceptDrop();

						m_CanvasSize += new Vector2(position.size.x - infoPanelWidth, position.size.y);
						GUILayoutUtility.GetRect(m_CanvasSize.x, m_CanvasSize.x, m_CanvasSize.y, m_CanvasSize.y);
					}
				}

				if (m_ShowAreaInfo)
				{
					DrawAreaInfo();
				}
			}

			// scroll window if dragging near scroll border
			if (m_IsDraggingSelection)
			{
				var pos = Event.current.mousePosition;
				if (pos.x < 10)
				{
					m_ScrollPos.x--;
					Repaint();
				}
				if (pos.x > position.width - 10)
				{
					m_ScrollPos.x++;
					Repaint();
				}
				if (pos.y < 30)
				{
					m_ScrollPos.y--;
					Repaint();
				}
				if (pos.y > position.height - 10)
				{
					m_ScrollPos.y++;
					Repaint();
				}
			}
		}

		private void DrawAreaInfo()
		{
			var allAreas = m_Items
				.OfType<WorkspaceItemBlueprint>()
				.Select(i => i.Blueprint as BlueprintArea)
				.NotNull()
				.ToArray();

			ActiveArea = ActiveArea && allAreas.Any(i => i == ActiveArea)
				? ActiveArea
				: allAreas.FirstOrDefault();

			int activeAreaIndex = allAreas.IndexOf(ActiveArea);

			using (var scope = new EditorGUILayout.VerticalScope())
			{
				GUI.Box(scope.rect, GUIContent.none);

				bool refresh = false;
				if (ActiveArea)
				{
					bool expandAll;
					bool collapseAll;

					using (GuiScopes.Horizontal())
					{
						refresh = GUILayout.Button("Refresh") || m_AreaInfo == null || m_AreaInfo.Area != ActiveArea;
						expandAll = GUILayout.Button("Expand All");
						collapseAll = GUILayout.Button("Collapse All");
					}

					foreach (var k in m_Foldouts.Keys.ToArray())
					{
						if (collapseAll)
						{
							m_Foldouts[k] = false;
						}
						else if (expandAll)
						{
							m_Foldouts[k] = true;
						}
					}
				}

				using (var infoScroll = new EditorGUILayout.ScrollViewScope(m_InfoScrollPos, GUILayout.Width(InfoPanelWidth)))
				{
					m_InfoScrollPos = infoScroll.scrollPosition;

					using (GuiScopes.Vertical())
					{
						if (ActiveArea)
						{
							EditorGUIUtility.labelWidth = 100;

							using (GuiScopes.Horizontal())
							{
								activeAreaIndex = EditorGUILayout.Popup("Area", activeAreaIndex, allAreas.Select(i => i.name).ToArray());
								refresh |= ActiveArea != allAreas[activeAreaIndex];
								ActiveArea = allAreas[activeAreaIndex];

								if (GUILayout.Button("Select", GUILayout.Width(50)))
								{
                                    Selection.activeObject = BlueprintEditorWrapper.Wrap(ActiveArea); 
								}
							}

							if (refresh)
							{
								m_AreaInfo = AreaInfoCollector.Collect(ActiveArea);
							}

							DrawObjectField("Static scene", m_AreaInfo.StaticScene?.Asset);
							DrawObjectField("Foliage scene", m_AreaInfo.FoliageScene?.Asset);

							DrawSceneReferenceList("Mechanic scenes", m_AreaInfo.MechanicScenes);
							DrawSceneReferenceList("Light scenes", m_AreaInfo.LightScenes);
							DrawSceneReferenceList("Audio scenes", m_AreaInfo.AudioScenes);

							DrawBlueprintList("Parts", m_AreaInfo.Parts);
							DrawBlueprintList("Enter points", m_AreaInfo.EnterPoints);
							DrawBlueprintList("Dialogs", m_AreaInfo.Dialogs);
                            DrawBlueprintList("Cutscenes", m_AreaInfo.Cutscenes);

                            foreach (var g in m_AreaInfo.Others.GroupBy(o => o.GetType()))
                            {
                                var t = g.Key.Name.Replace("Blueprint", "") + "s";
								DrawBlueprintList(t, g);
							}
						}
						else
						{
							EditorGUILayout.LabelField("Has no area in this workspace");
						}
					}
				}
			}
		}

		private static void DrawObjectField(string titleText, Object obj)
		{
			if (obj == null)
			{
				return;
			}

            SimpleBlueprint bp = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(obj);

			var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
			if (rect.Contains(Event.current.mousePosition))
			{
				if (Event.current.type == EventType.MouseDown)
				{
					Selection.activeObject = obj;

					if (Event.current.clickCount == 2)
					{
                        if (bp is BlueprintDialog || bp is BlueprintCueBase || bp is BlueprintAnswerBase)
                        {
                            DialogEditor.OpenAssetInDialogEditor(bp);
                        }
                        else if (bp is BlueprintQuest q)
                        {
                            QuestEditor.OpenAssetInQuestEditor(q);
                        }
                        else if (bp is Gate)
                        {
                            CutsceneEditorWindow.OpenAssetInCutsceneEditor(bp);
                        }
					}
				}
				else if (Event.current.type == EventType.MouseDrag)
				{
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.objectReferences = new[] { obj };
					DragAndDrop.StartDrag(titleText);
					Event.current.Use();
				}
			}

            if (bp != null)
            {
                BlueprintEditorUtility.ObjectField(rect, new GUIContent(titleText), bp, bp.GetType());
            }
            else
            {
                EditorGUI.ObjectField(rect, new GUIContent(titleText), obj, obj.GetType(),
                    obj is SceneAsset);
			}
		}

		private static void DrawBlueprintField(string titleText, BlueprintScriptableObject blueprint)
		{
			if (blueprint == null)
			{
				return;
			}

			var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
			if (rect.Contains(Event.current.mousePosition))
			{
				if (Event.current.type == EventType.MouseDown)
				{
					if (Event.current.clickCount == 2)
					{
						if (blueprint is BlueprintDialog || blueprint is BlueprintCueBase || blueprint is BlueprintAnswerBase)
						{
							DialogEditor.FocusAsset(blueprint as BlueprintDialog, blueprint);
						}
						else if (blueprint is BlueprintQuest)
						{
							QuestEditor.Focus(blueprint as BlueprintQuest, null);
						}
						else if (blueprint is Gate)
						{
							CutsceneEditorWindow.OpenAssetInCutsceneEditor(blueprint);
						}
						else
						{
							BlueprintInspectorWindow.OpenFor(blueprint);
						}
					}
				}
				else if (Event.current.type == EventType.MouseDrag)
				{
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.objectReferences = new[] { BlueprintEditorWrapper.Wrap(blueprint) };
					DragAndDrop.StartDrag(titleText);
					Event.current.Use();
				}
			}
			BlueprintEditorUtility.ObjectField(rect, new GUIContent(titleText), blueprint, blueprint.GetType());
		}

		private void DrawObjectsList(string titleText, IEnumerable<Object> objects)
		{
			if (!m_Foldouts.ContainsKey(titleText))
			{
				m_Foldouts[titleText] = false;
			}

			m_Foldouts[titleText] = EditorGUILayout.Foldout(m_Foldouts[titleText], $"{titleText} (count: {objects.Count()})");
			if (m_Foldouts[titleText])
			{
				EditorGUI.indentLevel += 1;
				int i = 0;
				foreach (var obj in objects)
				{
					DrawObjectField($"Item {i++}", obj);
				}
				EditorGUI.indentLevel -= 1;
			}
		}

		private void DrawBlueprintList<T>(string titleText, IEnumerable<T> blueprints) where T : BlueprintScriptableObject
		{
			if (!m_Foldouts.ContainsKey(titleText))
			{
				m_Foldouts[titleText] = false;
			}

			m_Foldouts[titleText] = EditorGUILayout.Foldout(m_Foldouts[titleText], $"{titleText} (count: {blueprints.Count()})");
			if (m_Foldouts[titleText])
			{
				EditorGUI.indentLevel += 1;
				int i = 0;
				foreach (var blueprint in blueprints)
				{
					DrawBlueprintField($"Item {i++}", blueprint);
				}
				EditorGUI.indentLevel -= 1;
			}
		}

		private void DrawSceneReferenceList(string titleText, IEnumerable<SceneReference> scenes)
		{
			DrawObjectsList(titleText, scenes.Select(sr => sr.Asset));
		}

		private void DrawSnapGrid()
		{
			if (m_SnapToGrid)
			{
				using (GuiScopes.Color(new Color(1, 1, 1, 0.4f)))
					GUI.DrawTextureWithTexCoords(
						new Rect(m_ScrollPos, position.size),
                        OwlcatEditorStyles.Instance.GridTexture,
						new Rect(
							// hacky way to make texture align with snapping grid
							m_ScrollPos.x / 64,
							(m_ScrollPos.y + position.height) / 64,
							position.width / 64,
							-position.height / 64));
			}
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				var presetRect = EditorGUILayout.GetControlRect(false, 16f, GUILayout.Width(200));
				var loadButtonClicked = GUI.Button(
					new Rect(presetRect.xMax - 18, presetRect.y, 18, presetRect.height),
					"", GUIStyle.none); // override "o" button that ObjectField draws to show our assetpicker

				var p = (WorkspacePreset)EditorGUI.ObjectField(presetRect, m_Preset, typeof(WorkspacePreset), false);
				if (p != m_Preset)
				{
					LoadPreset(p);
				}
				if (GUILayout.Button("Load", EditorStyles.miniLabel) || loadButtonClicked)
				{
					AssetPicker.ShowAssetPicker<WorkspacePreset>(pr => LoadPreset((WorkspacePreset)pr), new string[0], false, m_Preset);
				}

				if (m_Preset && GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
				{
					m_Preset.XmlData = SaveItems();
					EditorUtility.SetDirty(m_Preset);
					AssetDatabase.SaveAssets();
				}
				if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
				{
					var path = EditorUtility.SaveFilePanelInProject("Preset file", "Workspace", "asset", "");
					var existing = AssetDatabase.LoadAssetAtPath<WorkspacePreset>(path);
					if (!existing)
					{
						existing = CreateInstance<WorkspacePreset>();
						AssetDatabase.CreateAsset(existing, path);
					}
					m_Preset = existing;
					UpdatePreset();
					AssetDatabase.SaveAssets();
				}
				GUILayout.Space(64);
				m_SnapToGrid = GUILayout.Toggle(
					m_SnapToGrid,
					"Snap to grid",
					EditorStyles.toolbarButton,
					GUILayout.ExpandWidth(false));
				if (m_Items.Any(i => i.Selected) &&
					GUILayout.Button("Align", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
				{
					var x = m_Items.Where(i => i.Selected).Min(i => i.Position.x);
					var y = m_Items.Where(i => i.Selected).Min(i => i.Position.y);
					foreach (var item in m_Items)
					{
						if (item.Selected)
						{
							item.Position = new Vector2(x, y);
							y += item.Measure().y;
						}
					}
					UpdatePreset();
					Repaint();
				}

				GUILayout.FlexibleSpace();

				if (GUILayout.Button("New window", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
				{
					var w = CreateInstance<WorkspaceCanvasWindow>();
					w.m_SavedPreset = SaveItems();
					w.LoadItems(w.m_SavedPreset);
					w.position = new Rect(w.position.position + Vector2.one * 18, w.position.size);
					w.Show();
				}
				if (!string.IsNullOrEmpty(m_SavedPreset) && GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
				{
					LoadItems(m_SavedPreset);
					Repaint();
				}

				var areaInfoStyle = new GUIStyle(EditorStyles.toolbarButton);
				if (m_ShowAreaInfo)
					areaInfoStyle.normal = areaInfoStyle.active;
				if (GUILayout.Button("Area Info", areaInfoStyle, GUILayout.ExpandWidth(false)))
				{
					m_ShowAreaInfo = !m_ShowAreaInfo;
				}

				GUILayout.Space(64);

				var rect = GUILayoutUtility.GetRect(
					new GUIContent(OwlcatEditorStyles.Instance.DeleteIcon),
					EditorStyles.toolbarButton,
					GUILayout.Width(48));
				var isDropSite = DragManager.Instance.IsDragging<DraggedWorkspaceItems>() &&
							rect.Contains(Event.current.mousePosition);

				using (GuiScopes.Color(isDropSite ? Color.cyan : GUI.color))
				{
					if (GUI.Button(rect, OwlcatEditorStyles.Instance.DeleteIcon, EditorStyles.toolbarButton))
					{
						m_Items.RemoveAll(i => i.Selected);
						UpdatePreset();
					}

					if ((isDropSite && Event.current.type == EventType.DragPerform))
					{
						var dragged = (DraggedWorkspaceItems)DragManager.Instance.DraggedObject;
						m_Items.RemoveAll(i => dragged.Items.Contains(i));
						UpdatePreset();
					}
				}
			}
		}

		private void UpdatePreset()
		{
			if (m_Preset)
			{
				m_Preset.XmlData = SaveItems();
				EditorUtility.SetDirty(m_Preset);
			}
		}

		private void DrawDraggedItems()
		{
			if (!DragManager.Instance.IsDragging<DraggedWorkspaceItems>()) // todo: don't draw items dragged from ANOTHER window?
				return;

			var dragged = (DraggedWorkspaceItems)DragManager.Instance.DraggedObject;
			var positionDelta = Event.current.mousePosition - dragged.StartPosition;
			foreach (var item in dragged.Items)
			{
				var rect = new Rect(item.Position + positionDelta, item.Measure());
				item.OnGUI(rect);
			}
		}

		private void DrawSelectionRect()
		{
			if (Event.current.type == EventType.MouseDrag)
			{
				if (Event.current.button == 2) // mmb scroll
				{
					Event.current.Use();
					m_ScrollPos -= Event.current.delta;
					Repaint();
					return;
				}
				if (!m_IsDraggingSelection)
				{
					m_SelectionFrom = Event.current.mousePosition;
					m_IsDraggingSelection = true;
				}
				else
				{
					m_SelectionTo = Event.current.mousePosition;
					Repaint();
				}
				Event.current.Use();
			}

			if (m_IsDraggingSelection)
			{
				var rect = new Rect(m_SelectionFrom, m_SelectionTo - m_SelectionFrom);
				GUI.Box(rect, "", OwlcatEditorStyles.Instance.ThinFrame);

				if (Event.current.type == EventType.MouseUp)
				{
					foreach (var item in m_Items)
					{
						item.Selected = item.Rect.Overlaps(rect, true);
					}
					m_IsDraggingSelection = false;
					Repaint();
				}
			}
		}

		public static void UpdateTarget(bool first)
		{
			MarkTarget(
				first ? 1 : 2,
				ref first ? ref s_FirstTarget : ref s_SecondTarget,
				ref first ? ref s_SecondTarget : ref s_FirstTarget
			);
		}

		private static void MarkTarget(
			int targetNumber,
			ref WorkspaceItemBase relevantField,
			ref WorkspaceItemBase irrelevantField
		)
		{
			if (relevantField == null && s_MouseOverItem == null)
				return;

			relevantField?.MarkItemAsTarget(null);
			relevantField = null;

			if (s_MouseOverItem != null)
			{
				if (irrelevantField == s_MouseOverItem)
					irrelevantField = null;

				relevantField = s_MouseOverItem;
				relevantField.MarkItemAsTarget(targetNumber);
			}
		}

		private void DrawItems()
		{
			var dragged = DragManager.Instance.DraggedObject as DraggedWorkspaceItems;

			// when repainting, go in item order. When handling other events, go in REVERSE order
			// so that topmost item handles input first
			var from = Event.current.type == EventType.Repaint ? 0 : m_Items.Count - 1;
			var step = Event.current.type == EventType.Repaint ? 1 : -1;
			
			var hadMouseOverItem = false;
			for (int ii = from; ii >= 0 && ii < m_Items.Count; ii += step)
			{
				var item = m_Items[ii];
				var rect = item.Rect;
				if (rect.Contains(Event.current.mousePosition))
				{
					hadMouseOverItem = true;
					s_MouseOverItem = item;
				}

				var grayOut = DragManager.Instance.DragInProgress && dragged != null && dragged.Items.Contains(item);
				using (GuiScopes.Color(grayOut ? new Color(1, 1, 1, 0.4f) : GUI.color))
				{
					item.OnGUI(rect);
					if (item.Selected)
						GUI.Box(rect, "", OwlcatEditorStyles.Instance.SelectionRect);

					m_CanvasSize = Vector2.Max(m_CanvasSize, rect.max);
				}

				WorkspaceWindowItemOnGUI?.Invoke(item.GetDraggedObject(), rect);
				if (rect.Contains(Event.current.mousePosition) && Event.current.isMouse && !m_IsDraggingSelection &&
				    !DragManager.Instance.DragInProgress)
				{
					if (Event.current.clickCount > 1 && Event.current.button == 0)
					{
						item.DoubleClick();
						Event.current.Use();
						Repaint();
					}
					else if (Event.current.type == EventType.MouseUp)
					{
						if (Event.current.button == 0)
						{
							if (Event.current.control)
							{
								item.Selected = !item.Selected;
							}
							else
							{
								foreach (var otherItem in m_Items)
								{
									otherItem.Selected = otherItem == item;
								}
								item.Click();
							}
						}
						else
						{
							item.ShowContextMenu();
						}
						Event.current.Use();
						Repaint();
					}
					else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
					{
						var draggedItems = new DraggedWorkspaceItems
						{
							Items =
								item.Selected
									? m_Items.Where(i => i.Selected).ToList()
									: new List<WorkspaceItemBase> { item },
							StartPosition = Event.current.mousePosition
						};
						if (!item.Selected)
						{
							foreach (var otherItem in m_Items)
							{
								otherItem.Selected = otherItem == item;
							}
						}
						var icon = draggedItems.Items.First().GetDragIcon();
						DragManager.Instance.BeginDrag(
							w => { if (icon) GUI.DrawTexture(new Rect(0, 0, 32, 32), icon); },
							draggedItems,
							Vector2.one * 32);
						DragManager.Instance.UnityObjects =
							draggedItems.Items.Select(i => i.GetDraggedObject()).Where(o => o).ToArray();
						m_SelectionFrom = Event.current.mousePosition;
						Event.current.Use();
					}
				}
			}
			
			if (!hadMouseOverItem)
				s_MouseOverItem = null;
		}

		private void AcceptDrop()
		{
			if (Event.current.type == EventType.DragUpdated)
			{
				if (DragAndDrop.objectReferences.Any(EditorUtility.IsPersistent)
					|| DragAndDrop.objectReferences.OfType<GameObject>().Any()
                    || DragAndDrop.objectReferences.OfType<BlueprintEditorWrapper>().Any())
					DragAndDrop.visualMode = DragAndDropVisualMode.Link;
			}
			if (Event.current.type == EventType.DragPerform)
			{
				var dragged = DragManager.Instance.DraggedObject as DraggedWorkspaceItems;
				if (dragged != null)
				{
					// todo: remove items from their window if it's not THIS window
					var positionDelta = Event.current.mousePosition - dragged.StartPosition;
					foreach (var item in dragged.Items)
					{
						item.Position += positionDelta;
					}
					foreach (var item in dragged.Items)
					{
						SnapItem(item);
					}
				}
				else
				{
					AcceptDraggedItems(Event.current.mousePosition, DragAndDrop.objectReferences);
					DragAndDrop.AcceptDrag();
				}

				UpdatePreset();
			}
		}

		public void AcceptDraggedItems(Vector2 pos, IEnumerable<Object> objectReferences, bool includeScroll = false)
		{
            if (includeScroll)
            {
                pos += m_ScrollPos;
            }

            var list = objectReferences.ToList();
            // add blueprints
            foreach (var bp in objectReferences.OfType<BlueprintEditorWrapper>())
            {
                AddBlueprintItem(bp.Blueprint, ref pos);
                list.Remove(bp);
            }

            // add prefabs and in-scene refs
            foreach (var prefab in objectReferences.OfType<GameObject>())
            {
                var type = PrefabUtility.GetPrefabAssetType(prefab);
                //var state = PrefabUtility.GetPrefabInstanceStatus(prefab);
                if (type == PrefabAssetType.Regular || type == PrefabAssetType.Variant)
                {
                    var item = m_Items.OfType<WorkspaceItemPrefab>().SingleOrDefault(i => i.GameObject == prefab);
                    if (item == null)
                        m_Items.Add(item = new WorkspaceItemPrefab { GameObject = prefab });
                    item.Position = pos;
                    pos += Vector2.up * 32;

                    list.Remove(prefab);
                }

                if (type == PrefabAssetType.NotAPrefab)
                {
                    var evb = prefab.GetComponent<EntityViewBase>();
                    if (evb && prefab.scene.IsValid())
                    {
                        var item = m_Items.OfType<WorkspaceItemReference>().SingleOrDefault(i => i.UniqueId == evb.UniqueId);
                        if (item == null)
                            m_Items.Add(item = new WorkspaceItemReference(evb));
                        item.Position = pos;
                        pos += Vector2.up * 32;
                    }

                    list.Remove(prefab);
                }
            }

            // add scenes
            foreach (var scene in objectReferences.OfType<SceneAsset>())
            {
                var item = m_Items.OfType<WorkspaceItemScene>().SingleOrDefault(i => i.SceneAsset == scene);
                if (item == null)
                    m_Items.Add(item = new WorkspaceItemScene() { SceneAsset = scene });
                item.Position = pos;
                pos += Vector2.up * 32;
                list.Remove(scene);
            }

            // add everything that is an asset and we didn't add yet as default asssets
            foreach (var asset in list.Where(EditorUtility.IsPersistent))
            {
                var item = m_Items.OfType<WorkspaceItemOtherAsset>().SingleOrDefault(i => i.Asset == asset);
                if (item == null)
                    m_Items.Add(item = new WorkspaceItemOtherAsset() { Asset = asset });
                item.Position = pos;
                pos += Vector2.up * 32;
            } 

        }

		private void SnapItem(WorkspaceItemBase item)
		{
			var rect = item.Rect;
			bool xSnap = false, ySnap = false;
			foreach (var otherItem in m_Items)
			{
				if (otherItem == item)
					continue;

				var otherRect = otherItem.Rect;

				if (!xSnap && Mathf.Abs(rect.x - otherRect.x) < 15 && rect.yMax > otherRect.yMin - 32 && rect.yMin < otherRect.yMax + 32)
				{
					rect.x = otherRect.x;
					xSnap = true;
				}
				if (!xSnap && Mathf.Abs(rect.x - otherRect.xMax) < 15 && rect.yMax > otherRect.yMin - 32 && rect.yMin < otherRect.yMax + 32)
				{
					rect.x = otherRect.xMax;
					xSnap = true;
				}
				if (!xSnap && Mathf.Abs(rect.xMax - otherRect.x) < 15 && rect.yMax > otherRect.yMin - 32 && rect.yMin < otherRect.yMax + 32)
				{
					rect.x = otherRect.x - rect.width;
					xSnap = true;
				}
				if (!xSnap && Mathf.Abs(rect.xMax - otherRect.xMax) < 15 && rect.yMax > otherRect.yMin - 32 && rect.yMin < otherRect.yMax + 32)
				{
					rect.x = otherRect.xMax - rect.width;
					xSnap = true;
				}

				if (!ySnap && Mathf.Abs(rect.y - otherRect.y) < 15 && rect.xMax > otherRect.xMin - 32 && rect.xMin < otherRect.xMax + 32)
				{
					rect.y = otherRect.y;
					ySnap = true;
				}
				if (!ySnap && Mathf.Abs(rect.y - otherRect.yMax) < 15 && rect.xMax > otherRect.xMin - 32 && rect.xMin < otherRect.xMax + 32)
				{
					rect.y = otherRect.yMax;
					ySnap = true;
				}
				if (!ySnap && Mathf.Abs(rect.yMax - otherRect.y) < 15 && rect.xMax > otherRect.xMin - 32 && rect.xMin < otherRect.xMax + 32)
				{
					rect.y = otherRect.y - rect.height;
					ySnap = true;
				}
				if (!ySnap && Mathf.Abs(rect.yMax - otherRect.yMax) < 15 && rect.xMax > otherRect.xMin - 32 && rect.xMin < otherRect.xMax + 32)
				{
					rect.y = otherRect.yMax - rect.height;
					ySnap = true;
				}
				if (xSnap && ySnap)
					break;
			}

			if (m_SnapToGrid)
			{
				var gridX = Mathf.Round(rect.x / 64) * 64;
				var gridY = Mathf.Round(rect.y / 64) * 64;

				if (!xSnap && Mathf.Abs(rect.x - gridX) < 15)
					rect.x = gridX;

				if (!ySnap && Mathf.Abs(rect.y - gridY) < 15)
					rect.y = gridY;
			}
			item.Position = rect.position;
		}

		private void AddBlueprintItem(SimpleBlueprint bp, ref Vector2 pos)
		{
			var item = m_Items.OfType<WorkspaceItemBlueprint>().SingleOrDefault(i => i.Blueprint == bp);
			if (item == null)
				m_Items.Add(item = new WorkspaceItemBlueprint { Blueprint = bp });
			item.Position = pos;
			pos += Vector2.up * 32;
		}

		string SaveItems()
		{
			var xs = new XmlSerializer(typeof(List<WorkspaceItemBase>));
			using (var ss = new StringWriter())
			{
				xs.Serialize(ss, m_Items);
				return ss.ToString();
			}
		}
		
		[MenuItem("BP/Convert WorkspaceItemOtherAssets")]
		public static void Convert()
		{
			string[] guids = AssetDatabase.FindAssets("t:WorkspacePreset", new string[] { "Assets" });

			AssetDatabase.StartAssetEditing();
			foreach (string guid in guids)
			{
				try
				{
					bool changed = false;

					string path = AssetDatabase.GUIDToAssetPath(guid);
					PFLog.Default.Log($"Reading {guid} at {path}.");

					// Load
					WorkspacePreset workspacePreset = AssetDatabase.LoadAssetAtPath<WorkspacePreset>(path);

					// Read
					List<WorkspaceItemBase> items;
					var xmlSerializer = new XmlSerializer(typeof(List<WorkspaceItemBase>));
					using (var stringReader = new StringReader(workspacePreset.XmlData))
					{
						try
						{
							items = (List<WorkspaceItemBase>)xmlSerializer.Deserialize(stringReader);
						}
						catch (Exception exception)
						{
							PFLog.Default.Exception(exception, $"Failed to deserialize {nameof(WorkspacePreset)} {guid} at {path}.");
							continue;
						}
					}
					// Remove nulls and dead links
					for (int i = 0; i < items.Count; i++)
					{
						if (items[i] == null)
						{
							PFLog.Default.Error($"Null item encountered. Removed it.");
							items.RemoveAt(i--);
							changed = true;
							continue;
						}

                        if (items[i] is WorkspaceItemBlueprint)
                        {
                            WorkspaceItemBlueprint workspaceItemBlueprint = (WorkspaceItemBlueprint)items[i];
                            if (workspaceItemBlueprint.Blueprint == null)
                            {
                                PFLog.Default.Log($"Removed dead link to blueprint {workspaceItemBlueprint.BlueprintGuid}.");
                                items.RemoveAt(i--);
                                changed = true;
                                continue;
                            }
                        }
                        if (items[i] is WorkspaceItemOtherAsset)
                        {
                            WorkspaceItemOtherAsset workspaceItemOtherAsset = (WorkspaceItemOtherAsset)items[i];
                            if (workspaceItemOtherAsset.Asset == null)
                            {
                                PFLog.Default.Log($"Removed dead link to asset {workspaceItemOtherAsset.AssetGuid}.");
                                items.RemoveAt(i--);
                                changed = true;
                                continue;
                            }
                        }
                    }
					// Convert
					int count = Convert(items);
					if (count > 0)
					{
						PFLog.Default.Log($"Converted {count} items.");
						changed = true;
					}

					if (changed)
					{
						// Write
						using (var stringWriter = new StringWriter())
						{
							PFLog.Default.Log($"Writing {guid} at {path}.");
							xmlSerializer.Serialize(stringWriter, items);
							workspacePreset.XmlData = stringWriter.ToString();
						}
						// Mark
						EditorUtility.SetDirty(workspacePreset);
					}
				}
				catch (Exception exception)
				{
					PFLog.Default.Exception(exception, $"Failed to convert.");
				}
			}
			AssetDatabase.StopAssetEditing();
			AssetDatabase.SaveAssets();
		}
		/// <summary>
		/// Convert some of the <see cref="WorkspaceItemOtherAsset"/>s to new blueprints.
		/// </summary>
		/// <param name="items"></param>
		/// <returns>True if anything was changed, false otherwise.</returns>
		private static int Convert(List<WorkspaceItemBase> items)
		{
            int converted = 0;
            for (int i = 0; i < items.Count; i++)
			{
				// Interested only in WorkspaceItemOtherAssets ...
				WorkspaceItemOtherAsset workspaceItemOtherAsset = items[i] as WorkspaceItemOtherAsset;
				if (workspaceItemOtherAsset == null)
				{
					continue;
				}
				// ... that contain a blueprint
				BlueprintEditorWrapper blueprintEditorWrapper = workspaceItemOtherAsset.Asset as BlueprintEditorWrapper;
				if (blueprintEditorWrapper == null || blueprintEditorWrapper.Blueprint == null)
				{
					continue;
				}

				try
				{
					string guid = blueprintEditorWrapper.Blueprint.AssetGuid;

					Type type = BlueprintsDatabase.GetTypeById(guid);
					if (typeof(SimpleBlueprint).IsAssignableFrom(type))
					{
						WorkspaceItemBlueprint itemBlueprint = new WorkspaceItemBlueprint();
						itemBlueprint.BlueprintGuid = guid;
						itemBlueprint.Position = workspaceItemOtherAsset.Position;

						items[i] = itemBlueprint;

						PFLog.Default.Log($"Converted {nameof(WorkspaceItemOtherAsset)} with GUID {guid} and name {blueprintEditorWrapper.name} to {nameof(WorkspaceItemBlueprint)}.");
					}
					else
					{
						PFLog.Default.Warning($"Skipped {nameof(WorkspaceItemOtherAsset)} with GUID {guid} and name {blueprintEditorWrapper.name} of type {type}.");
					}
				}
				catch (Exception exception)
				{
					PFLog.Default.Exception(exception, $"Could not convert {nameof(WorkspaceItemOtherAsset)} to {nameof(WorkspaceItemBlueprint)}, because its blueprint is null.");
				}

                converted++;
            }
			return converted;
		}
		
		void LoadItems(string str)
		{
			var xs = new XmlSerializer(typeof(List<WorkspaceItemBase>));
			using (var ss = new StringReader(str))
			{
				m_Items = (List<WorkspaceItemBase>)xs.Deserialize(ss);
			}
			m_SavedPreset = str;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			s_FirstTarget?.MarkItemAsTarget(null);
			s_FirstTarget = null;
			s_SecondTarget?.MarkItemAsTarget(null);
			s_SecondTarget = null;
			m_SavedPreset = SaveItems();
			UpdatePreset();
		}

		public void LoadPreset(WorkspacePreset preset)
		{
			m_Preset = preset;
			if (preset)
			{
				ActiveArea = null;
				m_Foldouts.Clear();
				LoadItems(preset.XmlData);
			}
		}
	}

	class DraggedWorkspaceItems
	{
		public List<WorkspaceItemBase> Items;
		public Vector2 StartPosition;
	}
}