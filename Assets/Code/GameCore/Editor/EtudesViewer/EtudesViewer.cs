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
using Owlcat.Editor.Utility;
using Owlcat.Editor.Core.Utility;

namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
    public class EtudesViewer : KingmakerWindowBase
    {

        private string parent;
        private string selected;
        private Vector2 m_ScrollPos;
        private Dictionary<string, EtudeIdReferences> loadedEtudes = new Dictionary<string, EtudeIdReferences>();
        private Dictionary<string, EtudeIdReferences> filteredEtudes = new Dictionary<string, EtudeIdReferences>();
        private string rootEtudeId = "4f66e8b792ecfad46ae1d9ecfd7ecbc2";
        private bool UseFilter;
        private bool ShowOnlyTargetAreaEtudes;
        private bool ShowOnlyFlagLikes;

        private BlueprintArea TargetArea;
        public Texture2D commentIcon;
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
            
            commentIcon = EditorGUIUtility.Load("BlueprintIcons/BlueprintDialog.png") as Texture2D;

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
                        etudeChildrenDrawer.DefaultExpandedNodeWidth = EditorGUILayout.Slider(EtudesViewerTexts.NodeExpandWidth,etudeChildrenDrawer.DefaultExpandedNodeWidth,200,2000);
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
                    EditorGUILayout.LabelField($"{EtudesViewerTexts.HierarchyTree} {(loadedEtudes.Count == 0 ? "" : loadedEtudes[parent].Name)}", GUILayout.MinHeight(50));

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

        private void ApplyFilter()
        {
            Dictionary<string, EtudeIdReferences> etudesOfArea = new Dictionary<string, EtudeIdReferences>();

            filteredEtudes = loadedEtudes;

            if (ShowOnlyTargetAreaEtudes && TargetArea!=null)
            {
                etudesOfArea = GetAreaEtudes();
                filteredEtudes = etudesOfArea;
            }

            Dictionary<string, EtudeIdReferences> flaglikeEtudes = new Dictionary<string, EtudeIdReferences>();

            if (ShowOnlyFlagLikes)
            {
                flaglikeEtudes = GetFlaglikeEtudes();
                filteredEtudes = filteredEtudes.Keys.Intersect(flaglikeEtudes.Keys)
                    .ToDictionary(t => t, t => filteredEtudes[t]);
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

        private Dictionary<string, EtudeIdReferences> GetAreaEtudes()
        {
            Dictionary<string, EtudeIdReferences> etudesWithAreaLink = new Dictionary<string, EtudeIdReferences>();

            foreach (var etude in loadedEtudes)
            {
                if (etude.Value.LinkedArea == TargetArea.AssetGuid)
                {
                    if (!etudesWithAreaLink.ContainsKey(etude.Key))
                        etudesWithAreaLink.Add(etude.Key,etude.Value);

                    AddChildsToDictionary(etudesWithAreaLink, etude.Value);
                    AddParentsToDictionary(etudesWithAreaLink, etude.Value);

                }
            }

            return etudesWithAreaLink;
        }

        private void AddChildsToDictionary(Dictionary<string, EtudeIdReferences> dictionary, EtudeIdReferences etude)
        {
            foreach (var children in etude.ChildrenId)
            {
                if (dictionary.ContainsKey(children))
                    continue;

                dictionary.Add(children,loadedEtudes[children]);
                AddChildsToDictionary(dictionary, loadedEtudes[children]);
            }
        }

        private void AddParentsToDictionary(Dictionary<string, EtudeIdReferences> dictionary, EtudeIdReferences etude)
        {
            if (string.IsNullOrEmpty(etude.ParentId))
                return;

            if (dictionary.ContainsKey(etude.ParentId))
                return;

            dictionary.Add(etude.ParentId, loadedEtudes[etude.ParentId]);
            AddParentsToDictionary(dictionary, loadedEtudes[etude.ParentId]);
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
                etudeIdReferences.State = EtudeIdReferences.EtudeState.ComplitionBlocked;
                return;
            }

            if (etude.IsPlaying)
            {
                etudeIdReferences.State = EtudeIdReferences.EtudeState.Active;
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
                    GUILayout.Space(10f);

                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        ShowParentTree(loadedEtudes[rootEtudeId]);
                    }
                }
            }
        }

        private void DrawEtude(string etudeID, EtudeIdReferences etude)
        {
            var style = GUIStyle.none;

            style.fontStyle = FontStyle.Normal;

            if (Application.isPlaying)
            {
                UpdateEtudeState(etudeID, etude);
            }

            GUIContent content = new GUIContent(etude.Name, etude.Comment);

            if (selected == etudeID)
            {
                style.normal.textColor *= 2f;
                GuiScopes.Color(GUI.color * 1.3f);
                style.fontStyle = FontStyle.BoldAndItalic;
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

                Selection.activeObject = BlueprintEditorWrapper.Wrap(ResourcesLibrary.TryGetBlueprint(etudeID)); 
            }

            style.normal.textColor = Color.white;

            switch (etude.State)
            {
                case EtudeIdReferences.EtudeState.Completed:
                {
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), completed, ScaleMode.StretchToFill);
                        break;
                }
                case EtudeIdReferences.EtudeState.Active:
                {
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), active, ScaleMode.StretchToFill);
                        break;
                }
                case EtudeIdReferences.EtudeState.CompleteBeforeActive:
                {
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), completeBeforeActive, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.ComplitionBlocked:
                {
                    style.normal.textColor = Color.black;
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), complitionBlocked, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.Started:
                {
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), started, ScaleMode.StretchToFill);
                        break;
                }
                case EtudeIdReferences.EtudeState.NotStated:
                {
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), notStarted, ScaleMode.StretchToFill);
                        break;
                }
            }

            if (EtudeValidationProblem(etudeID,etude))
            {
                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), validationProblem, ScaleMode.StretchToFill);
            }

            GUI.Label(GUILayoutUtility.GetLastRect(), content, style);

            GuiScopes.Color(Color.white);

            int iconIndex = 1;

            if (!string.IsNullOrEmpty(etude.LinkedArea))
            {
                var styleAreaTexture = OwlcatEditorStyles.Instance.ExtraSignal;

                GUI.Box(new Rect(GUILayoutUtility.GetLastRect().xMax - iconIndex*16, GUILayoutUtility.GetLastRect().yMin, 16, 16), "", styleAreaTexture);
                iconIndex++;
            }

            if (etude.CompleteParent)
            {
                var styleCompletesParent = OwlcatEditorStyles.Instance.RevertButton;

                GUI.Box(new Rect(GUILayoutUtility.GetLastRect().xMax - iconIndex * 16, GUILayoutUtility.GetLastRect().yMin, 16, 16), "", styleCompletesParent);
                iconIndex++;
            }

            if (!string.IsNullOrEmpty(etude.Comment))
            {
                GUI.Box(
                    new Rect(GUILayoutUtility.GetLastRect().xMax - iconIndex * 16, GUILayoutUtility.GetLastRect().yMin,
                        16, 16), commentIcon);
                iconIndex++;
            }

            style.normal.textColor = Color.yellow;
            GuiScopes.Color(Color.yellow);
            GUI.Box(new Rect(GUILayoutUtility.GetLastRect().xMax - iconIndex * 16, GUILayoutUtility.GetLastRect().yMin,
                16, 16), "!");
            iconIndex++;
            style.normal.textColor = Color.black;
            GuiScopes.Color(Color.white);
        }

        private bool EtudeValidationProblem(string etudeID, EtudeIdReferences etude)
        {
            if (!string.IsNullOrEmpty(etude.ChainedTo) && !string.IsNullOrEmpty(etude.LinkedTo))
                return true;

            foreach (var chained in etude.ChainedId)
            {
                if (loadedEtudes[chained].ParentId != etude.ParentId)
                    return true;
                if (loadedEtudes[chained].Id == etude.Id)
                    return true;
            }

            foreach (var linked in etude.LinkedId)
            {
                if (loadedEtudes[linked].ParentId != etude.ParentId && loadedEtudes[linked].ParentId != etudeID)
                    return true;
                if (loadedEtudes[linked].Id == etude.Id)
                    return true;
            }

            return false;
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
                        if (loadedEtudes[childrenEtude].ChildrenId.Count != 0)
                        {
                            if (GUILayout.Button("", GUIStyle.none, GUILayout.MinWidth(15), GUILayout.MinHeight(15), GUILayout.MaxWidth(15)))
                            {
                                loadedEtudes[childrenEtude].Foldout = !loadedEtudes[childrenEtude].Foldout;
                            }

                            if (loadedEtudes[childrenEtude].Foldout)
                            {
                                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), foldoutOpened, ScaleMode.StretchToFill);
                            }
                            else
                            {
                                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), foldoutClosed, ScaleMode.StretchToFill);
                            }

                            GUILayout.Space(10f);

                            if (GUILayout.Button("", GUIStyle.none, GUILayout.MinWidth(15), GUILayout.MinHeight(15), GUILayout.MaxWidth(15)))
                            {
                                OpenCloseAllChildren(loadedEtudes[childrenEtude], !loadedEtudes[childrenEtude].Foldout);
                            }

                            if (loadedEtudes[childrenEtude].Foldout)
                            {
                                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), foldoutOpenedAll, ScaleMode.StretchToFill);
                            }
                            else
                            {
                                GUI.DrawTexture(GUILayoutUtility.GetLastRect(), foldoutClosedAll, ScaleMode.StretchToFill);
                            }
                        }

                        DrawEtude(childrenEtude, loadedEtudes[childrenEtude]);
                    }

                    if ((loadedEtudes[childrenEtude].ChildrenId.Count == 0) || (!loadedEtudes[childrenEtude].Foldout))
                        continue;

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(60f);

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