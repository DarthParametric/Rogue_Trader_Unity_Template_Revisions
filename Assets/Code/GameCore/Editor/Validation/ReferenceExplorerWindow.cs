using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Enums;
using Owlcat.Editor.Core.Utility;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Validation
{
    public class ReferenceExplorerWindow : EditorWindow
    {
        private ReferenceGraph m_Graph;
        private readonly List<ReferenceGraph.Entry> m_FilteredEntries = new List<ReferenceGraph.Entry>();
        private int m_PageStart;
        private Vector2 m_Scroll;
        private const int PageSize = 25;

        private readonly Dictionary<string, bool> m_Foldouts = new Dictionary<string, bool>();
        private Texture2D m_SmallErrorIcon;
        private Texture2D m_SmallWarningIcon;
        private readonly GUIContent m_Content = new GUIContent(); // for reuse
        private string m_Filter;
        private string m_TypeFilter;
        private Stack<string> m_RefChaseStack = new Stack<string>();
        private int m_Zebra;
        private bool m_OnlyErrors;
	    private int m_OwnerFilter;
	    private bool m_GraphDirty;

	    private static readonly string[] s_PossibleOwners =
		    new[] {"Everyone", "No one"}
			    .Concat(
				    Enum.GetValues(typeof(Authors)).Cast<Authors>().Select(d => d.ToString()))
			    .ToArray();

	    [MenuItem("Tools/DREAMTOOL/Explore")]
        public static void ShowWindow()
        {
            GetWindow<ReferenceExplorerWindow>().titleContent.text="DREAMTOOL";
        }

        void OnEnable()
        {
            m_SmallErrorIcon = EditorGUIUtility.FindTexture("d_console.erroricon.sml");
            m_SmallWarningIcon = EditorGUIUtility.FindTexture("d_console.warnicon.sml");
        }

        void OnGUI()
        {
            DrawToolBar();

            if (m_Graph == null)
                return;
            using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scope.scrollPosition;
                m_Zebra = 0;
                for (int ii = m_PageStart; ii < Math.Min(m_FilteredEntries.Count, m_PageStart + PageSize); ii++)
                {
                    var entry = m_FilteredEntries[ii];
                    m_RefChaseStack.Clear();
                    DrawEntry(entry, position.width - 20);
                }
            }
            DrawSubbar();
        }

        private void DrawSubbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("<<", EditorStyles.toolbarButton, GUILayout.Width(48)) && m_PageStart > 0)
                {
                    m_PageStart = 0;
                }
                if (GUILayout.Button("<", EditorStyles.toolbarButton, GUILayout.Width(48)) && m_PageStart > 0)
                {
                    m_PageStart = Math.Max(0, m_PageStart - PageSize);
                }
                GUILayout.Label($"Page {m_PageStart / PageSize} / {(m_FilteredEntries.Count + 1) / PageSize}");
                if (GUILayout.Button(">", EditorStyles.toolbarButton, GUILayout.Width(48)) &&
                    m_PageStart <= m_FilteredEntries.Count - PageSize)
                {
                    m_PageStart += PageSize;
                }
                if (GUILayout.Button(">>", EditorStyles.toolbarButton, GUILayout.Width(48)) &&
                    m_PageStart <= m_FilteredEntries.Count - PageSize)
                {
                    m_PageStart = Math.Max(m_FilteredEntries.Count - PageSize, 0);
                }
            }
        }

        private void DrawEntry(ReferenceGraph.Entry entry, float width)
        {
            using (var entryscope = new EditorGUILayout.VerticalScope())
            {
                EditorGUI.DrawRect(entryscope.rect, OwlcatEditorStyles.Instance.ArrayZebraColor(m_Zebra++));

                if (m_RefChaseStack.Count==0)
                {
                    bool expanded = DrawEntryHeader(entry, entry.ObjectGuid);

                    if (!expanded)
                        return;
                }

                m_RefChaseStack.Push(entry.ObjectGuid);

                if (entry.ValidationState != ReferenceGraph.ValidationStateType.Normal)
                {
	                EditorGUILayout.HelpBox(
		                entry.ValidationResult,
		                entry.ValidationState == ReferenceGraph.ValidationStateType.Error
			                ? MessageType.Error
			                : MessageType.Warning);
                }

                for (int ii = 0; ii < entry.References?.Count; ii++)
                {
                    var refData = entry.References[ii];
                    using (new EditorGUILayout.HorizontalScope(GUILayout.MaxWidth(width)))
                    {
                        GUILayout.Space(20);

                        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
                        {
                            if (GUILayout.Button(
                                $"Referenced ({ReferenceGraph.EntryRefMaskToString(entry, refData.ReferenceTypeMask)}) by {refData.ReferencingObjectName}:{refData.ReferencingObjectType} in {Path.GetFileNameWithoutExtension(refData.AssetPath)}",
                                EditorStyles.miniLabel))
                            {
                                Selection.activeObject =
                                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(refData.AssetPath);
                            }
                            if (refData.RefChasingAssetGuid != null)
                            {
                                var referencingEntry = m_Graph.FindEntryByGuid(refData.RefChasingAssetGuid);
                                if (referencingEntry != null)
                                {
                                    var subExpanded = DrawEntryHeader(referencingEntry, ii + "+" + entry.ObjectGuid);
                                    if (subExpanded && !m_RefChaseStack.Contains(refData.RefChasingAssetGuid))
                                    {
                                        DrawEntry(referencingEntry, width - 20);
                                    }
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox("Cannot follow referencing object.", MessageType.Error);
                                }
                            }
                        }
                    }

                }

                m_RefChaseStack.Pop();
            }
        }

        private bool DrawEntryHeader(ReferenceGraph.Entry entry, string expandedKey)
        {
            bool expanded;
            m_Foldouts.TryGetValue(expandedKey, out expanded);

            var icon = entry.ValidationState == ReferenceGraph.ValidationStateType.Error
                ? m_SmallErrorIcon
                : entry.ValidationState == ReferenceGraph.ValidationStateType.Warning
                    ? m_SmallWarningIcon
                    : null;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(icon, GUILayout.Width(18));

                m_Content.text = entry.ObjectName;
                m_Content.image = null;

                if (EditorGUILayout.Foldout(expanded, m_Content) != expanded)
                {
                    m_Foldouts[expandedKey] = !expanded;
                }

				GUILayout.FlexibleSpace();

	            DrawOwnerLabel(entry);

				GUILayout.Label(entry.ObjectType, GUILayout.Width(200));
                GUILayout.Label($"{entry.References?.Count ?? 0} refs", GUILayout.Width(64));
                GUILayout.Label(
                    $"{ReferenceGraph.EntryRefMaskToString(entry, entry.FullReferencesMask)}",
                    GUILayout.Width(200));

                if (GUILayout.Button("Select", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    Selection.activeObject =
                        AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                            AssetDatabase.GUIDToAssetPath(entry.ObjectGuid));
                }
            }
            return expanded;
        }

	    private void DrawOwnerLabel(ReferenceGraph.Entry entry)
	    {
		    if (m_RefChaseStack.Count > 0)
		    {
			    GUILayout.Space(150);
				return;
		    }
		    GUILayout.Label(entry.OwnerName, GUILayout.Width(150));
		    var r = GUILayoutUtility.GetLastRect();
		    if (Event.current.type == EventType.ContextClick && r.Contains(Event.current.mousePosition))
		    {
			    var m = new GenericMenu();
			    foreach (var designer in Enum.GetValues(typeof(Authors)).Cast<Authors>())
			    {
				    m.AddItem(
					    new GUIContent(designer.ToString()),
					    entry.OwnerName == designer.ToString(),
					    d =>
					    {
						    entry.OwnerName = d.ToString();
							//m_Graph.Save();
						    BlueprintToOwnerMatcher.Instance.UpdateCache(entry.ObjectGuid, entry.OwnerName);
						    m_GraphDirty = true;
					    },
					    designer);
			    }
				m.ShowAsContext();
		    }
	    }

	    private void DrawToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Load graph", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    m_Graph = ReferenceGraph.Reload();
                    Filter();
                }
                if (GUILayout.Button("Collect graph", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    ReferenceGraph.CollectMenu();
                    m_Graph = ReferenceGraph.Reload();
                    Filter();
                }
                if (m_Graph != null && GUILayout.Button("Analyze references", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    try
                    {
                        m_Graph.SetupChasingLinks();
	                    m_Graph.MatchToOwners();
                        m_Graph.AnalyzeReferencesInBlueprints();
                        m_Graph.AnalyzeReferencesInScenes();
                        m_Graph.Save();
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
                if (m_Graph != null && GUILayout.Button("Validate references", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    try
                    {
                        m_Graph.ValidateReferences();
						m_Graph.ValidateObjects();
                        m_Graph.Save();
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }

                if (GUILayout.Button("Redo everything", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
					ReferenceGraph.CollectMenu();
					m_Graph = ReferenceGraph.Reload();
	                RedoEverything(m_Graph);					
                    Filter();
                }

                GUILayout.FlexibleSpace();

				if (m_Graph==null)
					return;

	            if (m_GraphDirty)
	            {
		            using (GuiScopes.Color(new Color(0.86f, 0.35f, 0.35f)))
		            {
			            if (GUILayout.Button("Save owner changes", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			            {
				            m_Graph.Save();
				            BlueprintToOwnerMatcher.Instance.SaveCache();
				            m_GraphDirty = false;
			            }
		            }
		            GUILayout.FlexibleSpace();
				}

                var oe = GUILayout.Toggle(m_OnlyErrors, m_SmallErrorIcon, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
                if (oe != m_OnlyErrors)
                {
                    m_OnlyErrors = oe;
                    Filter();
                }

	            var idx = EditorGUILayout.Popup(
		            m_OwnerFilter,
		            s_PossibleOwners,
		            EditorStyles.toolbarPopup,
		            GUILayout.Width(120));
	            if (idx != m_OwnerFilter)
	            {
		            m_OwnerFilter = idx;
					Filter();
	            }

	            TypePicker.ToolbarButton(
		            string.IsNullOrEmpty(m_TypeFilter) ? "All types" : m_TypeFilter,
		            () => new[] { typeof(BlueprintScriptableObject) }.Concat(ReferenceGraph.GetRelevantTypes()),
		            SetTypeFilter,
		            false,
		            GUILayout.Width(120));

                var f = GUILayout.TextField(m_Filter, "ToolbarSeachTextField", GUILayout.Width(200));
                if (GUILayout.Button("", "ToolbarSeachCancelButton"))
                {
                    // Remove focus if cleared
                    f = "";
                    GUI.FocusControl(null);
                }

                if (m_Filter != f)
                {
                    m_Filter = f;
                    Filter();
                }
            }
        }

	    public static void RedoEverything(ReferenceGraph graph)
	    {
			graph.SetupChasingLinks();
			graph.MatchToOwners();
			graph.AnalyzeReferencesInBlueprints();
			graph.AnalyzeReferencesInScenes();

			graph.ValidateReferences();
			graph.ValidateObjects();
			graph.Save();
		}

	    private void SetTypeFilter(Type t)
        {
            if (t == typeof(BlueprintScriptableObject))
            {
                m_TypeFilter = null;
            }
            else
            {
                m_TypeFilter = t.Name;
            }
            Filter();
        }

        private void Filter()
        {
            m_FilteredEntries.Clear();
            IEnumerable<ReferenceGraph.Entry> all = m_Graph.Entries;
	        if (m_OnlyErrors)
	        {
		        all = all.Where(e => e.ValidationState != ReferenceGraph.ValidationStateType.Normal);
	        }
			if (m_OwnerFilter>0)
			{
				all = all.Where(
					e => m_OwnerFilter == 1 ? string.IsNullOrEmpty(e.OwnerName) : e.OwnerName == s_PossibleOwners[m_OwnerFilter]);
			}
            if (!string.IsNullOrEmpty(m_TypeFilter))
            {
                all = all.Where(e => e.ObjectType == m_TypeFilter);
            }
            if (!string.IsNullOrEmpty(m_Filter))
            {
                var split = m_Filter.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                all = all.Where(e => split.All(e.ObjectName.ToLowerInvariant().Contains));
            }

            m_FilteredEntries.AddRange(all);
            m_PageStart = Math.Max(0, Math.Min(m_PageStart, m_FilteredEntries.Count - PageSize));
        }
    }
}