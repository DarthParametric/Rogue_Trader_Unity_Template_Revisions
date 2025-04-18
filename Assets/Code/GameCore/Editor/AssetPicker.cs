using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Kingmaker.AreaLogic.TimeOfDay;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.Utility;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Visual.CharacterSystem;
using Owlcat.Editor.Core.Utility;
using Owlcat.Editor.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor
{
	public class AssetPicker : KingmakerWindowBase
	{
		public class HierarchyEntry : IComparable<HierarchyEntry>
		{
			public HierarchyEntry Parent;
			public string Path;
			public string Name;
			public string AssetGuid;
			public int InstanceId;
			public Object Asset;
			public List<HierarchyEntry> Children;
			public bool Expanded = true;
			public bool Hidden;

			public void FindAsset()
			{
				if (!Asset)
				{
					var hp = new HierarchyProperty(HierarchyType.Assets);
					hp.SetSearchFilter("", (int)SearchableEditorWindow.SearchMode.All);
					if (!hp.Find(InstanceId, null))
					{
						PFLog.Default.Log("Can't find instance id " + InstanceId);
						return;
					}

					Asset = hp.pptrValue;
				}
			}

			public int CompareTo(HierarchyEntry other)
			{
				if (Children == null && other.Children != null)
				{
					return 1;
				}
				if (Children != null && other.Children == null)
				{
					return -1;
				}
				return string.Compare(Path, other.Path, StringComparison.Ordinal);
			}
		}

		public static GUIStyle SelectedAssetStyle;

		public static GUIStyle AssetStyle;

		private static GUIStyle FolderStyle;

		public const int AssetShowCountStep = 50;

		private int m_AssetShowCount = AssetShowCountStep;

		private int m_AssetsShown = 0;

		private bool m_ShowTimeOfDaySelection;
		private static TimeOfDay s_SelectedTimeOfDay = TimeOfDay.Day;

		private bool m_EnableSelectionOnClick = false;
		private string[] m_LabelsFilter = null;
		private readonly HashSet<string> m_ValidLabelsGuids = new HashSet<string>();
		private readonly StringBuilder m_StringBuilder = new StringBuilder();

		private HierarchyEntry SelectedAssetEntry
		{
			get { return m_SelectedAssetEntry; }
			set
			{
				var e = value;
				while (e != null)
				{
					e.Expanded = true;
					e = e.Parent;
				}
				m_SelectedAssetEntry = value;
			}
		}

		[MenuItem("Design/Asset picker #%A", false, 5000)]
		public static void ShowAssetPicker()
		{
			GetWindow<AssetPicker>();
		}

		public static void ShowAssetPicker(
			[NotNull] Type assetType,
			[CanBeNull] FieldInfo fieldInfo,
			[NotNull] Action<Object> callback,
			[CanBeNull] Object selectedAsset = null,
			[CanBeNull] Func<HierarchyEntry, bool> filter = null,
			bool enableSelectionOnClick = false)
		{
			var w = CreateInstance<AssetPicker>();

			w.m_Filters.Clear();
			if (filter != null)
			{
				w.m_Filters.Add(filter);
			}
			if (fieldInfo != null)
			{
				if (fieldInfo.GetAttribute<DependenciesFilterAttribute>() != null)
				{
					w.m_Filters.AddRange(DependenciesFilters);
				}

				var pathFilter = fieldInfo.GetAttribute<AssetPathFilterAttribute>();
				if (pathFilter != null)
				{
					w.m_Filters.Add(he => pathFilter.Filters.Any(f => he.Path.Contains(f)));
				}
			}

			w.AssetType = assetType;
			w.m_EnableSelectionOnClick = enableSelectionOnClick;
			w.m_SelectionCallback = callback;
			w.m_LabelsFilter = null;

			w.UpdateAssetList();
			if (selectedAsset)
			{
				var assetPath = AssetDatabase.GetAssetPath(selectedAsset);
				w.SelectedAssetEntry =
					w.m_LoadedAssetsFlat.FirstOrDefault(e => e.Path == assetPath);
				w.m_MustScrollToSelected = true;
			}
			w.ShowAuxWindow();
			w.m_FocusNameFilter = true;
			w.Focus();
		}

		public static void ShowAssetPicker<T>(Action<Object> callback, string[] labels = null, bool enableSelectionOnClick = false, Object selectedAsset = null)
		{
			ShowAssetPicker<T>(callback, labels, null, enableSelectionOnClick, selectedAsset);
		}
		public static void ShowAssetPicker<T>(Action<Object> callback, string[] labels, string[] paths, bool enableSelectionOnClick = false, Object selectedAsset = null)
		{
			ShowAssetPicker<T>(callback, labels, paths, null, enableSelectionOnClick, selectedAsset);
		}

		public static void ShowAssetPicker<T>(Action<Object> callback, string[] labels, string[] paths, Func<HierarchyEntry, bool> filter, bool enableSelectionOnClick = false, Object selectedAsset = null)
        {
			var w = CreateInstance<AssetPicker>();
			w.m_EnableSelectionOnClick = enableSelectionOnClick;
			w.AssetType = typeof(T);
			w.m_SelectionCallback = callback;
			w.m_LabelsFilter = labels;
			if (paths != null)
			{
				w.m_Filters.Add(e => paths.Any(p => e.Path.StartsWith(p)));
			}
            if (filter != null)
            {
				w.m_Filters.Add(filter);
            }
			w.UpdateAssetList();
			if (selectedAsset)
			{
				var assetPath = AssetDatabase.GetAssetPath(selectedAsset);
				w.SelectedAssetEntry =
					w.m_LoadedAssetsFlat.FirstOrDefault(e => e.Path == assetPath);
				w.m_MustScrollToSelected = true;
			}
			w.ShowAuxWindow();
			w.m_FocusNameFilter = true;
			w.Focus();
		}

        public static Object GetRandomObject<T>(Action<Object> callback, string[] labels)
        {
            var w = CreateInstance<AssetPicker>();
            w.m_EnableSelectionOnClick = false;
            w.AssetType = typeof(T);
            w.m_SelectionCallback = callback;

            w.m_LabelsFilter = labels;
            w.UpdateAssetList();
            List<HierarchyEntry> filterdProperly = w.m_LoadedAssetsFlat;
            for (int i = 0; i < labels.Length; i++)
            {
                filterdProperly = filterdProperly.Where(m => AssetDatabase.GetLabels(AssetDatabase.LoadAssetAtPath<EquipmentEntity>(m.Path)).Contains(labels[i])).ToList();
            }
            var randomHierarchyEntry = w.GetRandomEntry(filterdProperly);
            if (randomHierarchyEntry != null)
            {
                randomHierarchyEntry.FindAsset();
                return randomHierarchyEntry.Asset;
            }
            else
            {
                return null;
            }
            
        }


        public HierarchyEntry GetRandomEntry(List<HierarchyEntry> entries)
        {
            if (entries == null)
            {
                return null;
            }
            else if (entries.Count == 0)
            {
                return null;
            }
            return entries[UnityEngine.Random.Range(0, entries.Count)];
        }

        public static void ShowAreaPicker(Action<Object, TimeOfDay> callback)
		{
			var w = CreateInstance<AssetPicker>();
			w.AssetType = typeof(BlueprintArea);
			w.m_SelectionCallback = o => callback.Invoke(o, s_SelectedTimeOfDay);
			w.UpdateAssetList();
			w.ShowAuxWindow();
			w.m_FocusNameFilter = true;
			w.m_ShowTimeOfDaySelection = true;
			w.Focus();
		}

		public static void ShowAreaPartPicker(Action<Object, TimeOfDay> callback)
		{
			var w = CreateInstance<AssetPicker>();
			w.AssetType = typeof(BlueprintAreaPart);
			w.m_SelectionCallback = o => callback.Invoke(o, s_SelectedTimeOfDay);
			w.UpdateAssetList();
			w.ShowAuxWindow();
			w.m_FocusNameFilter = true;
			w.m_ShowTimeOfDaySelection = true;
			w.Focus();
		}

		/// <summary>
		/// Shows built-in ObjectField with custom value source and pick target
		/// </summary>
		public static void ShowPropertyField(
			Rect position, RobustSerializedProperty property, FieldInfo fieldInfo,
			Object currentValue, Action<Object> pickCallback,
			GUIContent label, Type assetType,
			Func<HierarchyEntry, bool> filter = null)
		{
			assetType = Blueprints.BlueprintLinkDrawer.GetElementType(assetType) ?? assetType;
			using (var scope = new EditorGUI.PropertyScope(position, label, property))
			{
				EditorGUI.BeginChangeCheck();
				var buttonPos = position;
				buttonPos.xMin = buttonPos.xMax - EditorGUIUtility.singleLineHeight;
				var requesterWindow = focusedWindow;

				string controlName = property.Property.serializedObject.targetObject.GetInstanceID() + "_" + property.Property.propertyPath;
				var e = Event.current;
				bool showHotKey =
					GUI.GetNameOfFocusedControl() == controlName &&
					e.type == EventType.KeyDown &&
					(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

				bool deleteHotKey =
					GUI.GetNameOfFocusedControl() == controlName &&
					e.type == EventType.KeyDown &&
					e.keyCode == KeyCode.Delete;

				if (showHotKey || deleteHotKey)
				{
					e.Use();
				}

				if (deleteHotKey)
				{
					pickCallback(null);
				}
				else if (showHotKey || GUI.Button(buttonPos, "", GUIStyle.none))
				{
					//var p = copy.FindProperty(property.propertyPath);
					// invisible button overrides object picker
					ShowAssetPicker(
						assetType,
						fieldInfo,
						o =>
						{
							using (GuiScopes.UpdateObject(property.Property.serializedObject))
							{
								pickCallback(o);
							}

							if (requesterWindow != null)
							{
								requesterWindow.Repaint();
								requesterWindow.Focus();
							}
						},
						currentValue,
						filter);
				}

				GUI.SetNextControlName(controlName);
				Object obj;
				if (property.Property.hasMultipleDifferentValues)
				{
					EditorGUI.PropertyField(position, property, scope.content, false);
					obj = property.Property.objectReferenceValue;
				}
				else
				{
					obj = EditorGUI.ObjectField(
						position,
						scope.content,
						currentValue,
						assetType,
						false);
				}
				if (GUI.GetNameOfFocusedControl() == controlName)
				{
					CopyPasteController.Process(assetType, property);
				}

				if (EditorGUI.EndChangeCheck())
				{
					pickCallback(obj);
				}
			}
		}

		/// <summary>
		/// Shows built-in ObjectField, but overrides thumb button to call our AssetPicker window instead
		/// </summary>
		public static void ShowPropertyField(Rect position, RobustSerializedProperty property, FieldInfo fieldInfo, GUIContent label, Type assetType, Func<HierarchyEntry, bool> filter = null)
		{
			Action<Object> pickCallback =
				o =>
				{
					using (GuiScopes.UpdateObject(property))
					{
						property.Property.objectReferenceValue = o;
					}
				};
			ShowPropertyField(
				position, property, fieldInfo,
				property.Property.objectReferenceValue, pickCallback,
				label, assetType, filter
			);
		}

		public static void ShowPropertyField(RobustSerializedProperty property, FieldInfo fieldInfo, GUIContent label, Type assetType, Func<HierarchyEntry, bool> filter = null)
		{
			var r = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label);
			ShowPropertyField(r, property, fieldInfo, label, assetType, filter);
		}

		public static void ShowObjectField(
			Object currentValue, Action<Object> pickCallback,
			GUIContent label, Type assetType, Type fieldType = null,
			Func<HierarchyEntry, bool> filter = null, bool enableSelectionOnClick = false,
			string setControlName = null)
		{
			var rect = EditorGUILayout.GetControlRect(true);
			ShowObjectField(rect, currentValue, pickCallback, label, assetType, fieldType, filter, enableSelectionOnClick, setControlName);
		}

		public static void ShowObjectField(
			Rect position, Object currentValue, Action<Object> pickCallback,
			GUIContent label, Type assetType, Type fieldType = null,
			Func<HierarchyEntry, bool> filter = null, bool enableSelectionOnClick = false, 
            string setControlName=null)
		{
			EditorGUI.BeginChangeCheck();
			var buttonPos = position;
			buttonPos.xMin = buttonPos.xMax - EditorGUIUtility.singleLineHeight;
			var requesterWindow = focusedWindow;

		    string controlName = setControlName ?? ("ObjField" + label.text);
			var e = Event.current;
			bool showHotKey =
				GUI.GetNameOfFocusedControl() == controlName &&
				e.type == EventType.KeyDown &&
				(e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

			bool deleteHotKey =
				GUI.GetNameOfFocusedControl() == controlName &&
				e.type == EventType.KeyDown &&
				e.keyCode == KeyCode.Delete;

			if (showHotKey || deleteHotKey)
			{
				e.Use();
			}

			if (deleteHotKey)
			{
				pickCallback(null);
			}
			else if (showHotKey || GUI.Button(buttonPos, "", GUIStyle.none))
			{
				// invisible button overrides object picker
				ShowAssetPicker(
					assetType,
					null,
					o =>
					{
						pickCallback(o);

						if (requesterWindow != null)
						{
							requesterWindow.Repaint();
							if (!s_PreviewSelection)
							{
								requesterWindow.Focus();
							}
						}
					},
					currentValue,
					filter, 
					enableSelectionOnClick
				);
				GUI.changed = false; // prevent extra callback on selecting button
			}

			if (fieldType == null)
			{
				fieldType = assetType;
			}

			GUI.SetNextControlName(controlName);
			var obj = EditorGUI.ObjectField(
				position,
				label,
				currentValue,
				fieldType,
				false);

			if (EditorGUI.EndChangeCheck())
			{
				pickCallback(obj);
			}
		}


		[SerializeField]
		private Type m_AssetType;

		[SerializeField]
		private string m_TypeFilter;

		private Type AssetType
		{
			get { return m_AssetType; }
			set
			{
				m_AssetType = value;
				if (typeof(MonoBehaviour).IsAssignableFrom(m_AssetType)
					|| typeof(GameObject).IsAssignableFrom(m_AssetType))
				{
					m_TypeFilter = "Prefab";
				}
				else if (typeof(Object).IsAssignableFrom(m_AssetType))
				{
					m_TypeFilter = m_AssetType.Name;
				}
				else
				{
					m_TypeFilter = "";
				}
			}
		}

		[SerializeField]
		private bool m_IsFlatMode;

		[SerializeField]
		private string m_NameFilter;

		private HierarchyProperty m_HierarchyProperty;

		private List<HierarchyEntry> m_LoadedAssets;
        public List<HierarchyEntry> LoadedAssets => m_LoadedAssets;
        private readonly List<HierarchyEntry> m_LoadedAssetsFlat = new List<HierarchyEntry>();
		private Vector2 m_Scroll;

		private static readonly string[] PossibleTypeFilters;
		private static string[] s_ContextMenusFirstLevel;

		private readonly HashSet<string> m_FoldedPaths = new HashSet<string>();

		private static bool s_PreviewSelection;

		private Action<Object> m_SelectionCallback;

		private List<Func<HierarchyEntry, bool>> m_Filters = new List<Func<HierarchyEntry, bool>>();

		public static List<Func<HierarchyEntry, bool>> DependenciesFilters = new List<Func<HierarchyEntry, bool>>();

		private bool m_FocusNameFilter;

		private HierarchyEntry m_SelectedAssetEntry;

		private bool m_MustScrollToSelected;

		static AssetPicker()
		{
			var blueprintTypes = TypeCache.GetTypesDerivedFrom(typeof(BlueprintScriptableObject))
				.Where(t => !t.IsAbstract);
			PossibleTypeFilters =
				blueprintTypes.Select(t => t.Name)
					.OrderBy(n => n)
					.Concat(new[] { "Prefab", "GameObject", "Material", "Texture", "Scene" })
					.ToArray();
		}

		private static void InitGUIStyles()
		{
			SelectedAssetStyle = new GUIStyle(EditorStyles.label);
			SelectedAssetStyle.normal = SelectedAssetStyle.focused;
			SelectedAssetStyle.active = SelectedAssetStyle.focused;
			SelectedAssetStyle.onNormal = SelectedAssetStyle.focused;
			SelectedAssetStyle.onActive = SelectedAssetStyle.focused;

			AssetStyle = new GUIStyle(EditorStyles.label);
			AssetStyle.active = AssetStyle.normal;
			AssetStyle.focused = AssetStyle.normal;
			AssetStyle.onActive = AssetStyle.normal;
			AssetStyle.onFocused = AssetStyle.normal;

			FolderStyle = new GUIStyle(EditorStyles.foldout);
			FolderStyle.focused = FolderStyle.normal;
			FolderStyle.active = FolderStyle.normal;
			FolderStyle.hover = FolderStyle.normal;
			FolderStyle.onFocused = FolderStyle.normal;
			FolderStyle.onActive = FolderStyle.normal;
			FolderStyle.onHover = FolderStyle.normal;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			titleContent = new GUIContent("Assets");

			FillContextMenuData();

			if (!string.IsNullOrEmpty(m_TypeFilter))
			{
				UpdateAssetList();
			}

			var folded = EditorPrefs.GetString("KM_AP_Folded_Paths", "");
			m_FoldedPaths.UnionWith(folded.Split('\n'));

			if (SelectedAssetStyle == null)
			{
				InitGUIStyles();
			}
		}

		private static void FillContextMenuData()
		{
			if (s_ContextMenusFirstLevel == null)
			{
				// find all types that have context menus attached
				// todo: this does not actually work: we need also menus for PARENT classes,
				// and also methods defined with [ContextMenu] attribute do not appear here
				var allMenus = EditorGUIUtility.SerializeMainMenuToString()
					.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
				s_ContextMenusFirstLevel = allMenus
					.SkipWhile(m => m != "CONTEXT") // find CONTEXT menu
					.Skip(1) // skip it
					.TakeWhile(m => m.StartsWith("    ")) // take until next top-level menu
					.Where(m => !m.StartsWith("     ")) // 5 spaces means at least third-level, we don't want those
					.Select(m => m.TrimStart(' ')) // cut spaces from start
					.ToArray();
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			EditorPrefs.SetString("KM_AP_Folded_Paths", string.Join("\n", m_FoldedPaths.ToArray()));
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			HandleKeyDown();

			// header (filters)
			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(false)))
			{
				GUILayout.Space(1);
				using (new EditorGUILayout.HorizontalScope())
				{
					// disable changing type if this is a pop-up picker
					using (new EditorGUI.DisabledGroupScope(m_SelectionCallback != null))
					{
						GUILayout.Label("Asset type:", GUILayout.ExpandWidth(false));

						var enter = Event.current.type != EventType.Used && Event.current.isKey &&
									(Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
						m_TypeFilter = GUILayout.TextField(m_TypeFilter ?? "", GUILayout.MaxWidth(200));
						if (enter && Event.current.type == EventType.Used)
						{
							UpdateAssetList();
						}

						if (GUILayout.Button("", OwlcatEditorStyles.Instance.OpenButton, GUILayout.ExpandWidth(false)))
						{
							var m = new GenericMenu();
							foreach (var filter in PossibleTypeFilters)
							{
								m.AddItem(
									new GUIContent(filter),
									filter == m_TypeFilter,
									f =>
									{
										GUIUtility.keyboardControl = GUIUtility.hotControl = -1;
										m_TypeFilter = (string)f;
										UpdateAssetList();
									},
									filter);
							}
							m.ShowAsContext();
						}
					}
					GUILayout.FlexibleSpace();
					if (GUILayout.Button(
						m_IsFlatMode ? "Flat mode" : "Folders mode",
                        OwlcatEditorStyles.Instance.ThinFrame,
						GUILayout.Width(90),
						GUILayout.Height(18)))
					{
						m_IsFlatMode = !m_IsFlatMode;
					}
				}
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label("Name filter:", GUILayout.ExpandWidth(false));

					EditorGUI.BeginChangeCheck();
					GUI.SetNextControlName("NameFilter");
					m_NameFilter = EditorGUILayout.TextField(m_NameFilter ?? "", GUILayout.MaxWidth(200));
					if (m_FocusNameFilter)
					{
						EditorGUI.FocusTextInControl("NameFilter");
						m_FocusNameFilter = false;
					}
					if (EditorGUI.EndChangeCheck())
					{
						UpdateFilter();
					}
				}

				if (m_ShowTimeOfDaySelection)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						GUILayout.Label("Time of day:", GUILayout.ExpandWidth(false));
						s_SelectedTimeOfDay = (TimeOfDay)EditorGUILayout.EnumPopup(s_SelectedTimeOfDay, GUILayout.MaxWidth(200));
					}
				}
			}
			// asset list
			using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll, GUI.skin.box, GUILayout.ExpandHeight(true)))
			{
				m_Scroll = scope.scrollPosition;
				m_AssetsShown = 0;
				DisplayList(m_IsFlatMode ? m_LoadedAssetsFlat : m_LoadedAssets);
			}
			// footer
			if (m_AssetsShown == m_AssetShowCount)
			{
				if (GUILayout.Button(m_AssetsShown + " assets. Show more..."))
				{
					m_AssetShowCount += AssetShowCountStep;
				}
			}
			else
			{
				GUILayout.Box(m_AssetsShown + " assets total.", GUILayout.ExpandWidth(true));
			}

			if (m_MustScrollToSelected)
			{
				Repaint();
			}
		}

		private void HandleKeyDown()
		{
			// special keyboard control when windown is in picker mode
			if (m_SelectionCallback == null || Event.current.type != EventType.KeyDown)
			{
				return;
			}

			switch (Event.current.keyCode)
			{
				case KeyCode.KeypadEnter:
				case KeyCode.Return:
					if (SelectedAssetEntry != null)
					{
						SelectedAssetEntry.FindAsset();
						s_PreviewSelection = false;
						m_SelectionCallback(SelectedAssetEntry.Asset);
						Close();
					}
					break;
				case KeyCode.Escape:
					Close();
					return;
				case KeyCode.DownArrow:
					{
						//todo skip hidden!
						SelectedAssetEntry =
							m_LoadedAssetsFlat
								.SkipWhile(e => e != SelectedAssetEntry)
								.Skip(1)
								.FirstOrDefault(e => !e.Hidden) ?? SelectedAssetEntry;
						m_MustScrollToSelected = true;
						if (SelectedAssetEntry != null && m_EnableSelectionOnClick)
						{
							SelectedAssetEntry.FindAsset();
							s_PreviewSelection = true;
							m_SelectionCallback(SelectedAssetEntry.Asset);
						}
						Repaint();
					}
					break;
				case KeyCode.UpArrow:
					{
						SelectedAssetEntry =
							m_LoadedAssetsFlat
								.Reverse<HierarchyEntry>()
								.SkipWhile(e => e != SelectedAssetEntry)
								.Skip(1)
								.FirstOrDefault(e => !e.Hidden) ?? SelectedAssetEntry;
						m_MustScrollToSelected = true;
						if (SelectedAssetEntry != null && m_EnableSelectionOnClick)
						{
							SelectedAssetEntry.FindAsset();
							s_PreviewSelection = true;
							m_SelectionCallback(SelectedAssetEntry.Asset);
						}
						Repaint();
					}
					break;
			}
		}

		private void DisplayList(List<HierarchyEntry> list)
		{
			if (list == null)
			{
				return;
			}
			foreach (var entry in list)
			{
				if (m_AssetsShown >= m_AssetShowCount)
				{
					return;
				}
				if (entry.Hidden)
				{
					continue;
				}

				if (entry.Children == null)
				{
					// this is a leaf node
					DisplayAsset(entry);
					m_AssetsShown++;
				}
				else
				{
					// this is a folder
					var ex = EditorGUILayout.Foldout(entry.Expanded, entry.Name, true, FolderStyle);
					// store folded status
					if (ex != entry.Expanded)
					{
						if (ex)
						{
							m_FoldedPaths.Remove(entry.Path);
						}
						else
						{
							m_FoldedPaths.Add(entry.Path);
						}
						entry.Expanded = ex;
					}
					// show contents
					// ReSharper disable once AssignmentInConditionalExpression
					if (entry.Expanded)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.Space(10);
							using (new EditorGUILayout.VerticalScope())
							{
								DisplayList(entry.Children);
							}
						}
					}
				}
			}
		}

		private void DisplayAsset(HierarchyEntry entry)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(16);
			var rect = GUILayoutUtility.GetRect(new GUIContent(entry.Name), GUI.skin.label, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();

			var selected = Selection.assetGUIDs.Contains(entry.AssetGuid) || entry == SelectedAssetEntry;
			if (Event.current.type == EventType.Repaint)
			{
				if (m_MustScrollToSelected && selected)
				{
					m_Scroll.y = rect.yMin - (position.height - 50) / 2;
					m_MustScrollToSelected = false;
				}
			}

			if (rect.Contains(Event.current.mousePosition))
			{
				// click & doubleclick
				if (m_SelectionCallback != null)
				{
					if (Event.current.type == EventType.MouseDown && (m_EnableSelectionOnClick || Event.current.clickCount > 1))
					{
						entry.FindAsset();
						s_PreviewSelection = Event.current.clickCount <= 1;
						m_SelectionCallback(entry.Asset);
						if (m_EnableSelectionOnClick)
						{
							SelectedAssetEntry = entry;
						}
						if (Event.current.clickCount > 1)
						{
							Close();
						}
						Event.current.Use();
					}
				}
				// drag
				if (m_SelectionCallback == null && Event.current.type == EventType.MouseDrag)
				{
					DragAndDrop.PrepareStartDrag();
					if (!selected)
					{
						entry.FindAsset();
						DragAndDrop.objectReferences = new[] { entry.Asset };
					}
					else
					{
						DragAndDrop.objectReferences = Selection.objects;
					}
					DragAndDrop.StartDrag("AssetPicker");
					Event.current.Use();
				}
				// context menu
				if (Event.current.type == EventType.ContextClick)
				{
					Vector2 mousePos = Event.current.mousePosition;
					entry.FindAsset();
					var typeName = entry.Asset.GetType().Name;
					DisplayAssetContextMenu(typeName, mousePos);
					Event.current.Use();
				}
				// prevent button from eating RMB
				if (Event.current.isMouse && Event.current.button != 0)
				{
					return;
				}
			}

			var style = selected ? SelectedAssetStyle : AssetStyle;
			if (GUI.Button(rect, entry.Name, style))
			{
				entry.FindAsset();

				if (m_SelectionCallback == null)
				{
					// normal asset selection
					if (Event.current.control)
					{
						// ctrl-click: add/remove to selection
						Selection.objects = selected
							? Selection.objects.Where(o => o != entry.Asset).ToArray()
							: Selection.objects.Concat(new[] { entry.Asset }).ToArray();
					}
					// todo: shift-click
					else if (!Event.current.shift)
					{
						// simple click - select asset
						Selection.activeObject = entry.Asset;
					}
				}
				else
				{
					SelectedAssetEntry = entry;
				}
			}
		}

		public static void DisplayAssetContextMenu(string typeName, Vector2 mousePos)
		{
			FillContextMenuData();
			if (s_ContextMenusFirstLevel.Contains(typeName))
			{
				EditorUtility.DisplayPopupMenu(
					new Rect(mousePos.x, mousePos.y, 0, 0),
					"CONTEXT/" + typeName + "/",
					null);
			}
		}

		private HierarchyEntry m_FirstVisibleEntry = null;

		private void UpdateFilter()
		{
			m_FirstVisibleEntry = null;
			UpdateFilterRecursive(m_LoadedAssets);
			if (m_SelectionCallback != null)
			{
				if (SelectedAssetEntry == null || SelectedAssetEntry.Hidden)
				{
					SelectedAssetEntry = m_FirstVisibleEntry;
				}
				m_MustScrollToSelected = true;
			}
		}

		private bool UpdateFilterRecursive(List<HierarchyEntry> list)
		{
			if (list == null)
			{
				return false;
			}

			var res = false;
			foreach (var entry in list)
			{
				if (entry.Children != null)
				{
					entry.Hidden = !UpdateFilterRecursive(entry.Children);
				}
				else
				{
					entry.Hidden = !MatchesName(entry, m_NameFilter);
					if (!entry.Hidden)
					{
						entry.Hidden = !MatchesLabels(entry, m_LabelsFilter);
					}

					if (!entry.Hidden && m_FirstVisibleEntry == null)
					{
						m_FirstVisibleEntry = entry;
					}
				}

				res |= !entry.Hidden;
			}
			return res;
		}

		private bool MatchesLabels(HierarchyEntry entry, string[] labelsFilter)
		{
			if (labelsFilter == null || labelsFilter.Length == 0)
			{
				return true;
			}

			return m_ValidLabelsGuids.Contains(entry.AssetGuid);
		}

		private bool MatchesName(HierarchyEntry entry, string nameFilter)
		{
			if (string.IsNullOrEmpty(nameFilter))
			{
				return true;
			}

			foreach (var filter in nameFilter.ToLowerInvariant().Split())
			{
				if (!entry.Path.ToLowerInvariant().Contains(filter))
				{
					return false;
				}
			}

			return true;
		}

		private void UpdateAssetList()
		{
			m_LoadedAssets = new List<HierarchyEntry>();
			m_LoadedAssetsFlat.Clear();

			GenerateLabelAssets();

			m_HierarchyProperty = new HierarchyProperty(HierarchyType.Assets);
			m_HierarchyProperty.SetSearchFilter(GetFilter(), (int)SearchableEditorWindow.SearchMode.All);

			while (m_HierarchyProperty.Next(null))
			{
				AddAssetInfo(m_HierarchyProperty);
			}

			FilterAssets(ref m_LoadedAssets);
			m_LoadedAssets.ForEach(SortChildren);

			m_LoadedAssetsFlat.Clear();
			BuildFlatAssets(m_LoadedAssets);

			UpdateFilter();
			if (m_SelectionCallback != null)
			{
				SelectedAssetEntry = m_LoadedAssetsFlat.FirstOrDefault();
			}
		}

		private string GetFilter()
		{
			m_StringBuilder.Remove(0, m_StringBuilder.Length);

			m_StringBuilder.AppendFormat("t:{0} ", m_TypeFilter);

			if (m_LabelsFilter != null)
			{
				foreach (string label in m_LabelsFilter)
				{
					// only specify the first label - rest are treated by OR
					// and would give more irrelevant results
					m_StringBuilder.AppendFormat("l:{0} ", label);
					break;
				}
			}

			return m_StringBuilder.ToString();
		}

		private void GenerateLabelAssets()
		{
			m_ValidLabelsGuids.Clear();
			if (m_LabelsFilter == null)
				return;

			for (int i = 0; i < m_LabelsFilter.Length; i++)
			{
				var label = m_LabelsFilter[i];
				var guids = AssetDatabase.FindAssets($"t:{m_TypeFilter} l:{label}");
				if (i == 0)
				{
					m_ValidLabelsGuids.AddRange(guids);
				}
				else
				{
					var newGuids = new HashSet<string>(guids);
					m_ValidLabelsGuids.RemoveWhere(g => !newGuids.Contains(g));
				}
			}
		}

		private void AddAssetInfo(HierarchyProperty hierarchyProperty)
		{
			var path = AssetDatabase.GUIDToAssetPath(hierarchyProperty.guid);
			var folders = path.Split('/');
			var list = m_LoadedAssets;
			HierarchyEntry parent = null;
			for (int ii = 1; ii < folders.Length - 1; ii++) // 1 to skip Assets folder, -1 to skip asset name
			{
				var folder = folders[ii];
				var p = parent;
				parent = list.SingleOrDefault(e => e.Name == folder && e.Children != null);
				if (parent == null)
				{
					parent = new HierarchyEntry
					{
						Parent = p,
						Name = folder,
						Path = string.Join("/", folders, 0, ii + 1),
					};
					list.Add(parent);
					parent.Expanded = !m_FoldedPaths.Contains(parent.Path);
				}
				list = parent.Children = parent.Children ?? new List<HierarchyEntry>();
			}
			var hierarchyEntry = new HierarchyEntry
			{
				Parent = parent,
				Name = hierarchyProperty.name,
				Path = path,
				InstanceId = hierarchyProperty.instanceID,
				AssetGuid = hierarchyProperty.guid,
				Children = hierarchyProperty.isFolder ? new List<HierarchyEntry>() : null
			};
			list.Add(hierarchyEntry);
		}

		private void SortChildren(HierarchyEntry entry)
		{
			if (entry.Children == null)
			{
				return;
			}

			entry.Children.Sort();
			foreach (var child in entry.Children)
			{
				SortChildren(child);
			}
		}

		private void FilterAssets(ref List<HierarchyEntry> entries)
		{
			bool prefabType = typeof(MonoBehaviour).IsAssignableFrom(AssetType);
			if (!prefabType
				&& m_Filters.Count <= 0)
			{
				return;
			}

			if (entries == null)
			{
				return;
			}

			entries.ForEach(e => FilterAssets(ref e.Children));

			entries = entries
				.Where(FitsFilters)
				.ToList();
		}

		private bool FitsFilters(HierarchyEntry he)
		{
			if (he.Children != null && he.Children.Count > 0)
				return true;
			foreach (var filter in m_Filters)
			{
				if (!filter(he))
					return false;
			}

			return true;
		}

		private void BuildFlatAssets(List<HierarchyEntry> entries)
		{
			foreach (var e in entries)
			{
				if (e.Children != null)
				{
					BuildFlatAssets(e.Children);
				}
				else
				{
					m_LoadedAssetsFlat.Add(e);
				}
			}
		}
	}


}