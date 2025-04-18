#if UNITY_EDITOR && EDITOR_FIELDS
using Code.GameCore.Editor.EtudesViewer;
using System;
using System.Collections.Generic;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Editor;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using System.Linq;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.EtudesViewer;
using Kingmaker.Editor.Validation;

namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
    public class EtudesViewer : KingmakerWindowBase
    {
        private const float IconH = 16.0f;
        private const float IconW = 16.0f;

        private const int DefaultIndent = 2;
        private const float IndentFactor = IconW;

        private string parent;
        private string selected;
        private Vector2 m_ScrollPos;
        private Dictionary<string, EtudeIdReferences> loadedEtudes = new();
        private Dictionary<string, EtudeIdReferences> filteredEtudes = new();
        private string rootEtudeId = "4f66e8b792ecfad46ae1d9ecfd7ecbc2";
        private bool UseFilter;
        private bool ShowOnlyTargetAreaEtudes;
        private bool ShowOnlyFlagLikes;

        private BlueprintArea TargetArea;
        public Texture2D areaIcon;
        public Texture2D actionIcon;
        public Texture2D commentIcon;
        public Texture2D completeParentIcon;
        public Texture2D etudeState;
        public Texture2D notStarted;
        public Texture2D started;
        public Texture2D active;
        public Texture2D completeBeforeActive;
        public Texture2D complitionBlocked;
        public Texture2D completed;
        public Texture2D foldoutClosed;
        public Texture2D foldoutClosedAll;
        public Texture2D foldoutOpened;
        public Texture2D foldoutOpenedAll;
        public Texture2D validationProblem;
        public Texture2D grid;

        private EtudeChildrenDrawer etudeChildrenDrawer;
        private Rect workspaceRect;

        private float currentScrollViewWidth = 450f;
        private bool resize = false;
        private Rect cursorChangeRect;
        private int m_Indent = DefaultIndent;

        public string Find = "";

        [MenuItem("Design/EtudesViewer")]
        public static void ShowTool()
        {
            var window = GetWindow<EtudesViewer>();
            window.titleContent.text = "EtudesViewer";
            window.Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            loadedEtudes = EtudesTreeLoader.Instance.LoadedEtudes;
            etudeChildrenDrawer = new EtudeChildrenDrawer(loadedEtudes, this);
            etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
            
            areaIcon = EditorGUIUtility.Load("BlueprintIcons/BlueprintArea.png") as Texture2D;

            // actionIcon = EditorGUIUtility.Load("BlueprintIcons/Cutscene.png") as Texture2D;
            actionIcon = EditorGUIUtility.IconContent("d_Settings").image as Texture2D;

            commentIcon = EditorGUIUtility.Load("BlueprintIcons/BlueprintDialog.png") as Texture2D;

            completeParentIcon = EditorGUIUtility.Load("revert.png") as Texture2D;

            etudeState = EditorGUIUtility.Load("Icons/etude_state.png") as Texture2D;

            notStarted = EditorGUIUtility.Load("Gray_Background.png") as Texture2D;
            started = EditorGUIUtility.Load("Cyan_Background.png") as Texture2D;
            active = EditorGUIUtility.Load("Green_Background.png") as Texture2D;
            completed = EditorGUIUtility.Load("Yellow_Background.png") as Texture2D;
            completeBeforeActive = EditorGUIUtility.Load("Blue_Background.png") as Texture2D;
            complitionBlocked = EditorGUIUtility.Load("Orange_Background.png") as Texture2D;
            validationProblem = EditorGUIUtility.Load("Red_Background.png") as Texture2D;

            foldoutClosed = EditorGUIUtility.Load("FoldoutClosed.png") as Texture2D;
            foldoutClosedAll = EditorGUIUtility.Load("FoldoutClosedAll.png") as Texture2D;
            foldoutOpened = EditorGUIUtility.Load("FoldoutOpened.png") as Texture2D;
            foldoutOpenedAll = EditorGUIUtility.Load("FoldoutOpenedAll.png") as Texture2D;
            
            grid = EditorGUIUtility.Load("grid.png") as Texture2D;

            wantsMouseMove = wantsMouseEnterLeaveWindow = true;

            Selection.selectionChanged += OnSelectionChanged;
        }

        protected override void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void Update()
        {
            etudeChildrenDrawer?.Update();
        }

        protected override void OnGUI()
        {
            if (Event.current.type == EventType.Layout && etudeChildrenDrawer != null)
            {
                etudeChildrenDrawer.UpdateBlockersInfo();
            }

            base.OnGUI();
            
            if (parent == null)
            {
                parent = rootEtudeId;
                selected = parent;
            }

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box, GUILayout.MinHeight(60),
                GUILayout.MinWidth(300)))
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinHeight(60),
                    GUILayout.MinWidth(300)))
                {
                    if (string.IsNullOrEmpty(parent))
                        return;

                    EditorGUILayout.LabelField(
                        $"{EtudesViewerTexts.HierarchyFromEtude} : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}");
                    EditorGUILayout.LabelField(
                        $"{EtudesViewerTexts.SelectedEtude} : {(loadedEtudes.Count == 0 ? "" : loadedEtudes[selected].Name)}");

                    if (loadedEtudes.Count != 0)
                    {
                        if (GUILayout.Button(EtudesViewerTexts.RefreshEtudesTree, GUILayout.MinWidth(300), GUILayout.MaxWidth(300)))
                        {
                            EtudesTreeLoader.Instance.ReloadBlueprintsTree();
                            loadedEtudes = EtudesTreeLoader.Instance.LoadedEtudes;
                            etudeChildrenDrawer = new EtudeChildrenDrawer(loadedEtudes, this);
                            etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
                            
                            ApplyFilter();
                        }
                    }
                    
                    if (GUILayout.Button(EtudesViewerTexts.RefreshIndexDreamtool, GUILayout.MinWidth(300), GUILayout.MaxWidth(300)))
                    {
                        ReferenceGraph.CollectMenu();
                        etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
                        etudeChildrenDrawer.ReferenceGraph.AnalyzeReferencesInBlueprints();
                        etudeChildrenDrawer.ReferenceGraph.Save();
                    }

                    UseFilter = GUILayout.Toggle(UseFilter, EtudesViewerTexts.UseFilter);

                }

                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinHeight(60),
                    GUILayout.MinWidth(300)))
                {
                    GUILayout.Label(EtudesViewerTexts.Find);
                    Find = GUILayout.TextField(Find,GUILayout.MinWidth(250));

                    Event e = Event.current;
                    if (e.type == EventType.Repaint)
                    {
                        Rect mouseArea = GUILayoutUtility.GetLastRect();
                        if (workspaceRect.Contains(Event.current.mousePosition))
                        {
                            GUI.FocusControl(null);
                        }
                    }

                    
                }

                if (UseFilter)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinHeight(60),
                        GUILayout.MinWidth(300)))
                    {
                        ShowOnlyTargetAreaEtudes = GUILayout.Toggle(ShowOnlyTargetAreaEtudes, EtudesViewerTexts.OnlyAreaEtudes);

                        if (ShowOnlyTargetAreaEtudes)
                        {
                            TargetArea =
                                (BlueprintArea)BlueprintEditorUtility.ObjectField(EtudesViewerTexts.TargetArea, TargetArea,
                                    typeof(BlueprintArea), false);
                        }

                        ShowOnlyFlagLikes = GUILayout.Toggle(ShowOnlyFlagLikes, EtudesViewerTexts.OnlyEtudesLikeFlags);

                        if (GUILayout.Button(EtudesViewerTexts.ApplyFilter, GUILayout.MaxWidth(300)))
                        {
                            EtudesTreeLoader.Instance.ReloadBlueprintsTree();
                            loadedEtudes = EtudesTreeLoader.Instance.LoadedEtudes;
                            etudeChildrenDrawer = new EtudeChildrenDrawer(loadedEtudes, this);
                            etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
                            ApplyFilter();
                        }
                    }
                }

                if (etudeChildrenDrawer != null)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinHeight(60),
                        GUILayout.MinWidth(300)))
                    {
                        etudeChildrenDrawer.DefaultExpandedNodeWidth = EditorGUILayout.Slider(
                            EtudesViewerTexts.NodeExpandWidth,
                            etudeChildrenDrawer.DefaultExpandedNodeWidth,
                            200, 2000);
                    }
                }

                if (etudeChildrenDrawer != null && !etudeChildrenDrawer.BlockersInfo.IsEmpty)
                {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.MinHeight(60),
                        GUILayout.MinWidth(350)))
                    {
                        var info = etudeChildrenDrawer.BlockersInfo;
                        var lockSelf = info.Blockers.Contains(info.Owner);
                        if (lockSelf)
                        {
                            GUILayout.Label(EtudesViewerTexts.CompletionIsBlockedByEtudesCondition);
                        }

                        if (info.Blockers.Count > 1 || !lockSelf)
                        {
                            GUILayout.Label(EtudesViewerTexts.CompletionIsBlockedByChildensConditions);
                            foreach (var blocker in info.Blockers)
                            {
                                var bluprint = blocker.Blueprint;
                                if (GUILayout.Button(bluprint.name))
                                {
                                    Selection.activeObject = BlueprintEditorWrapper.Wrap(bluprint);
                                }
                            }
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (var scope = new EditorGUILayout.ScrollViewScope(m_ScrollPos,GUI.skin.box, GUILayout.Width(currentScrollViewWidth)))
                {
                    m_Indent = EditorGUILayout.IntSlider(
                        EtudesViewerTexts.IndentWidth,
                        m_Indent,
                        1, 10,
                        GUILayout.MaxWidth(300));

                    EditorGUILayout.LabelField($"{EtudesViewerTexts.HierarchyTree} {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", GUILayout.MinHeight(32));

                    if (loadedEtudes.Count == 0)
                    {
                        EditorGUILayout.HelpBox(EtudesViewerTexts.EtudesAreNotSetup, MessageType.Info);
                        if (GUILayout.Button(EtudesViewerTexts.SetupEtudesTree, GUILayout.MinWidth(300), GUILayout.MaxWidth(300)))
                        {
                            EtudesTreeLoader.Instance.ReloadBlueprintsTree();
                            loadedEtudes = EtudesTreeLoader.Instance.LoadedEtudes;
                            etudeChildrenDrawer = new EtudeChildrenDrawer(loadedEtudes, this);
                            etudeChildrenDrawer.ReferenceGraph = ReferenceGraph.Reload();
                        }
                        return;
                    }

                    if (Application.isPlaying)
                    {
                        foreach (var etude in Game.Instance.Player.EtudesSystem.Etudes.RawFacts)
                        {
                            FillPlaymodeEtudeData(etude);
                        }
                    }

                    ShowBlueprintsTree();

                    m_ScrollPos = scope.scrollPosition;
                }

                ResizeScrollView();

                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true)))
                {
                    EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true) ,GUILayout.ExpandHeight(true));

                    if (Event.current.type == EventType.Repaint)
                    {
                        workspaceRect = GUILayoutUtility.GetLastRect();
                        etudeChildrenDrawer?.SetWorkspaceRect(workspaceRect);
                    }
                    etudeChildrenDrawer.OnGUI();
                }
            }
        }

        private void OnSelectionChanged()
        {
            var bpw = Selection.activeObject as BlueprintEditorWrapper;
            if (bpw == null)
            {
                return;
            }

            if (loadedEtudes.TryGetValue(bpw.Blueprint.AssetGuid, out var etude))
            {
                selected = etude.Id;
                string parentId = etude.ParentId;
                while (!string.IsNullOrEmpty(parentId))
                {
                    if (!loadedEtudes.TryGetValue(parentId, out var parentEtude))
                    {
                        break;
                    }
                    parentEtude.Foldout = true;
                    parentId = parentEtude.ParentId;
                }
                Repaint();
            }
        }

        private void ApplyFilter()
        {
            filteredEtudes = loadedEtudes;

            if (ShowOnlyTargetAreaEtudes && TargetArea!=null)
            {
                var etudesOfArea = GetAreaEtudes();
                filteredEtudes = etudesOfArea;
            }

            if (ShowOnlyFlagLikes)
            {
                var flagLikeEtudes = GetFlaglikeEtudes();
                filteredEtudes = filteredEtudes.Keys
                    .Intersect(flagLikeEtudes.Keys)
                    .ToDictionary(key => key, key => filteredEtudes[key]);
            }

            if (!string.IsNullOrEmpty(Find))
            {
                var namedEtudes = GetNamedEtudes();
                filteredEtudes = filteredEtudes.Keys
                    .Intersect(namedEtudes.Keys)
                    .ToDictionary(key => key, key => filteredEtudes[key]);
            }
        }
        
        [MenuItem("CONTEXT/BlueprintEtude/Open in EtudeViewer")]
        public static void OpenAssetInEtudeViewer()
        {
            BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);
            if (blueprint == null)
                return;
            
            EtudeChildrenDrawer.TryToSetParent(blueprint.AssetGuid);
            
        }

        private Dictionary<string, EtudeIdReferences> GetFlaglikeEtudes()
        {
            Dictionary<string, EtudeIdReferences> etudesFlaglike = new Dictionary<string, EtudeIdReferences>();

            foreach (var etude in loadedEtudes)
            {
                bool flaglike = string.IsNullOrEmpty(etude.Value.ChainedTo) &&
                               // (etude.Value.ChainedId.Count == 0) &&
                                string.IsNullOrEmpty(etude.Value.LinkedTo) &&
                                string.IsNullOrEmpty(etude.Value.LinkedArea) && !ParentHasArea(etude.Value);

                if (flaglike)
                {
                    etudesFlaglike.Add(etude.Key,etude.Value);
                    AddParentsToDictionary(etudesFlaglike, etude.Value);
                }
            }

            return etudesFlaglike;
        }

        public bool ParentHasArea(EtudeIdReferences etude)
        {
            if (string.IsNullOrEmpty(etude.ParentId))
                return false;

            if (string.IsNullOrEmpty(loadedEtudes[etude.ParentId].LinkedArea))
            {
                return ParentHasArea(loadedEtudes[etude.ParentId]);
            }

            return true;
        }

        private Dictionary<string, EtudeIdReferences> FilterEtudes(Func<EtudeIdReferences, bool> filter)
        {
            var filtered = new Dictionary<string, EtudeIdReferences>();
            foreach ((string key, var etude) in loadedEtudes)
            {
                if (filter(etude))
                {
                    filtered.TryAdd(key, etude);
                    AddChildrenToDictionary(filtered, etude);
                    AddParentsToDictionary(filtered, etude);
                }
            }
            return filtered;
        }

        private Dictionary<string, EtudeIdReferences> GetNamedEtudes()
        {
            return FilterEtudes(etude => etude.Name.Contains(Find, StringComparison.OrdinalIgnoreCase));
        }

        private Dictionary<string, EtudeIdReferences> GetAreaEtudes()
        {
            return FilterEtudes(etude => etude.LinkedArea == TargetArea.AssetGuid);
        }

        private void AddChildrenToDictionary(Dictionary<string, EtudeIdReferences> dictionary, EtudeIdReferences etude)
        {
            foreach (string id in etude.ChildrenId)
            {
                var childEtude = loadedEtudes[id];
                dictionary.TryAdd(id, childEtude);
                AddChildrenToDictionary(dictionary, childEtude);
            }
        }

        private void AddParentsToDictionary(Dictionary<string, EtudeIdReferences> dictionary, EtudeIdReferences etude)
        {
            while (true)
            {
                if (string.IsNullOrEmpty(etude.ParentId))
                    return;

                var parentEtude = loadedEtudes[etude.ParentId];
                dictionary.TryAdd(etude.ParentId, parentEtude);
                etude = parentEtude;
            }
        }

        private void FillPlaymodeEtudeData(Etude etude)
        {
            EtudeIdReferences etudeIdReferences = loadedEtudes[etude.Blueprint.AssetGuid];
            UpdateStateInRef(etude, etudeIdReferences);
        }

        void UpdateStateInRef(Etude etude, EtudeIdReferences etudeIdReferences)
        {
            if (etude.IsCompleted)
            {
                etudeIdReferences.State = EtudeIdReferences.EtudeState.Completed;
                return;
            }

            if (etude.CompletionInProgress)
            {
                etudeIdReferences.State = EtudeIdReferences.EtudeState.CompletionInProgress;
                return;
            }

            if (etude.IsPlaying)
            {
                etudeIdReferences.State = EtudeIdReferences.EtudeState.Playing;
            }
            else
            {
                etudeIdReferences.State = EtudeIdReferences.EtudeState.Started;
            }
        }

        private void ShowBlueprintsTree()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawEtude(rootEtudeId,loadedEtudes[rootEtudeId]);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(m_Indent * IndentFactor);

                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        ShowParentTree(loadedEtudes[rootEtudeId]);
                    }
                }
            }
        }

        private static void DrawTintedIconWithTooltip(Texture2D icon, Color tint, string tooltip)
        {
            GUILayout.Box(new GUIContent("", tooltip), GUIStyle.none, GUILayout.Width(IconW), GUILayout.Height(IconH));

            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), icon, ScaleMode.StretchToFill,
                true, 0, tint, 0, 0);
        }

        private void DrawStateIcon(Color stateColor, string stateTooltip)
        {
            DrawTintedIconWithTooltip(etudeState, stateColor, stateTooltip);
        }

        private void DrawEtude(string etudeID, EtudeIdReferences etude)
        {
            if (Application.isPlaying)
            {
                UpdateEtudeState(etudeID, etude);
            }

            using (new GUILayout.HorizontalScope())
            {
                Color? stateColor = null;
                if (Application.isPlaying)
                {
                    // State icon with tooltip
                    (var color, string tooltip) = GetEtudeStateColorAndTooltip(etude);
                    DrawStateIcon(color, tooltip);
                    stateColor = color;
                }

                string problem = GetEtudeValidationProblem(etudeID, etude);
                if (!string.IsNullOrEmpty(problem))
                {
                    DrawStateIcon(Color.red, $"Validation problem:\n{problem}");
                    stateColor = Color.red;
                }

                // Label + select button
                GUIContent content = new GUIContent(etude.Name, etude.Comment);
                var style = selected == etudeID
                    ? new GUIStyle(EditorStyles.selectionRect) // Make a copy to keep original styles safe
                    : new GUIStyle(EditorStyles.label);

                if (stateColor.HasValue)
                {
                    style.normal.textColor = stateColor.Value;
                }

                if (GUILayout.Button(content, style, GUILayout.MaxWidth(300)))
                {
                    if (selected != etudeID)
                    {
                        selected = etudeID;
                    }
                    else
                    {
                        parent = etudeID;
                        etudeChildrenDrawer.SetParent(parent, workspaceRect);
                    }

                    var bp = BlueprintsDatabase.LoadById<BlueprintEtude>(etudeID);
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
                }

                if (etude.CompleteParent)
                {
                    DrawTintedIconWithTooltip(completeParentIcon, Color.white, "The parent is completed as well");
                }

                if (!string.IsNullOrEmpty(etude.Comment))
                {
                    DrawTintedIconWithTooltip(commentIcon, Color.white, etude.Comment);
                }

                if (etude.HasSomeMechanics)
                {
                    DrawTintedIconWithTooltip(actionIcon, new Color(0.5f, 1.0f, 0.5f), $"Has some mechanics inside");
                }

                if (!string.IsNullOrEmpty(etude.LinkedArea))
                {
                    DrawTintedIconWithTooltip(areaIcon, Color.white, $"Has area linked:\n{etude.LinkedAreaName}");
                }
            }
        }

        private (Color, string) GetEtudeStateColorAndTooltip(EtudeIdReferences etude)
        {
            switch (etude.State)
            {
                case EtudeIdReferences.EtudeState.NotStarted:
                {
                    return (Color.gray, "Not Started");
                }
                case EtudeIdReferences.EtudeState.Started:
                {
                    return (Color.cyan, "Started");
                }
                case EtudeIdReferences.EtudeState.Playing:
                {
                    return (Color.green, "Playing");
                }
                case EtudeIdReferences.EtudeState.CompleteBeforeActive:
                {
                    return (Color.yellow, "Completed");
                }
                case EtudeIdReferences.EtudeState.CompletionInProgress:
                {
                    return (new Color(1.0f, 0.5f, 0.0f), "Completion In Progress");
                }
                case EtudeIdReferences.EtudeState.Completed:
                {
                    return (Color.yellow, "Completed");
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetEtudeValidationProblem(string etudeID, EtudeIdReferences etude)
        {
            if (!string.IsNullOrEmpty(etude.ChainedTo) && !string.IsNullOrEmpty(etude.LinkedTo))
                return "Etude is linked and chained at the same time";

            foreach (string chained in etude.ChainedId)
            {
                if (loadedEtudes[chained].ParentId != etude.ParentId)
                    return "Etude chained to wrong parent";

                if (loadedEtudes[chained].Id == etude.Id)
                    return "Etude is chained to itself";
            }

            foreach (string linked in etude.LinkedId)
            {
                if (loadedEtudes[linked].ParentId != etude.ParentId && loadedEtudes[linked].ParentId != etudeID)
                    return "Etude is linked to wrong parent";

                if (loadedEtudes[linked].Id == etude.Id)
                    return "Etude is linked to itself";
            }

            return string.Empty;
        }

        public void UpdateEtudeState(string etudeID, EtudeIdReferences etude)
        {
            BlueprintEtude blueprintEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(etudeID);

            var item = Game.Instance.Player.EtudesSystem.Etudes.Get(blueprintEtude);
            if (item != null)
                UpdateStateInRef(item, etude);

            else if (Game.Instance.Player.EtudesSystem.EtudeIsPreCompleted(blueprintEtude))
                etude.State = EtudeIdReferences.EtudeState.CompleteBeforeActive;
            else if (Game.Instance.Player.EtudesSystem.EtudeIsCompleted(blueprintEtude))
                etude.State = EtudeIdReferences.EtudeState.Completed;
        }

        private void ShowParentTree(EtudeIdReferences etude)
        {
            foreach (var childrenEtude in etude.ChildrenId)
            {
                if (UseFilter && !filteredEtudes.ContainsKey(childrenEtude))
                    continue;
                using (new GUILayout.HorizontalScope())
                {
                    var foldLayoutOptions = new[] {GUILayout.Width(IconW), GUILayout.Height(IconH)};
                    
                    if (loadedEtudes[childrenEtude].ChildrenId.Count != 0)
                    {
                        if (GUILayout.Button("", GUIStyle.none, foldLayoutOptions))
                        {
                            if (Event.current.alt)
                            {
                                OpenCloseAllChildren(loadedEtudes[childrenEtude], !loadedEtudes[childrenEtude].Foldout);
                            }
                            else
                            {
                                loadedEtudes[childrenEtude].Foldout = !loadedEtudes[childrenEtude].Foldout;
                            }
                        }

                        GUI.DrawTexture(
                            GUILayoutUtility.GetLastRect(),
                            loadedEtudes[childrenEtude].Foldout
                                ? foldoutOpened
                                : foldoutClosed,
                            ScaleMode.StretchToFill);
                    }
                    else
                    {
                        GUILayout.Box("", GUIStyle.none, foldLayoutOptions);
                    }

                    DrawEtude(childrenEtude, loadedEtudes[childrenEtude]);
                }

                if ((loadedEtudes[childrenEtude].ChildrenId.Count == 0) || (!loadedEtudes[childrenEtude].Foldout))
                    continue;

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(m_Indent * IndentFactor);

                     using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        ShowParentTree(loadedEtudes[childrenEtude]);
                    }
                }
            }
        }

        private void OpenCloseAllChildren(EtudeIdReferences etude, bool foldoutState)
        {
            etude.Foldout = foldoutState;

            foreach (var cildrenID in etude.ChildrenId)
            {
                loadedEtudes[cildrenID].Foldout = true;
                OpenCloseAllChildren(loadedEtudes[cildrenID], foldoutState);
            }
        }
        
        private void ResizeScrollView()
        {
            Rect previousRect = GUILayoutUtility.GetLastRect();
            cursorChangeRect = new Rect(previousRect.xMax,previousRect.yMin,5f,previousRect.height);

            EditorGUIUtility.AddCursorRect(cursorChangeRect,MouseCursor.ResizeHorizontal);
         
            if( Event.current.type == EventType.MouseDown && cursorChangeRect.Contains(Event.current.mousePosition)){
                resize = true;
            }

            if (Event.current.type == EventType.MouseDrag && resize)
            {
                Vector2 delta = Event.current.delta;
                currentScrollViewWidth = Math.Max(50f, currentScrollViewWidth + delta.x);
                cursorChangeRect.Set(currentScrollViewWidth,cursorChangeRect.y,cursorChangeRect.width,cursorChangeRect.height);

                Event.current.Use();
            }
            
            if(Event.current.type == EventType.MouseUp)
                resize = false;
        }
    }
}
#endif