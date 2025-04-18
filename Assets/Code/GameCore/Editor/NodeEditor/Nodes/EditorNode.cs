using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem.Interfaces;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Core.Utility;
using Owlcat.Runtime.Core.Utility.EditorAttributes;
using RectEx;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.NodeEditor.Nodes
{
    public abstract class EditorNode
    {
        private static int s_NextNodeId;
        public readonly Graph Graph;

        [CanBeNull]
        private EditorNode m_Parent;

        public bool FadeOut = false;
        public bool Foldout = false;
        
        const float FoldoutButtonSize = 40;
        private string FoldoutLabel => Foldout ? "Collapsed" : "Expanded";

        [CanBeNull]
        public EditorNode Parent
        {
            get { return m_Parent; }
            set
            {
                m_Parent = value;
                if (value != null)
                    SetParentAsset(value.GetAsset());
            }
        }

        public IEnumerable<EditorNode> VirtualNodes
        {
            get { return VirtualChildren.Values; }
        }

        internal readonly Dictionary<ScriptableObject, EditorNode> VirtualChildren =
            new Dictionary<ScriptableObject, EditorNode>();

        private readonly int m_Id;

        public int GroupId
        {
            get { return Graph.GetGroupId(GetAsset()); }
            set
            {
                int current = GroupId;
                if (current > 0 && current != value)
                    Debug.LogWarningFormat("Node {0} belongs to multiple cue groups, bugs are likely.", GetAsset());
                Graph.SetGroupId(GetAsset(), value);
            }
        }

        public Vector2 Center;
        public Vector2 Size;

        protected EditorNode(Graph graph, Vector2 size)
        {
            m_Id = s_NextNodeId++;
            Graph = graph;
            Size = size;
        }

        public virtual EditorNode AddVirtualChild(EditorNode referencedNode)
        {
            EditorNode child = new VirtualNode(Graph, referencedNode);
            child.Parent = this;
            VirtualChildren[referencedNode.GetAsset()] = child;
            return child;
        }

        public void ClearVirtualChildren()
        {
            VirtualChildren.Clear();
        }

        public void Draw(CanvasView view)
        {
            Profiler.BeginSample("Schedule Window");
            try
            {
                if (GetAsset() == null)
                    return;
                var bp = GetBlueprint();
                if (bp != null && BlueprintsDatabase.GetMetaById(bp.AssetGuid).ShadowDeleted)
                    GUI.color = Colors.ShadowDeleted;
                else
                    GUI.color = GetWindowColor();

                if (this == Graph.SelectedNode)
                    GUI.color = Colors.GetHighlighColor(GUI.color);

                if (FadeOut)
                    GUI.color = Colors.GetFadeColor(GUI.color);

                Rect r = new Rect(Center, Size);
                r.position -= Size / 2f;
                r = view.ToScreen(r);

                // check that node is visible on screen
                if (NodeEditorBase.DrawAllNodes || r.Overlaps(view.VisibleScreenArea))
                {
                    var oldSize = Size;
                    r = GUILayout.Window(m_Id, r, DrawWindow, GetName());
                    
                    
                    Size = r.size;
                    if (Size != oldSize)
                        Graph.Layout();
                }
                
                DrawFoldoutButton(r);

                if (bp is IEditorCommentHolder)
                {
                    DrawCommentToFoldedNode(r);
                }

                if (GetBlueprint() is ILocalizedStringHolder blueprint && blueprint.LocalizedStringText.Shared)
                {
                    DrawSharedStringMarker(r);
                }

            }
            finally
            {
                Profiler.EndSample();
            }

            if (Foldout)
            {
                return;
            }

            foreach (EditorNode virtualChild in VirtualChildren.Values)
            {
                virtualChild.Draw(view);
            }
        }

        private void DrawSharedStringMarker(Rect r)
        {
            GUI.color = Color.yellow;
            float markerSize = 45f;
            
            var markerRect = new Rect(new Vector2(r.xMax , r.yMax - markerSize), new Vector2(markerSize, markerSize));
            var boxStyle = new GUIStyle(EditorStyles.textArea);
            GUI.Box(markerRect, GUIContent.none, boxStyle);
            
            
            var labelStyle = new GUIStyle(EditorStyles.textArea)
                { wordWrap = true, fontSize = 25, alignment = TextAnchor.MiddleCenter };
            EditorGUI.LabelField(markerRect, "Ш", labelStyle);
        }

        private void DrawFoldoutButton(Rect r)
        {
            var buttonRect = new Rect(new Vector2(r.xMax - FoldoutButtonSize * 2, r.yMin - FoldoutButtonSize * 0.5f),
                new Vector2(FoldoutButtonSize * 2, FoldoutButtonSize * 0.5f));
            var boxStyle = new GUIStyle(EditorStyles.textArea) {stretchWidth = true};
            GUI.Box(buttonRect, GUIContent.none, boxStyle);
            
            var buttonStyle = new GUIStyle(EditorStyles.textArea)
                {wordWrap = true, fontSize = 10, stretchWidth = true};
            
            GUI.color = Foldout ? Colors.Collapsed : Colors.Expanded;
            
            if (GUI.Button(buttonRect, FoldoutLabel, buttonStyle))
            {
                // dropping focus from other windows
                GUI.FocusControl("");
                Graph.SelectedNode = this;
                Graph.Repaint();
                Event.current.Use();

                this.OpenCloseAllChildren(!Foldout);
            }
        }
        
        private void DrawCommentToFoldedNode(Rect r)
        {
            if (Foldout)
            {
                GUI.color = Color.white;
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                const float sizeY = 200;
                float centeredY = r.yMin + (r.yMax - r.yMin) * 0.5f - sizeY * 0.5f;
                var noteRect = new Rect(new Vector2(r.xMax, centeredY), new Vector2(sizeY * 2, sizeY));
                var boxStyle = new GUIStyle(EditorStyles.textArea)
                    {stretchWidth = true, border = new RectOffset(2, 2, 2, 2)};
                GUI.Box(noteRect, GUIContent.none, boxStyle);
                
                const int fontSize = 45;
                var textAreaStyle = new GUIStyle(EditorStyles.textArea)
                    {wordWrap = true, fontSize = fontSize, stretchWidth = true};

                var toolbarRect = noteRect.CutFromTop(20f);
                EditorGUI.LabelField(toolbarRect[0], "EditorOnlyComment");

                GetSerializedObject().Update();
                var property = GetSerializedObject().FindProperty("Blueprint.m_EditorComment.m_EditorComment");
                if (property != null)
                {
                    property.stringValue = EditorGUI.TextArea(toolbarRect[1], property.stringValue, textAreaStyle);
                }

                GetSerializedObject().ApplyModifiedProperties();

                if (FadeOut)
                {
                    GUI.color = Colors.GetFadeColor(GUI.color);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        public void DrawDebug(CanvasView view)
        {
            if (Foldout)
            {
                return;
            }
            
            int index = 0;
            Vector2 messagesStart = Center - Size / 2 + new Vector2(0f, 20f);
            foreach (var m in DialogDebug.DebugMessages)
            {
                if (m.Blueprint != BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(GetAsset()))
                    continue;

                Profiler.BeginSample("One Message");
                Rect messageRect = new Rect(messagesStart + new Vector2(0f, 16 * index), new Vector2(280f, 16f));
                messageRect = view.ToScreen(messageRect);
                index++;

                GUI.color = m.Color;
                var style = new GUIStyle(GUI.skin.button);
                style.alignment = TextAnchor.MiddleLeft;
                GUI.Button(messageRect, m.Message, style);
                Profiler.EndSample();
            }

            Profiler.BeginSample("Children");
            foreach (EditorNode virualChild in VirtualChildren.Values)
                virualChild.DrawDebug(view);
            Profiler.EndSample();
        }

        public virtual void DrawConnections(CanvasView view, bool foldout)
        {
            if (foldout)
            {
                foreach (EditorNode virtualChild in VirtualChildren.Values)
                    virtualChild.DrawConnections(view, true);
                return;
            }

            if (GetAsset() == null)
                return;
            foreach (EditorNode node in GetReferencedNodes())
                DrawFunctions.Connection(view, this, node, Colors.Connection);

            if (Graph.ShowRelations)
            {
                var assetPath = AssetDatabase.GetAssetPath(GetAsset());
                var children = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var child in children)
                {
                    if (child == GetAsset())
                        continue;
                    var so = new SerializedObject(child);
                    var p = so.GetIterator();
                    do
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference)
                            continue;
                        var o = p.objectReferenceValue as ScriptableObject;
                        if (o == null)
                            continue;
                        if (!Graph.ContainsNode(o))
                            continue;
                        var node = Graph.GetNode(o);
                        DrawFunctions.Connection(view, this, 8, node, 8, Colors.ReferenceLink);
                    } while (p.Next(true));
                }
            }

            foreach (EditorNode virtualChild in VirtualChildren.Values)
                virtualChild.DrawConnections(view, false);
        }

        public virtual Color GetWindowColor()
        {
            return Colors.Default;
        }

        public bool MatchesFilter(string filter)
        {
            return GetName().ToLowerInvariant().Contains(filter)
                || GetText().ToLowerInvariant().Contains(filter);
        }

        public abstract string GetName();

        public virtual string GetText()
        {
            return "";
        }

        public abstract ScriptableObject GetAsset();

        public BlueprintScriptableObject GetBlueprint()
            => BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(GetAsset());

        public abstract ScriptableObject GetParentAsset();

        public abstract void SetParentAsset(ScriptableObject parent);

        public abstract SerializedObject GetSerializedObject();

        public virtual bool CanBeShared()
        {
            return true;
        }

        public void LoadParentNode()
        {
            var parentAsset = GetParentAsset();
            if (parentAsset == null)
                return;
            if (Graph.ContainsNode(parentAsset))
                Parent = Graph.GetNode(parentAsset);
        }

        private void DrawWindow(int id)
        {
            Profiler.BeginSample("Draw Window");
            try
            {
                if (FadeOut)
                {
                    GUI.color = Colors.GetFadeColor(GUI.color);
                    GUI.contentColor = Colors.GetFadeColor(GUI.contentColor);
                }

                if (Event.current.type == EventType.MouseDown)
                {
                    Graph.SelectedNode = this;
                    Graph.Repaint();
                }

                if (Graph.SelectedNode == this && Event.current.type == EventType.MouseUp)
                {
                    Selection.activeObject = GetAsset();
                }

                NodeEditorBase.CurrentNode = this;

                Profiler.BeginSample("DrawComment()");
                var commentChanged = DrawComment();
                Profiler.EndSample();
                
                Profiler.BeginSample("DrawContent()");
                using (new InfoBoxDisableScope())
                {
                    DrawContent();
                }
                Profiler.EndSample();

                Profiler.BeginSample("DrawMarkers()");
                DrawMarkers(Graph.ShowExtendedMarkers);
                Profiler.EndSample();

                Profiler.BeginSample("DragAndDrop");
                DragAndDropController.Update(this);
                Profiler.EndSample();

                if (Event.current.type == EventType.MouseDown)
                {
                    // dropping focus from other windows
                    GUI.FocusControl("");
                    Graph.SelectedNode = this;
                    Graph.Repaint();
                    Event.current.Use();
                }

                if (Event.current.button != 2)
                    GUI.DragWindow();

                if (commentChanged)
                {
                    var w = GetAsset() as BlueprintEditorWrapper;
                    LocalizationUtility.AddCommentsToJsons(w);
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private void DrawMarkers(bool extended)
        {
            var markers = GetMarkers(extended).ToList();
            if (markers.Count <= 0)
                return;

            GUILayout.BeginHorizontal();
            int markersOnLine = 0;
            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.focused = style.normal;
            style.active = style.normal;
            if (extended)
            {
                style.wordWrap = false;
                style.alignment = TextAnchor.UpperLeft;
            }

            foreach (string marker in markers)
            {
                GUILayout.Button(marker, style, GUILayout.ExpandWidth(false));
                markersOnLine++;
                if (extended || NodeEditorBase.SingleLineMarkers || markersOnLine >= 3)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    markersOnLine = 0;
                }
            }
            GUILayout.EndHorizontal();
        }

        private bool DrawComment()
        {
            if (this is VirtualNode)
                return false;

            var w = GetAsset() as
                BlueprintEditorWrapper;
            var blueprint = w.Blueprint;
            if (blueprint == null)
                return false;

            var commentChanged = false;
            if (!string.IsNullOrEmpty((blueprint as BlueprintScriptableObject)?.Comment))
            {
                GUI.color = Color.magenta;
                if (FadeOut)
                {
                    GUI.color = Colors.GetFadeColor(GUI.color);
                }

                GetSerializedObject().Update();
                var ww = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                var property = GetSerializedObject().FindProperty("Blueprint.Comment");
                if (property != null)
                {
                    property.stringValue = EditorGUILayout.TextArea(property.stringValue, ww);
                }

                if (GetSerializedObject().ApplyModifiedProperties())
                    commentChanged = true;
                
                GUI.color = Color.white;
                if (FadeOut)
                {
                    GUI.color = Colors.GetFadeColor(GUI.color);
                }
            }

            return commentChanged;
        }

        protected abstract void DrawContent();

        public virtual IEnumerable<string> GetMarkers(bool extended)
        {
            return Enumerable.Empty<string>();
        }

        public IEnumerable<ScriptableObject> GetAllReferencedAssets()
        {
            return GetAllReferencedAssetsInternal().Select(a => BlueprintEditorWrapper.Wrap(a)).NotNull();
        }

        protected virtual IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal() { yield break; }

        public IEnumerable<EditorNode> GetReferencedNodes()
        {
            return GetAllReferencedAssets()
                .Select(GetReferencedNode);
        }

        public EditorNode GetReferencedNode(ScriptableObject asset)
        {
            if (asset == null)
            {
                PFLog.Default.Error($"NULL reference in node for {GetAsset().NameSafe()}");
            }
            EditorNode virtualChild;
            VirtualChildren.TryGetValue(asset, out virtualChild);
            if (virtualChild != null)
                return virtualChild;
            return Graph.GetNode(asset);
        }
        public EditorNode GetReferencedNode(SimpleBlueprint asset)
        {
            return GetReferencedNode(BlueprintEditorWrapper.Wrap(asset));
        }

        public virtual bool CanAddReference(Type type, SimpleBlueprint r = null)
        {
            GetSerializedObject().Update();
            return GetListProperty(type, r) != null;
        }

        [CanBeNull]
        protected abstract SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null);

        public virtual void AddReferencedAsset(ScriptableObject asset)
        {
            var list = GetListProperty(asset.GetWrappedType(), BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(asset));
            if (list == null)
                return;

            GetSerializedObject().Update();
            list.arraySize++;
            SetReferencedAsset(list, list.arraySize - 1, asset);
            GetSerializedObject().ApplyModifiedProperties();

            UndoManager.Instance.RegisterUndo("", () => VirtualChildren.Remove(asset));
        }

        private void SetReferencedAsset(SerializedProperty list, int index, ScriptableObject asset)
        {
            PFLog.Default.Log($"{GetAsset().name}: Set RA at {index} to {asset.name}");
            if (list.propertyType == SerializedPropertyType.ObjectReference)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            }
            else if (list.propertyType == SerializedPropertyType.Generic && list.type.EndsWith("Reference"))
            {
                var elt = list.GetArrayElementAtIndex(index);
                BlueprintReferenceBase.SetPropertyValue(elt, BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(asset));
            }
        }

        private ScriptableObject GetReferencedAsset(SerializedProperty list, int index)
        {
            if (list.propertyType == SerializedPropertyType.ObjectReference)
            {
                return list.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableObject;
            }
            else if (list.propertyType == SerializedPropertyType.Generic)
            {
                return BlueprintEditorWrapper.Wrap(
                    BlueprintReferenceBase.GetPropertyValue(list.GetArrayElementAtIndex(index)));
            }
            return null;
        }

        public virtual void RemoveReferencedAsset(ScriptableObject asset, bool move = false)
        {
            var list = GetListProperty(asset.GetWrappedType(), BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(asset));
            if (list == null)
                return;

            VirtualChildren.Remove(asset);

            GetSerializedObject().Update();
            for (int i = list.arraySize - 1; i >= 0; i--)
                if (GetReferencedAsset(list, i) == asset)
                    list.DeleteArrayElementAtIndex(i);

            GetSerializedObject().ApplyModifiedProperties();
        }

        public void ReorderReferrencedAsset(ScriptableObject asset, int shift)
        {
            var list = GetListProperty(asset.GetWrappedType(), BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(asset));
            if (list == null)
                return;

            GetSerializedObject().Update();

            int prevIndex = -1;
            for (int i = 0; i < list.arraySize; ++i)
                if (GetReferencedAsset(list, i) == asset)
                    prevIndex = i;

            if (prevIndex < 0)
                return;

            int newIndex = Math.Min(Math.Max(0, prevIndex + shift), list.arraySize - 1);
            if (newIndex == prevIndex)
                return;

            if (newIndex < prevIndex)
            {
                for (int i = prevIndex; i > newIndex; i--)
                    SetReferencedAsset(list, i, GetReferencedAsset(list, i - 1));
                //list.GetArrayElementAtIndex(i).objectReferenceValue = list.GetArrayElementAtIndex(i - 1).objectReferenceValue;
            }

            if (newIndex > prevIndex)
            {
                for (int i = prevIndex; i < newIndex; i++)
                    SetReferencedAsset(list, i, GetReferencedAsset(list, i + 1));
                //list.GetArrayElementAtIndex(i).objectReferenceValue = list.GetArrayElementAtIndex(i + 1).objectReferenceValue;
            }

            SetReferencedAsset(list, newIndex, asset);

            GetSerializedObject().ApplyModifiedProperties();
        }

        public virtual void RemoveReferencedAssets(Predicate<Object> predicate)
        {
            GetSerializedObject().Update();
            foreach (Type type in NodeEditorAssetType.AllTypes.Select(t => t.Template.GetType()))
            {
                var links = GetListProperty(type);
                if (links == null)
                    continue;

                for (int i = links.arraySize - 1; i >= 0; i--)
                    if (predicate(GetReferencedAsset(links, i)))
                        links.DeleteArrayElementAtIndex(i);
            }
            GetSerializedObject().ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public abstract class EditorNode<T> : EditorNode where T : SimpleBlueprint // todo: [bp] also element?
    {
        protected readonly T Asset;

        protected readonly SerializedObject SerializedObject;

        protected SerializedProperty FindProperty(string p)
            => SerializedObject.FindProperty("Blueprint." + p);

        public override string GetName()
        {
            return Asset.name;
        }

        public override ScriptableObject GetAsset()
        {
            return BlueprintEditorWrapper.Wrap(Asset as SimpleBlueprint);
        }

        public override ScriptableObject GetParentAsset()
        {
            var p = FindProperty("ParentAsset");
            if (p == null || p.propertyType != SerializedPropertyType.String)
                return null;
            var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(p.stringValue);
            return BlueprintEditorWrapper.Wrap(bp);
        }

        public override void SetParentAsset(ScriptableObject parent)
        {
            using (GuiScopes.UpdateObject(SerializedObject))
            {
                var p = FindProperty("ParentAsset");
                if (p == null || p.propertyType != SerializedPropertyType.String)
                    return;
                var bp = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(parent);
                p.stringValue = bp?.AssetGuid ?? "";
            }
        }

        protected EditorNode(Graph graph, T asset, Vector2 size) : base(graph, size)
        {
            Asset = asset;
            SerializedObject = new SerializedObject(GetAsset());
        }

        public override SerializedObject GetSerializedObject()
        {
            return SerializedObject;
        }
    }
}