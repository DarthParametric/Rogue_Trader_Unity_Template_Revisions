

using Object = UnityEngine.Object;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Blueprints.Creation;
using Owlcat.Editor.Core.Utility;
#if UNITY_EDITOR && EDITOR_FIELDS
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Editor.EtudesViewer;
using Kingmaker.Editor.Validation;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
    public class EtudeChildrenDrawer
    {
        private Dictionary<string, EtudeIdReferences> loadedEtudes = new Dictionary<string, EtudeIdReferences>();
        private Dictionary<string, EtudeDrawerData> etudeDrawerData = new Dictionary<string, EtudeDrawerData>();
        private string parentEtude;
        private float chainedShift = 40;
        private float linkedShift = 20;
        private float verticalShift = 20;
        private float lastRectMaxY;
        private Rect workspaceRect;
        private int maxDepthToDefaultShow = 0;
        private bool FirstLayoutProcess;
        public float DefaultExpandedNodeWidth = 600;

        public ReferenceGraph ReferenceGraph;
        private ReferenceGraph.Entry selectedEntry;
        private List<ReferenceGraph.Ref> startReferences = new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> completeReferences= new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> checkReferences= new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> synchronizedReferences= new List<ReferenceGraph.Ref>();
        private List<ReferenceGraph.Ref> otherReferences= new List<ReferenceGraph.Ref>();
        private List<string> conflictingGroupReferences= new List<string>();
        private bool startFoldout = false;
        private bool completeFoldout = false;
        private bool checkFoldout = false;
        private bool synchronizedFoldout = false;
        private bool otherFoldout = false;
        private bool conflictingGroupFoldout = false;

        private string oldFind = "";
        private Dictionary<string, EtudeIdReferences> foundedEtudes = new Dictionary<string, EtudeIdReferences>();

        public static bool newParentFromContestComand = false;
        public static string newParentID;
        

        public BlockersInfo BlockersInfo { get; } = new BlockersInfo();
        public static event Action<BlueprintScriptableObject, Rect> EtudeViewerWindowItemOnGUI;

        private string SelectedId = "";
        private string SelectedName = "";
        
        //zoom
        private const float kZoomMin = 0.1f;
        private const float kZoomMax = 10.0f;
        
        private float _zoom = 1.0f;
        private Vector2 _zoomCoordsOrigin = Vector2.zero;
        private EtudesViewer etudeViewer;

        private EtudeChildrenDrawer()
        {
        }

        public EtudeChildrenDrawer(Dictionary<string, EtudeIdReferences> etudes, EtudesViewer viewer)
        {
            loadedEtudes = etudes;
            etudeViewer = viewer;
            BlueprintScriptableObject.ChangedEvent += OnBlueprintChanged;
        }
        

        public void SetWorkspaceRect(Rect rect)
        {
            workspaceRect = rect;
        }

        public void SetParent(string parent, Rect rect)
        {
            parentEtude = parent;
            etudeDrawerData = new Dictionary<string, EtudeDrawerData>();
            workspaceRect = rect;
            _zoomCoordsOrigin = Vector2.zero;
            FirstLayoutProcess = true;
        }

        public static void TryToSetParent(string parent)
        {
            newParentFromContestComand = true;
            newParentID = parent;
        }
        

        public void Update()
        {
            BlueprintEtude oldSelectedEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(SelectedId);
            if (oldSelectedEtude == null)
            {
                EtudesTreeLoader.Instance.RemoveEtudeData(SelectedId);
                return;
            }

            if (oldSelectedEtude.AssetName != SelectedName)
                EtudesTreeLoader.Instance.UpdateEtude(oldSelectedEtude);
        }

        public void OnGUI()
        {
            if (string.IsNullOrEmpty(parentEtude))
                return;

            HandleEvents();

            EditorGUILayout.LabelField($"Дочернее дерево этюда: {loadedEtudes[parentEtude].Name}");

            GUI.DrawTextureWithTexCoords(workspaceRect, etudeViewer.grid,
                new Rect(_zoomCoordsOrigin.x / 30, -_zoomCoordsOrigin.y / 30, workspaceRect.width / (30 * _zoom),
                    workspaceRect.height / (30 * _zoom)));

            EditorZoomArea.Begin(_zoom, workspaceRect);
            GUILayout.BeginArea(new Rect(10.0f - _zoomCoordsOrigin.x, 10.0f - _zoomCoordsOrigin.y, 10000, 10000));
            PrepareLayout();
            DrawSelection();
            DrawLines();
            DrawEtudes();
            DrawReferences();
            DrawFind();
            GUILayout.EndArea();
            EditorZoomArea.End();

            if (newParentFromContestComand)
            {
                if (loadedEtudes.ContainsKey(newParentID))
                {
                    BlueprintEtude clickedEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(newParentID);
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(clickedEtude);

                    if (clickedEtude.Parent.IsEmpty())
                    {
                        parentEtude = clickedEtude.AssetGuid;
                    }
                    else
                    {
                        parentEtude = clickedEtude.Parent.GetBlueprint().AssetGuid;
                    }
                    
                    etudeDrawerData = new Dictionary<string, EtudeDrawerData>();
                    _zoomCoordsOrigin = Vector2.zero;
                    FirstLayoutProcess = true;
                }
                
                newParentFromContestComand = false;
            }
        }

        private void PrepareLayout()
        {
            lastRectMaxY = workspaceRect.y;

            foreach (var etude in etudeDrawerData)
            {
                etude.Value.NeedToPaint = false;
            }

            FindRectForNextEtude(parentEtude,loadedEtudes[parentEtude],true);
            LayoutChildren(parentEtude);
            if(FirstLayoutProcess)
            {
                ShowOnkyFewEtudes(40);
                FirstLayoutProcess = false;
            }
        }
        
        private void OnBlueprintChanged(BlueprintScriptableObject blueprint)
        {
            BlueprintEtude etude = blueprint as BlueprintEtude;
            
            if (etude == null)
                return;
                
            EtudesTreeLoader.Instance.UpdateEtude(etude);
        }

        private void ShowOnkyFewEtudes(int maxEtudes)
        {
            int depth01Count = etudeDrawerData.Count(kvp => (kvp.Value.Depth == 1));
            int depth02Count = etudeDrawerData.Count(kvp => (kvp.Value.Depth == 2));
            int depth03Count = etudeDrawerData.Count(kvp => (kvp.Value.Depth == 3));
            int depth04Count = etudeDrawerData.Count(kvp => (kvp.Value.Depth == 4));
            int depth05Count = etudeDrawerData.Count(kvp => (kvp.Value.Depth == 5));

            if(   depth01Count<=maxEtudes  ) 
                maxDepthToDefaultShow = 1;
            if(   (depth01Count+depth02Count)<=maxEtudes  ) 
                maxDepthToDefaultShow = 2;
            if(   (depth01Count+depth02Count+depth03Count)<=maxEtudes  ) 
                maxDepthToDefaultShow = 3;
            if(   (depth01Count+depth02Count+depth03Count+depth04Count)<=maxEtudes  ) 
                maxDepthToDefaultShow = 4;
            if(   (depth01Count+depth02Count+depth03Count+depth04Count+depth05Count)<=maxEtudes  ) 
                maxDepthToDefaultShow = 5;

            foreach (var etudeDrower in etudeDrawerData)
            {
                if (etudeDrower.Value.Depth>=maxDepthToDefaultShow)
                    etudeDrower.Value.ShowChildren = false;

                if (etudeDrower.Value.Depth>maxDepthToDefaultShow)
                    etudeDrower.Value.NeedToPaint = false;

            }
        }

        private void DrawEtudes()
        {
            foreach (var drawerData in etudeDrawerData)
            {
                if(drawerData.Value.NeedToPaint)
                    DrawEtude(drawerData.Key,loadedEtudes[drawerData.Key], drawerData.Value);
            }
        }

        private void DrawSelection()
        {
            BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);

            if (blueprint == null)
                return;

            if (!loadedEtudes.ContainsKey(blueprint.AssetGuid))
                EtudesTreeLoader.Instance.UpdateEtude(blueprint);

            if (blueprint.AssetGuid!=SelectedId || blueprint.AssetName!=SelectedName)
            {
                BlueprintEtude oldSelectedEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(SelectedId);
                if (oldSelectedEtude!=null)
                {
                    EtudesTreeLoader.Instance.UpdateEtude(oldSelectedEtude);
                }
                else
                {
                    EtudesTreeLoader.Instance.RemoveEtudeData(SelectedId);
                }
                
                EtudesTreeLoader.Instance.UpdateEtude(blueprint);
                SelectedId = blueprint.AssetGuid;
                SelectedName = blueprint.AssetName;
            }
                
            DrawSelectionFrame(blueprint.AssetGuid,Color.green,Color.blue, 10f);
            
        }

        private void DrawFind()
        {
            if (etudeViewer.Find == "")
                return;

            foreach (var etude in foundedEtudes)
            {
                DrawSelectionFrame(etude.Key,Color.magenta,Color.magenta, 5f );
            }

            if (etudeViewer.Find == oldFind)
                return;

            oldFind = etudeViewer.Find;
            foundedEtudes = loadedEtudes.Where(e => e.Value.Name.IndexOf(etudeViewer.Find, StringComparison.OrdinalIgnoreCase) >= 0).ToDictionary(item => item.Key, item => item.Value);
        }
        

        public void UpdateBlockersInfo()
        {
            BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);
            if (blueprint != null)
            {
                UpdateBlockersInfo(blueprint);
            }
            else
            {
                BlockersInfo.Clear();
            }
        }

        private void DrawSelectionFrame(string selectedEtude,Color color, Color parentColor, float width)
        {
            if (etudeDrawerData.ContainsKey(selectedEtude) && etudeDrawerData[selectedEtude].NeedToPaint)
            {
                EtudeDrawerData drawerData = etudeDrawerData[selectedEtude];

                Vector3 p1 = new Vector3(drawerData.EtudeRect.xMin, drawerData.EtudeRect.yMin);
                Vector3 p2 = new Vector3(drawerData.EtudeRect.xMax, drawerData.EtudeRect.yMin);
                Vector3 p3 = new Vector3(drawerData.EtudeRect.xMax, drawerData.EtudeRect.yMax);
                Vector3 p4 = new Vector3(drawerData.EtudeRect.xMin, drawerData.EtudeRect.yMax);

                Handles.color = color;
                Handles.DrawAAPolyLine(width, p1, p2, p3, p4, p1);
                
                if (!string.IsNullOrEmpty(loadedEtudes[selectedEtude].ParentId) && etudeDrawerData.ContainsKey(loadedEtudes[selectedEtude].ParentId))
                {
                    EtudeDrawerData parentDrawerData = etudeDrawerData[loadedEtudes[selectedEtude].ParentId];
                    Vector3 p5 = new Vector3(drawerData.EtudeRect.xMin, drawerData.EtudeRect.yMin);
                    Vector3 p6 = new Vector3(parentDrawerData.EtudeRect.xMax, parentDrawerData.EtudeRect.yMax);
                    Handles.DrawAAPolyLine(width, p5, p6);
                }
                
            }

            if (!string.IsNullOrEmpty(loadedEtudes[selectedEtude].ParentId))
            {
                DrawSelectionFrame(loadedEtudes[selectedEtude].ParentId,parentColor ,parentColor, width);
            }
            
            
        }

        private void DrawLines()
        {
            foreach (var drawerData in etudeDrawerData)
            {
                if(!drawerData.Value.NeedToPaint)
                    continue;

                if (loadedEtudes.ContainsKey(drawerData.Key) && loadedEtudes[drawerData.Key].LinkedId.Count > 0)
                {
                    foreach (var linkedEtude in loadedEtudes[drawerData.Key].LinkedId)
                    {
                        if (!etudeDrawerData.ContainsKey(linkedEtude))
                            continue;

                        if (!etudeDrawerData[linkedEtude].NeedToPaint)
                            continue;

                        Handles.color = Color.yellow;
                        
                        if (loadedEtudes[linkedEtude].ParentId == drawerData.Key)
                        {
                            Vector2 p1v2 = etudeDrawerData[linkedEtude].LeftEnterPoint - chainedShift * Vector2.right;
                            Vector3 p1 = new Vector3(p1v2.x,p1v2.y);
                            Vector3 p2 = new Vector3(etudeDrawerData[linkedEtude].LeftEnterPoint.x,etudeDrawerData[linkedEtude].LeftEnterPoint.y);
                            
                            Handles.DrawAAPolyLine(5.0f, p1, p2);
                        }
                        else
                        {
                            Vector3 p1 = new Vector3(drawerData.Value.LinkedStartPoint.x,drawerData.Value.LinkedStartPoint.y);
                            Vector2 p2v2 = etudeDrawerData[linkedEtude].LeftEnterPoint - 0.5f * linkedShift * Vector2.right;
                            Vector3 p2 = new Vector3(p2v2.x,p2v2.y);
                            Vector3 p3 = new Vector3(etudeDrawerData[linkedEtude].LeftEnterPoint.x,etudeDrawerData[linkedEtude].LeftEnterPoint.y);

                            Handles.DrawAAPolyLine(5.0f, p1, p2, p3);
                        }
                    }
                }

                if (drawerData.Value.ShowChildren && drawerData.Value.ChainStarts.Count > 0)
                {
                    Vector3 p0 = new Vector3(drawerData.Value.RightExitPoint.x,drawerData.Value.RightExitPoint.y);
                    Vector2 p1v2 = etudeDrawerData[drawerData.Value.ChainStarts.First().Key].LeftEnterPoint - chainedShift * Vector2.right;
                    Vector3 p1 = new Vector3(p1v2.x,p1v2.y);

                    Vector3 p2;
                    if (drawerData.Value.ChainStarts.First().Value == drawerData.Value.ChainStarts.Last().Value)
                    {
                        p2 = new Vector3(p1.x,p1.y+verticalShift/2);
                    }
                    else
                    {
                        Vector2 p2v2 = etudeDrawerData[drawerData.Value.ChainStarts.Last().Key].LeftEnterPoint - chainedShift * Vector2.right;
                        p2 = new Vector3(p2v2.x,p2v2.y);
                    }
                    
                    Handles.color = Color.black;
                    Handles.DrawAAPolyLine(10.0f, p0, p1, p2);
                }

                if ((loadedEtudes[drawerData.Key].ChainedId.Count > 0) && (etudeDrawerData.ContainsKey(loadedEtudes[drawerData.Key].ChainedId.Last())))
                {
                    Vector3 p1 = new Vector3(drawerData.Value.RightExitPoint.x,drawerData.Value.RightExitPoint.y);
                    Vector2 p2v2 = drawerData.Value.RightExitPoint + 0.5f * chainedShift * Vector2.right;
                    Vector3 p2 = new Vector3(p2v2.x,p2v2.y);
                    Vector2 p3v2 = etudeDrawerData[loadedEtudes[drawerData.Key].ChainedId.Last()].LeftEnterPoint - 0.5f *chainedShift * Vector2.right;
                    Vector3 p3 = new Vector3(p3v2.x,p3v2.y);
                    
                    Handles.color = Color.green;

                    if (loadedEtudes[drawerData.Key].ChainedId.Count == 1 && drawerData.Value.NeedToPaint)
                    {
                        Handles.DrawAAPolyLine(5.0f, p1, p2);
                    }
                    else
                    {
                        if(drawerData.Value.NeedToPaint)
                            Handles.DrawAAPolyLine(5.0f, p1, p2, p3);
                    }

                    foreach (var chainedEtude in loadedEtudes[drawerData.Key].ChainedId)
                    {
                        if (!etudeDrawerData.ContainsKey(chainedEtude))
                            continue;

                        if (!etudeDrawerData[chainedEtude].NeedToPaint)
                            continue;

                        
                        Vector2 p4v2 = etudeDrawerData[chainedEtude].LeftEnterPoint - 0.5f *chainedShift * Vector2.right;
                        Vector3 p4 = new Vector3(p4v2.x,p4v2.y);
                        Vector3 p5 = new Vector3(etudeDrawerData[chainedEtude].LeftEnterPoint.x,etudeDrawerData[chainedEtude].LeftEnterPoint.y);
                        
                        Handles.DrawAAPolyLine(5.0f, p4, p5);
                    }
                }

            }
        }

        private void LayoutChildren(string etudeID)
        {
            EtudeIdReferences localParent = loadedEtudes[etudeID];
            if (localParent == null)
                return;

            Dictionary<string, EtudeIdReferences> chainStarts = new Dictionary<string, EtudeIdReferences>();

            foreach (var childID in localParent.ChildrenId)
            {
                EtudeIdReferences child = loadedEtudes[childID];

                if (string.IsNullOrEmpty(child.ChainedTo) && (string.IsNullOrEmpty(child.LinkedTo) || (child.LinkedTo == etudeID)))
                {
                    chainStarts.Add(childID,child);
                }
            }

            if (etudeDrawerData.ContainsKey(etudeID))
            {
                etudeDrawerData[etudeID].ChainStarts = chainStarts;
            }

            foreach (var chainStartID in chainStarts)
            {
                FindRectForChain(chainStartID.Key, chainStartID.Value, etudeDrawerData[etudeID].Depth+1);
            }
        }

        private void FindRectForChain(string etudeID, EtudeIdReferences etude, int depth = 0)
        {


            FindRectForNextEtude(etudeID, etude , string.IsNullOrEmpty(etude.LinkedArea),depth);

            foreach (var chainedID in etude.ChainedId)
            {
                FindRectForChain(chainedID, loadedEtudes[chainedID], depth);
            }
            
            if (etudeDrawerData[etudeID].ShowChildren)
            {
                LayoutChildren(etudeID);
            }

            foreach (var linkedID in etude.LinkedId)
            {
                if (loadedEtudes[linkedID].ParentId == etudeID)
                    continue;
                
                FindRectForChain(linkedID, loadedEtudes[linkedID], depth);
            }
        }

        private void DrawEtude(string etudeID, EtudeIdReferences etude, EtudeDrawerData drawerData)
        {
            if (Application.isPlaying)
            {
                etudeViewer.UpdateEtudeState(etudeID, etude);
            }

            var style = GUIStyle.none;
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;

            var styleAreaTexture = OwlcatEditorStyles.Instance.ExtraSignal;

            if (GUI.Button(drawerData.EtudeButtonRect, "", GUIStyle.none))
            {
                BlueprintEtude clickedEtude = (BlueprintEtude)ResourcesLibrary.TryGetBlueprint(etudeID);

                if (clickedEtude == null)
                {
                    EtudesTreeLoader.Instance.RemoveEtudeData(etudeID);
                }
                else
                {
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(clickedEtude); 
                }

                if (Event.current.button == 1)
                {
                    OnRightClick();
                }
            }

            if (Event.current.type != EventType.Layout &&
                drawerData.EtudeRect.Contains(Event.current.mousePosition))
            {
                EtudeViewerWindowItemOnGUI?.Invoke((BlueprintEtude)ResourcesLibrary.TryGetBlueprint(etudeID),
                    drawerData.EtudeRect);
            }

            DrawEtudeColor(etude,drawerData.EtudeRect);

            EditorGUI.LabelField( new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y,drawerData.EtudeRect.width,drawerData.EtudeRect.height), etude.Name, style);

            if (!string.IsNullOrEmpty(etude.Comment))
            {
                var commentStyle = GUIStyle.none;
                commentStyle.normal.textColor = Color.magenta;
                commentStyle.fontSize = 8;
                commentStyle.wordWrap = true;
                commentStyle.clipping = TextClipping.Clip;
                Rect commentRect = new Rect(drawerData.EtudeRect.x + 5, drawerData.EtudeRect.y + 30,
                    drawerData.EtudeRect.width - 5, 40);
                GUI.DrawTexture(commentRect, etudeViewer.notStarted, ScaleMode.StretchToFill);
                EditorGUI.TextArea( commentRect, etude.Comment, commentStyle);
            }
            
            style = GUIStyle.none;
            style.normal.textColor = Color.white;
            style.fontSize = 12;
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;
            
            int iconIndex = 1;

            if (!string.IsNullOrEmpty(etude.LinkedArea))
            {
                GUI.Box(new Rect(drawerData.EtudeRect.xMax - iconIndex*16, drawerData.EtudeRect.yMax -16, 16, 16), "", styleAreaTexture);
                iconIndex++;
            }

            if (etude.CompleteParent)
            {
                var styleCompletesParent = OwlcatEditorStyles.Instance.RevertButton;

                GUI.Box(new Rect(drawerData.EtudeRect.xMax - iconIndex * 16, drawerData.EtudeRect.yMax -16, 16, 16), "", styleCompletesParent);
                iconIndex++;
            }

            style.normal.textColor = Color.yellow;
            GuiScopes.Color(Color.yellow);
            GUI.Box(new Rect(drawerData.EtudeRect.xMax - iconIndex * 16, drawerData.EtudeRect.yMax - 16,
                16, 16), "A");
            iconIndex++;
            style.normal.textColor = Color.black;
            GuiScopes.Color(Color.white);


            GUI.Box(new Rect(drawerData.LeftEnterPoint.x-4,drawerData.LeftEnterPoint.y-4 , 8, 8), "", styleAreaTexture);
            
            if (etude.LinkedId.Count > 0)
            {
                GUI.Box(new Rect(drawerData.LinkedStartPoint.x-4,drawerData.LinkedStartPoint.y-4 , 8, 8), "", styleAreaTexture);
            }

            if(etude.ChildrenId.Count > 0)
            {
                if (GUI.Button(new Rect(drawerData.RightExitPoint.x-5,drawerData.RightExitPoint.y+5 , 40, 20),drawerData.ShowChildren?"-":$"+ {(etude.ChildrenId.Count)}"))
                {
                    drawerData.ShowChildren = ! drawerData.ShowChildren;
                }
            }
            
            if (selectedEntry?.ObjectGuid == etudeID)
            {
                if (GUI.Button(new Rect(drawerData.EtudeRect.xMax - 5, drawerData.EtudeRect.yMin - 5, 40, 20), "><"))
                {
                    selectedEntry = null;
                }
            }
            else
            {
                if (GUI.Button(new Rect(drawerData.EtudeRect.xMax - 5, drawerData.EtudeRect.yMin - 5, 40, 20), "<>"))
                {
                    selectedEntry = ReferenceGraph.Entries.Find(e => e.ObjectGuid == etudeID);

                    if (selectedEntry != null)
                    {
                        startFoldout = false;
                        checkFoldout = false;
                        completeFoldout = false;
                        synchronizedFoldout = false;
                        conflictingGroupFoldout = false;
                        
                        startReferences = 
                            selectedEntry.References.Where(r
                                => ((EtudeReferenceType)r.ReferenceTypeMask).HasFlag(EtudeReferenceType.Start)).ToList();
                    
                        completeReferences =
                            selectedEntry.References.Where(r
                                => ((EtudeReferenceType)r.ReferenceTypeMask).HasFlag(EtudeReferenceType.Complete)).ToList();
                    
                        checkReferences = 
                            selectedEntry.References.Where(r
                                => ((EtudeReferenceType)r.ReferenceTypeMask).HasFlag(EtudeReferenceType.Check)).ToList();
                    
                        synchronizedReferences = 
                            selectedEntry.References.Where(r
                                => ((EtudeReferenceType)r.ReferenceTypeMask).HasFlag(EtudeReferenceType.Synchronized)).ToList();
                        
                        otherReferences = 
                            selectedEntry.References.Where(r
                                => (EtudeReferenceType)r.ReferenceTypeMask == EtudeReferenceType.None).ToList();

                        conflictingGroupReferences = GetConflictingEtudes(etudeID);

                    }
                }
            }
        }

        public void DrawEtudeColor(EtudeIdReferences etude, Rect rect)
        {
            switch (etude.State)
            {
                case EtudeIdReferences.EtudeState.NotStarted:
                {
                    GUI.DrawTexture(rect, etudeViewer.notStarted, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.Started:
                {
                    GUI.DrawTexture(rect, etudeViewer.started, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.Playing:
                {
                    GUI.DrawTexture(rect, etudeViewer.active, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.CompleteBeforeActive:
                {
                    GUI.DrawTexture(rect, etudeViewer.completed, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.CompletionInProgress:
                {
                    GUI.DrawTexture(rect, etudeViewer.complitionBlocked, ScaleMode.StretchToFill);
                    break;
                }
                case EtudeIdReferences.EtudeState.Completed:
                {
                    GUI.DrawTexture(rect, etudeViewer.completed, ScaleMode.StretchToFill);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private List<string> GetConflictingEtudes(string etudeID)
        {
            List<string> result = new List<string>();
            
            foreach (var conflictingGroup in loadedEtudes[etudeID].ConflictingGroups)
            {
                foreach (var etude in EtudesTreeLoader.Instance.ConflictingGroups[conflictingGroup].Etudes)
                {
                    if (result.Contains(etude))
                        continue;
                    result.Add(etude);
                }
            }
            
            result = result.OrderBy(e => loadedEtudes[e].Priority).ToList();

            return result;
        }
        
        private void OnRightClick()
        {
            
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Создать дочерний этюд"), false, OnAddEtudeClick);
            menu.AddItem(new GUIContent("Построить дерево"), false, OnSetRootClick);
            menu.AddItem(new GUIContent("На уровень выше"), false, OnSetParentAsRootClick);
            menu.ShowAsContext();
        }
        
        private void OnAddEtudeClick()
        {
            BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);

            if (blueprint == null)
                return;

            var creator = ScriptableObject.CreateInstance<BlueprintEtudeCreator>();

            if (blueprint.Parent != null)
            {
                creator.Parent = blueprint.ToReference<BlueprintEtudeReference>();
            }
            
            NewAssetWindow.ShowWindow(creator);
        }
        
        private void OnSetRootClick()
        {
            BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);

            if(blueprint != null)
            {
                SetParent(blueprint.AssetGuid,workspaceRect);
            }
        }
        
        private void OnSetParentAsRootClick()
        {
            BlueprintEtude blueprint = BlueprintEditorWrapper.Unwrap<BlueprintEtude>(Selection.activeObject);

            if (blueprint != null && !string.IsNullOrEmpty(loadedEtudes[blueprint.AssetGuid].ParentId))
            {
                SetParent(loadedEtudes[blueprint.AssetGuid].ParentId,workspaceRect);
            }
        }
        
        private void FindRectForNextEtude(string etudeID, EtudeIdReferences etude, bool showChildren = false, int depth = 0)
        {
            EtudeDrawerData newData = new EtudeDrawerData();

            if (!etudeDrawerData.ContainsKey(etudeID))
            {
                etudeDrawerData.Add(etudeID,newData);
                etudeDrawerData[etudeID].ShowChildren = showChildren;
            }

            etudeDrawerData[etudeID].NeedToPaint = true;

            float x = 0;
            float y = 0;

            float etudeSizeY = 36f;
            float etudeSizeX = 200f;

            if (!string.IsNullOrEmpty(etude.Comment))
            {
                etudeSizeY += 50f;
            }

            if (!string.IsNullOrEmpty(etude.ChainedTo) && etudeDrawerData.ContainsKey(etude.ChainedTo))
            {
                if(loadedEtudes[etude.ChainedTo].ChainedId.IndexOf(etudeID) == 0)
                {
                    y = etudeDrawerData[etude.ChainedTo].EtudeRect.y;

                }
                else
                {
                    y = lastRectMaxY + etudeSizeY + verticalShift;
                }

                x = etudeDrawerData[etude.ChainedTo].EtudeRect.xMax + chainedShift;
            }
            else
            {
                if (!string.IsNullOrEmpty(etude.LinkedTo) && etudeDrawerData.ContainsKey(etude.LinkedTo) && (etude.LinkedTo != etude.ParentId))
                {
                    y = lastRectMaxY + etudeDrawerData[etude.LinkedTo].EtudeRect.height + verticalShift;
                    x = etudeDrawerData[etude.LinkedTo].EtudeRect.x + linkedShift;
                }
                else
                {
                    if (!string.IsNullOrEmpty(etude.ParentId))
                    {
                        if (etudeDrawerData.ContainsKey(etude.ParentId))
                        {
                            y = lastRectMaxY + verticalShift;
                            x = etudeDrawerData[etude.ParentId].EtudeRect.xMax + chainedShift * 2f;
                        }
                        else
                        {
                            y = lastRectMaxY + verticalShift;
                            x = chainedShift;
                        }
                    }
                }
            }
            
            etudeDrawerData[etudeID].EtudeButtonRect = new Rect(x,y,etudeSizeX,etudeSizeY);
            
            if (selectedEntry?.ObjectGuid == etudeID)
            {
                etudeSizeX = DefaultExpandedNodeWidth;
                etudeSizeY += FindRectsForReferences(etudeDrawerData[etudeID], etudeSizeX, etudeSizeY);
                
            }

            etudeDrawerData[etudeID].EtudeRect = new Rect(x,y,etudeSizeX,etudeSizeY);
            etudeDrawerData[etudeID].LeftEnterPoint = new Vector2(x,y+18);
            etudeDrawerData[etudeID].RightExitPoint = new Vector2(x+etudeSizeX,y+18);
            etudeDrawerData[etudeID].LinkedStartPoint = new Vector2(x+linkedShift*0.5f,y+etudeSizeY);
            etudeDrawerData[etudeID].Depth = depth;

            lastRectMaxY = Mathf.Max(lastRectMaxY,y + etudeSizeY);
        }

        private float FindRectsForReferences(EtudeDrawerData drawerData, float sizeX, float sizeY)
        {
            float result = 0f;
            
            drawerData.StartedReferencesLabelRect = new Rect(drawerData.EtudeRect.x, drawerData.EtudeRect.y + sizeY, sizeX, 20);
            result += 20f;
            
            drawerData.StartedRects = new List<Rect>();

            if (startFoldout)
            {
                for (int i = 0; i < startReferences.Count; i++)
                {
                    drawerData.StartedRects.Add(new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y + sizeY + result,sizeX,40));
                    result += 40f;
                }
            }

            drawerData.CheckedReferencesLabelRect = new Rect(drawerData.EtudeRect.x, drawerData.EtudeRect.y + sizeY + result, sizeX, 20);
            result += 20f;
            
            drawerData.CheckedRects = new List<Rect>();

            if (checkFoldout)
            {
                for (int i = 0; i < checkReferences.Count; i++)
                {
                    drawerData.CheckedRects.Add(new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y + sizeY + result,sizeX,40));
                    result += 40f;
                }
            }

            drawerData.CompletedReferencesLabelRect = new Rect(drawerData.EtudeRect.x, drawerData.EtudeRect.y + sizeY + result, sizeX, 20);
            result += 20f;
            
            drawerData.CompletedRects = new List<Rect>();

            if (completeFoldout)
            {
                for (int i = 0; i < completeReferences.Count; i++)
                {
                    drawerData.CompletedRects.Add(new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y + sizeY + result,sizeX,40));
                    result += 40f;
                }
            }

            drawerData.SynchronizedReferencesLabelRect = new Rect(drawerData.EtudeRect.x, drawerData.EtudeRect.y + sizeY + result, sizeX, 20);
            result += 20f;
            
            drawerData.SynchronizedRects = new List<Rect>();

            if (synchronizedFoldout)
            {
                for (int i = 0; i < synchronizedReferences.Count; i++)
                {
                    drawerData.SynchronizedRects.Add(new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y + sizeY + result,sizeX,40));
                    result +=40f;
                }
            }
            
            drawerData.OtherReferencesLabelRect = new Rect(drawerData.EtudeRect.x, drawerData.EtudeRect.y + sizeY + result, sizeX, 20);
            result += 20f;
            
            drawerData.OtherRects = new List<Rect>();

            if (otherFoldout)
            {
                for (int i = 0; i < otherReferences.Count; i++)
                {
                    drawerData.OtherRects.Add(new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y + sizeY + result,sizeX,40));
                    result +=40f;
                }
            }

            drawerData.ConflictingGroupsLabelRect = new Rect(drawerData.EtudeRect.x, drawerData.EtudeRect.y + sizeY + result, sizeX, 20);
            result += 20f;
            
            drawerData.ConflictingGroupsRects = new List<Rect>();

            if (conflictingGroupFoldout)
            {
                for (int i = 0; i < conflictingGroupReferences.Count; i++)
                {
                    drawerData.ConflictingGroupsRects.Add(new Rect(drawerData.EtudeRect.x,drawerData.EtudeRect.y + sizeY + result,sizeX,60));
                    result +=60f;
                }
            }

            return result;
        }

        void UpdateBlockersInfo(BlueprintEtude etude)
        {
            if (loadedEtudes.TryGetValue(etude.AssetGuid, out var etudeRef)
                && etudeRef.State == EtudeIdReferences.EtudeState.CompletionInProgress)
            {
                var item = Game.Instance.Player.EtudesSystem.Etudes.Get(etude);
                if (item != null)
                {
                    var checkedEtudes = new HashSet<Etude>();
                    var selfBlockers = new HashSet<Etude>();
                    FillBlockers(item, checkedEtudes, selfBlockers);
                    BlockersInfo.Owner = item;
                    BlockersInfo.Blockers = selfBlockers;
                }
            }
            else
            {
                BlockersInfo.Clear();
            }
        }

        void FillBlockers(Etude etude, HashSet<Etude> checkedEtudes, HashSet<Etude> blockers)
        {
            if (etude.IsCompleted || checkedEtudes.Contains(etude))
                return;

            checkedEtudes.Add(etude);
            if (etude.ComplitionBlockers.Count == 0)
            {
                blockers.Add(etude);
            }
            else
            {
                foreach (var child in etude.ComplitionBlockers)
                {
                    FillBlockers(child, checkedEtudes, blockers);
                }
            }
        }
        
        private Object LoadReference(ReferenceGraph.Ref r)
        {
            if (r.AssetPath.StartsWith("Assets"))
            {
                return AssetDatabase.LoadAssetAtPath<Object>(r.AssetPath);
            }

            return BlueprintEditorWrapper.Wrap(BlueprintsDatabase.LoadAtPath<SimpleBlueprint>(r.AssetPath));
        }

        private void DrawReferences()
        {
            if (selectedEntry == null || !etudeDrawerData.ContainsKey(selectedEntry.ObjectGuid))
            {
                return;
            }

            EtudeDrawerData drawerData = etudeDrawerData[selectedEntry.ObjectGuid];

            startFoldout = EditorGUI.Foldout(drawerData.StartedReferencesLabelRect, startFoldout, $"Стартует из:({startReferences.Count})",EditorStyles.foldout);

            if (startFoldout && drawerData.StartedRects.Count == startReferences.Count)
            {

                for (int i = 0; i < startReferences.Count; i++)
                {
                    if (GUI.Button(drawerData.StartedRects[i],
                        $"{startReferences[i].ReferencingObjectName} \nin {startReferences[i].AssetPath.Replace("Assets/Mechanics/Blueprints", "")}",
                        EditorStyles.miniLabel))
                    {
                        Selection.activeObject = LoadReference(startReferences[i]);
                    }
                }
            }

            checkFoldout = EditorGUI.Foldout(drawerData.CheckedReferencesLabelRect,checkFoldout, $"Проверяется в:({checkReferences.Count})",EditorStyles.foldout);

            if (checkFoldout && drawerData.CheckedRects.Count == checkReferences.Count)
            {
                for (int i = 0; i < checkReferences.Count; i++)
                {
                    if (GUI.Button(drawerData.CheckedRects[i], $"{checkReferences[i].ReferencingObjectName} \nin {checkReferences[i].AssetPath.Replace("Assets/Mechanics/Blueprints","")}", EditorStyles.miniLabel))
                    {
                        Selection.activeObject = LoadReference(checkReferences[i]);
                    }
                }
            }

            completeFoldout = EditorGUI.Foldout(drawerData.CompletedReferencesLabelRect, completeFoldout, $"Комплитится в:({completeReferences.Count})",EditorStyles.foldout);

            if (completeFoldout && drawerData.CompletedRects.Count == completeReferences.Count)
            {
                for (int i = 0; i < completeReferences.Count; i++)
                {
                    if (GUI.Button(drawerData.CompletedRects[i], $"{completeReferences[i].ReferencingObjectName} \nin {completeReferences[i].AssetPath.Replace("Assets/Mechanics/Blueprints","")}", EditorStyles.miniLabel))
                    {
                        Selection.activeObject = LoadReference(completeReferences[i]);
                    }
                }
            }

            synchronizedFoldout = EditorGUI.Foldout(drawerData.SynchronizedReferencesLabelRect, synchronizedFoldout, $"Засинхронизированные этюды:({synchronizedReferences.Count})",EditorStyles.foldout);

            if (synchronizedFoldout && drawerData.SynchronizedRects.Count == synchronizedReferences.Count)
            {
                for (int i = 0; i < synchronizedReferences.Count; i++)
                {
                    if (GUI.Button(drawerData.SynchronizedRects[i], $"{synchronizedReferences[i].ReferencingObjectName} \nin {synchronizedReferences[i].AssetPath.Replace("Assets/Mechanics/Blueprints","")}", EditorStyles.miniLabel))
                    {
                        Selection.activeObject = LoadReference(synchronizedReferences[i]);
                    }
                }
            }
            
            otherFoldout = EditorGUI.Foldout(drawerData.OtherReferencesLabelRect, otherFoldout, $"Прочие ссылки:({otherReferences.Count})",EditorStyles.foldout);

            if ( otherFoldout && drawerData.OtherRects.Count == otherReferences.Count)
            {
                for (int i = 0; i < otherReferences.Count; i++)
                {
                    if (GUI.Button(drawerData.OtherRects[i], $"{otherReferences[i].ReferencingObjectName} \nin {otherReferences[i].AssetPath.Replace("Assets/Mechanics/Blueprints","")}", EditorStyles.miniLabel))
                    {
                        Selection.activeObject = LoadReference(otherReferences[i]);
                    }
                }
            }

            conflictingGroupFoldout = EditorGUI.Foldout(drawerData.ConflictingGroupsLabelRect, conflictingGroupFoldout, $"Конфликты:({conflictingGroupReferences.Count})",EditorStyles.foldout);

            if (conflictingGroupFoldout && drawerData.ConflictingGroupsRects.Count == conflictingGroupReferences.Count)
            {
                for (int i = 0; i < conflictingGroupReferences.Count; i++)
                {
                    DrawEtudeColor(loadedEtudes[conflictingGroupReferences[i]],drawerData.ConflictingGroupsRects[i]);

                    string isThis = (selectedEntry.ObjectGuid == conflictingGroupReferences[i]) ? "(THIS)" : "";
                
                    if (GUI.Button(drawerData.ConflictingGroupsRects[i].ScaleSizeBy(new Vector2(1f,0.5f),drawerData.ConflictingGroupsRects[i].TopLeft()), $"({loadedEtudes[conflictingGroupReferences[i]].Priority}){isThis}  {loadedEtudes[conflictingGroupReferences[i]].Name} ", EditorStyles.miniLabel))
                    {
                        Selection.activeObject = BlueprintEditorWrapper.Wrap(ResourcesLibrary.TryGetBlueprint(conflictingGroupReferences[i])); 
                    }
                
                    GUI.Label(drawerData.ConflictingGroupsRects[i].ScaleSizeBy(new Vector2(1f,0.5f),drawerData.ConflictingGroupsRects[i].TopLeft()+Vector2.up*40),$"Группы: {GetConflictingGroupNames(conflictingGroupReferences[i])}",EditorStyles.miniLabel);
                }
            }
        }

        private string GetConflictingGroupNames(string etudeID)
        {
            string result ="";

            foreach (var conflictingGroup in loadedEtudes[etudeID].ConflictingGroups)
            {
                result += EtudesTreeLoader.Instance.ConflictingGroups[conflictingGroup].Name + " ";
            }
            
            return result;
        }


        ///zoom
        
        private Vector2 ConvertScreenCoordsToZoomCoords(Vector2 screenCoords)
        {
            return (screenCoords - workspaceRect.TopLeft()) / _zoom + _zoomCoordsOrigin;
        }

        private void HandleEvents()
        {
            // Allow adjusting the zoom with the mouse wheel as well. In this case, use the mouse coordinates
            // as the zoom center instead of the top left corner of the zoom area. This is achieved by
            // maintaining an origin that is used as offset when drawing any GUI elements in the zoom area.
            if (Event.current.type == EventType.ScrollWheel)
            {
                Vector2 screenCoordsMousePos = Event.current.mousePosition;
                Vector2 delta = Event.current.delta;
                Vector2 zoomCoordsMousePos = ConvertScreenCoordsToZoomCoords(screenCoordsMousePos);
                float zoomDelta = -delta.y / 150.0f;
                float oldZoom = _zoom;
                _zoom += zoomDelta;
                _zoom = Mathf.Clamp(_zoom, kZoomMin, kZoomMax);
                _zoomCoordsOrigin += (zoomCoordsMousePos - _zoomCoordsOrigin) - (oldZoom / _zoom) * (zoomCoordsMousePos - _zoomCoordsOrigin);
                Event.current.Use();
            }

            // Allow moving the zoom area's origin by dragging with the middle mouse button or dragging
            // with the left mouse button with Alt pressed.
            if (Event.current.type == EventType.MouseDrag &&
                Event.current.button == 2)
            {
                Vector2 delta = Event.current.delta;
                delta /= _zoom;
                _zoomCoordsOrigin -= delta;

                Event.current.Use();
            }
        }
    }

    public class EditorZoomArea
    {
        private const float kEditorWindowTabHeight = 21.0f;
        private static Matrix4x4 _prevGuiMatrix;

        public static Rect Begin(float zoomScale, Rect screenCoordsArea)
        {
            GUI.EndGroup();        // End the group Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.

            Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());
            clippedArea.y += kEditorWindowTabHeight;
            GUI.BeginGroup(clippedArea);

            _prevGuiMatrix = GUI.matrix;
            Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
            GUI.matrix = translation * scale * translation.inverse * GUI.matrix;

            return clippedArea;
        }

        public static void End()
        {
            GUI.matrix = _prevGuiMatrix;
            GUI.EndGroup();
            GUI.BeginGroup(new Rect(0.0f, kEditorWindowTabHeight, Screen.width, Screen.height));
        }
    }

    public static class RectExtensions
    {
        public static Vector2 TopLeft(this Rect rect)
        {
            return new Vector2(rect.xMin, rect.yMin);
        }
        public static Rect ScaleSizeBy(this Rect rect, float scale)
        {
            return rect.ScaleSizeBy(scale, rect.center);
        }
        public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale;
            result.xMax *= scale;
            result.yMin *= scale;
            result.yMax *= scale;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }
        public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
        {
            return rect.ScaleSizeBy(scale, rect.center);
        }
        public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale.x;
            result.xMax *= scale.x;
            result.yMin *= scale.y;
            result.yMax *= scale.y;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }
    }

    public class BlockersInfo
    {
        public Etude Owner;
        public HashSet<Etude> Blockers;
        public bool IsEmpty => Owner == null;
        public void Clear()
        {
            Owner = null;
            Blockers?.Clear();
        }
    }
}

#endif