using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Editor;
using Code.Editor.KnowledgeDatabase;
using Code.GameCore.ElementsSystem;
using Editors;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Editor.UIElements;
using Owlcat.Runtime.Core.Utility;
using Kingmaker.EntitySystem;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.CodeTimer;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Owlcat.Editor.Core.Utility;
using Owlcat.QA.Validation;
using Owlcat.Runtime.Core.Utility.EditorAttributes;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.EditorPreferences;
using Kingmaker.Utility.UnityExtensions;


namespace Kingmaker.Editor.Blueprints
{
    [CustomEditor(typeof(BlueprintEditorWrapper))]
    public class BlueprintInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (m_Custom?.TotalOverride ?? false)
                return null; // no elements if custom gui wants total control

            ((BlueprintEditorWrapper)target).SyncPropertiesWithProto();
            var inspector = UIElementsUtility.CreateInspector(serializedObject);
            m_Inspector = inspector;
            OnSetDirty();
            return inspector;
        }

        private List<SerializedObject> m_ComponentsWrapped;
        private int m_SoloIndex = -1;
        private BlueprintInspectorCustomGUI m_Custom;
        private Texture2D m_HeaderIcon;
        private VisualElement m_Inspector;

        private SimpleBlueprint Blueprint
            => BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(target);

        private BlueprintScriptableObject BlueprintComplex
            => BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(target);

        private void OnEnable()
        {
            UpdateComponentsWrappers();
            if (Blueprint != null)
            {
                m_Custom = BlueprintInspectorCustomGUI.GetForType(Blueprint.GetType());
                
                m_Custom?.OnEnable(this);
            }

            string iconname = "BlueprintIcons/" + Blueprint?.GetType().Name + ".png";
            var icon = EditorGUIUtility.Load(iconname) as Texture2D;
            m_HeaderIcon = icon.Or(OwlcatEditorStyles.Instance.BlueprintItemIcon);
            
            BlueprintsDatabase.OnInvalidated += UpdateComponentsWrappers;
            BlueprintsDatabase.OnInvalidated += OnSetDirty;
            BlueprintsDatabase.OnSetDirty += OnSetDirty;
            BlueprintsDatabase.OnSaved += OnSetDirty;
        }
        
        private void OnDisable()
        {
            BlueprintsDatabase.OnInvalidated -= UpdateComponentsWrappers;
            BlueprintsDatabase.OnInvalidated -= OnSetDirty;
            BlueprintsDatabase.OnSetDirty -= OnSetDirty;
            BlueprintsDatabase.OnSaved -= OnSetDirty;
        }

        private void OnSetDirty()
        {
            if (m_Inspector == null)
                return;
            
            string id = ((BlueprintEditorWrapper)target).Blueprint?.AssetGuid;

            if (id == null)
                return;

            m_Inspector.Q("SaveButton")?.SetEnabled(BlueprintsDatabase.IsDirty(id));
            m_Inspector.Q("DiscardButton")?.SetEnabled(BlueprintsDatabase.IsDirty(id));
        }

        public void DrawPropertiesExcluding(params string[] props)
        {
            DrawPropertiesExcluding(serializedObject, props);
        }

        private void UpdateComponentsWrappers()
        {
            if (BlueprintComplex != null)
            {
                foreach (var c in m_ComponentsWrapped.EmptyIfNull())
                {
                    DestroyImmediate(c.targetObject);
                }
                m_ComponentsWrapped = new List<SerializedObject>();
                for (var ii = 0; ii < BlueprintComplex.ComponentsArray.Length; ii++)
                {
                    var comps = targets
                        .Cast<BlueprintEditorWrapper>()
                        .Select(w => ((BlueprintScriptableObject)w.Blueprint).ComponentsArray.Get(ii))
                        .Select(c => BlueprintComponentEditorWrapper.Wrap(c))
                        .ToArray();

                    bool typesMacth = true;
                    Type type = null;
                    for (int jj = 0; jj < targets.Length; jj++)
                    {
                        type = type ?? comps[jj].Component?.GetType();
                        if (type != comps[jj].Component?.GetType())
                        {
                            typesMacth = false;
                            break;
                        }
                    }

                    m_ComponentsWrapped.Add(typesMacth ? new SerializedObject(comps) : null);
                }
            }
        }

        protected override void OnHeaderGUI()
        {
            //base.OnHeaderGUI();
            using (var s = new EditorGUILayout.HorizontalScope("In BigTitle"))
            {
                GUILayout.Space(5);
                if (GUILayout.Button(m_HeaderIcon, GUIStyle.none, GUILayout.Width(32), GUILayout.Height(32)))
                {
                    BlueprintProjectView.Ping(Blueprint);
                }
                if (BlueprintsDatabase.IsDirty(Blueprint.AssetGuid))
                {
                    var r = GUILayoutUtility.GetLastRect();
                    GUI.DrawTexture(new Rect(r.xMax - 9, r.yMin, 10, 16),
                        OwlcatEditorStyles.Instance.BlueprintIsDirtyIcon);
                }

                GUILayout.Space(5);

                using (new EditorGUILayout.VerticalScope())
                {
                    bool shadowDeleted = BlueprintsDatabase.GetMetaById(Blueprint.AssetGuid).ShadowDeleted;
                    GUILayout.Label(Blueprint.name, EditorStyles.boldLabel);
                    if (shadowDeleted)
                    {
                        var r = GUILayoutUtility.GetLastRect();
                        r.y += r.height/ 2;
                        r.height = 1;
                        using (GuiScopes.Color(Color.red))
                        {
                            EditorGUI.DrawRect(r, Color.red);
                            GUILayout.Label("DELETED. DO NOT USE", EditorStyles.whiteLabel);
                        }
                    }

                    GUILayout.Label(Blueprint.GetType().Name);
                }
                
                GUILayout.FlexibleSpace();
                var blueprintProperty = serializedObject.FindProperty("Blueprint");
                PrototypedObjectEditorUtility.DrawDescriptionButton(blueprintProperty);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && s.rect.Contains(Event.current.mousePosition))
                {
                    var menu = new GenericMenu();
                    BlueprintContextMenu.AddItemsToMenu(menu, Blueprint);
                    menu.ShowAsContext();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            //EditorGUILayout.LabelField(target.GetHashCode().ToString());
            //EditorGUILayout.LabelField(target.hideFlags.ToString()); 
            //base.OnInspectorGUI();

            BlueprintJsonWrapper blueprintJsonWrapper = BlueprintsDatabase.GetCachedWrapper(Blueprint.AssetGuid);
            if (blueprintJsonWrapper == null)
            {
                blueprintJsonWrapper = BlueprintsDatabase.LoadWrapperById(Blueprint.AssetGuid);
            }

            if (blueprintJsonWrapper == null ||
                blueprintJsonWrapper.ReusedWrapper == null)
            {
                Selection.activeObject = BlueprintEditorWrapper.Wrap(Blueprint);;
            }
            else if(target != blueprintJsonWrapper.ReusedWrapper)
            {
                if (Selection.activeObject == target) // auto reselect correct wrapper if possible
                {
                    Selection.activeObject = BlueprintEditorWrapper.Wrap(Blueprint);
                    return;
                }
                EditorGUILayout.HelpBox("This is not the canonical wrapper", MessageType.Error);
            }

            if (Blueprint is BlueprintBroken broken)
            {
                if (broken.Exception == null)
                {
                    EditorGUILayout.HelpBox("Blueprint is broken", MessageType.Error);
                    return;
                }

                using (GuiScopes.Horizontal())
                {
                    if (GUILayout.Button("Copy Error"))
                    {
                        GUIUtility.systemCopyBuffer = broken.Exception.ToString();
                    }

                    string path = BlueprintsDatabase.GetAssetPath(Blueprint);
                    if (!path.IsNullOrEmpty())
                    {
                        if (GUILayout.Button("Show in explorer"))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", "/select," + path);
                        }

                        if (GUILayout.Button("Open as file"))
                        {
                            Application.OpenURL(path);
                        }
                    }
                }

                EditorGUILayout.HelpBox(broken.Exception.Message, MessageType.Error);
                return;
            }

            m_Custom?.OnHeader(Blueprint);
            if (m_Custom?.TotalOverride ?? false)
            {
                return;
            }

            using (GuiScopes.UpdateObject(serializedObject))
            {
                if (m_SoloIndex < 0)
                {
                    OnInspectorGuiProperties();
                }
            }
            DrawActionsForObject(Blueprint);

            m_Custom?.OnBeforeComponents(Blueprint);

            OnInspectorGuiComponents();

            m_Custom?.OnFooter(Blueprint);

            var id = ((BlueprintEditorWrapper)target).Blueprint?.AssetGuid;

            if (id != null)
            {
                if (BlueprintsDatabase.IsDirty(id))
                {
                    using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label(BlueprintsDatabase.IsChangedOnDisk(id)
                            ? "Changed on disk AND in Unity"
                            : "Changed in Unity");

                        if (GUILayout.Button("Save"))
                        {
                            BlueprintsDatabase.Save(id);
                        }

                        if (EditorPreferences.Instance.ProjectIsModTemplate)
                        {
                            if (GUILayout.Button("Save as Patch"))
                            {
                                BlueprintPatchEditorUtility.SavePatch(BlueprintComplex);
                            }
                        }

                        if (GUILayout.Button("Discard"))
                        {
                            BlueprintsDatabase.Discard(id);
                        }
                    }
                }
                else
                {
                    if (EditorPreferences.Instance.ProjectIsModTemplate)
                    {
                        using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                        {
                            if (GUILayout.Button("Save as Patch"))
                            {
                                BlueprintPatchEditorUtility.SavePatch(BlueprintComplex);
                            }
                        }
                    }
                }
            }
        }

        public void OnInspectorGuiProperties()
        {
            Profiler.BeginSample("BlueprintInspector.OnInspectorGuiProperties() header");

            // header
            using (
                new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(16);

                if (GUILayout.Button("New window", EditorStyles.miniButtonLeft))
                {
                    BlueprintInspectorWindow.OpenFor(Blueprint);
                }

                if (GUILayout.Button("Find References", EditorStyles.miniButtonMid))
                {
                    ReferencesWindow.ReferencesWindow.FindReferencesInProject(Blueprint);
                }

                IBlueprintScanner scanner = Blueprint as IBlueprintScanner;
                if (scanner != null)
                {
                    if (GUILayout.Button("Scan", EditorStyles.miniButtonMid))
                    {
                        foreach (var tgt in targets)
                        {
                            (BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(tgt) as IBlueprintScanner)?.Scan();
                            EditorUtility.SetDirty(tgt);

                        }
                        serializedObject.Update();
                    }
                }

                //Allow moders create patches for NonOverridable blueprint types
                #if OWLCAT_MODS
                if (BlueprintComplex != null && targets.Length == 1)
                #else 
                if (BlueprintComplex != null && targets.Length == 1 && !BlueprintComplex.GetType().HasAttribute<NonOverridableAttribute>())
                #endif 
                {
                    if (GUILayout.Button("Create inherited", EditorStyles.miniButtonMid))
                    {
                        PrototypableUtility.CreateInheritedAsset(BlueprintComplex);
                    }

                    if (GUILayout.Button(
                        "Sync children",
                        EditorStyles.miniButtonMid))
                    {
                        PrototypableUtility.SyncWithChildren(BlueprintComplex);
                    }
                    
                    if (GUILayout.Button(
                            "Sync children recursively",
                            BlueprintComplex.PrototypeLink != null ? EditorStyles.miniButtonMid : EditorStyles.miniButtonRight))
                    {
                        PrototypableUtility.SyncWithChildrenDeep(BlueprintComplex);
                    }

                    using (GuiScopes.Color(new Color(0.7f, 0.9f, 1)))
                    {
                        if (BlueprintComplex.PrototypeLink != null && GUILayout.Button("Sync with proto", EditorStyles.miniButtonRight))
                        {
                            ((BlueprintEditorWrapper)target).SyncPropertiesWithProto();
                        }
                    }
                }

                GUILayout.Space(16);
            }

            BlueprintEditorUtility.ShowType("Script", Blueprint.GetType());

            if (BlueprintComplex != null && targets.Length == 1) // do not show stuff for multi-selections
            {

                // todo: [bp] fix validation
                // show errors
                //bool hasErrorsInComponents = BlueprintComplex.ComponentsArray.EmptyIfNull().Any(c => c.ValidationStatus.Errors.Any());
                //var vc = AssetValidator.ValidatedObject == target ? AssetValidator.Context : Blueprint.ValidationStatus; // use validator context instead of blueprint's as it may be more up-to-date
                //bool hasErrors = vc.HasErrors || hasErrorsInComponents;
                //using (new EditorGUILayout.HorizontalScope())
                //{
                //    if (hasErrors)
                //    {
                //        using (GuiScopes.Color(Color.red))
                //        {
                //            if (GUILayout.Button("Validation errors", GUILayout.ExpandWidth(false)))
                //            {
                //                //Blueprint.Validate();
                //                AssetValidator.ShowValidationWindow(target, true);
                //            }
                //        }
                //        GUILayout.Space(10);
                //    }
                //    else
                //    {
                //        GUILayout.Button("", GUIStyle.none, GUILayout.ExpandWidth(false));
                //    }
                //    GUILayout.Label("Type: " + Blueprint.GetType().Name);
                //}

                using (new EditorGUILayout.HorizontalScope())
                {
                    var newProto = BlueprintEditorUtility.ObjectField(
                        "Prototype",
                        BlueprintComplex.PrototypeLink as SimpleBlueprint,
                        Blueprint.GetType(),
                        false);
                    if (newProto != BlueprintComplex.PrototypeLink)
                    {
                        if (EditorUtility.DisplayDialog(
                            "Change prototype",
                            "Changing prototype link is a dangerous operation that may affect multiple blueprints. Are you sure?",
                            "Yes",
                            "No"))
                        {
                            BlueprintComplex.SetPrototype((BlueprintScriptableObject)newProto, false, false);
                            //if (CheckInheritanceValidation())
                            //{
                            //    serializedObject.Update();
                            //    Blueprint.Validate();
                            //}
                        }
                    }

                    if (BlueprintComplex.PrototypeLink != null && GUILayout.Button("", OwlcatEditorStyles.Instance.OpenButton))
                        BlueprintInspectorWindow.OpenFor(BlueprintComplex.PrototypeLink as BlueprintScriptableObject);
                }
            }
            Profiler.EndSample();

            //EditorGUILayout.TextArea(string.Join("\n", Blueprint.PropertyOverrides.ToArray()));

            // properties

            PrototypedObjectEditorUtility.RootInspector = this;
            PrototypedObjectEditorUtility.ShowPropertyChildren(
                serializedObject.FindProperty(nameof(BlueprintEditorWrapper.Blueprint)));
        }
        
        private static IReadOnlyCollection<int> GetDuplicateIndices(BlueprintScriptableObject objectToValidate)
        {
            var indexed = objectToValidate.ComponentsArray.Select((v, i) => (v, i));
            var duplicates = from component in indexed
                let compIndex = component.i
                where component.v
                let type = component.v.GetType()
                where !type.HasAttribute<AllowMultipleComponentsAttribute>()
                group compIndex by type
                into byType
                where byType.Skip(1).Any()
                select byType;

            var duplicateIndices = duplicates
                .SelectMany(v => v)
                .Distinct()
                .ToHashSet();
            return duplicateIndices;
        }


        public void OnInspectorGuiComponents()
        {
            if (BlueprintComplex == null)
            {
                return;
            }

            var componentsArray = serializedObject.FindProperty("Blueprint.Components");
            // components
            using (ProfileScope.New("Draw components"))
            {
                var duplicateIndices = GetDuplicateIndices(BlueprintComplex);
                        
                int count = BlueprintComplex?.ComponentsArray.Length ?? 0;
                for (int ii = 0; ii < count; ii++)
                {
                    if (m_SoloIndex >= 0 && ii != m_SoloIndex)
                    {
                        continue;
                    }

                    GUILayout.Space(10);

                    using (var box = new EditorGUILayout.VerticalScope())
                    {
                        var oldColor = GUI.backgroundColor;
                        if(duplicateIndices.Contains(ii))
                            GUI.backgroundColor = Color.red;

                        try
                        {
                            var position = box.rect;
                            GUI.Box(position, "");

                            GUILayout.Space(5f);

                            using (new EditorGUILayout.VerticalScope())
                            {
                                GUILayout.Space(10f);
                                DrawComponent(componentsArray, ii);
                                DrawActionsForObject(BlueprintComplex.ComponentsArray[ii]);
                            }

                            GUILayout.Space(10f);
                        }
                        finally
                        {
                            GUI.backgroundColor = oldColor;
                        }
                    }
                }
            }

            var proto = BlueprintComplex?.PrototypeLink as BlueprintScriptableObject;
            if (proto)
            {
                foreach (var component in proto.ComponentsArray)
                {
                    if (BlueprintComplex.IsOverridden(component.name) &&
                        BlueprintComplex.ComponentsArray.All(c => c.PrototypeLink != component))
                    {
                        using (new EditorGUILayout.HorizontalScope("IN Title"))
                        {
                            if (GUILayout.Button("", OwlcatEditorStyles.Instance.RevertButton, GUILayout.Width(16)))
                            {
                                BlueprintComplex.SetOverridden(component.name, false);
                                ((ScriptableWrapperBase)target).SyncPropertiesWithProto();
                                UpdateComponentsWrappers();
                            }

                            // draw name
                            GUILayout.Label("Removed: " + ClassNames.GetObjectNameNoPrefix(component), "IN TitleText");
                        }
                    }
                }
            }

            GUILayout.Space(5);

            if (BlueprintComplex?.CanAddComponents() ?? false)
            {
                // addcomponent buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool nodeEditor = NodeEditorBase.Drawing;
                    GUILayout.FlexibleSpace();
                    TypePicker.Button(
                        "Add Component",
                        BlueprintComplex.GetValidComponentTypes,
                        type =>
                        {
                            AddComponentFromMenu(type);
                            if (nodeEditor)
                                BlueprintNodeEditor.CheckForNewNodes();
                            UpdateComponentsWrappers();
                        }
                    );
                    if (GUILayout.Button("Paste"))
                    {
                        var cc = componentsArray.arraySize;
                        CopyPasteController.PasteProperty(typeof(BlueprintComponent), componentsArray);
                        if (CopyPasteController.HasBlueprintComponent)
                        {
                            componentsArray.serializedObject.ApplyModifiedProperties();
                            // ugly hack: add override markers to any components that were pasted. Must do this after
                            // ApplyModifiedProperties because the override manager cannot work in serialized world
                            for (int ii = cc; ii < componentsArray.arraySize; ii++)
                            {
                                BlueprintComplex.SetOverridden(BlueprintComplex.ComponentsArray[ii].name, true);
                            }
                            serializedObject.Update();
                            BlueprintsDatabase.SetDirty(Blueprint.AssetGuid);
                            UpdateComponentsWrappers();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        public void DrawComponent(SerializedProperty componentsArray, int index)
        {
            if (Event.current.type == EventType.Used)
                return;

            var comTgt = m_ComponentsWrapped[index].targetObject as BlueprintComponentEditorWrapper;
            
            if (comTgt.WrappedInstance != BlueprintComplex.ComponentsArray[index])
            {
                PFLog.Default.Error("Component wrappers mismatch");
                UpdateComponentsWrappers();
                return;
            }

            var component = comTgt.Component;
            bool disabled = comTgt.Component.Disabled;
            using (new EditorGUILayout.VerticalScope())
            {
                bool safeForDelete =
                    targets.Length == 1 &&
                    ((BlueprintComplex.ComponentsArray[index] as IOverrideOnActivateMethod)?.IsOverrideOnActivateMethod ?? false);

                if (!comTgt)
                {
                    using (new EditorGUILayout.HorizontalScope("IN Title"))
                    {
                        // draw name
                        GUILayout.Label("<Differnt component types>", "IN TitleText");
                    }
                    return;
                }
                Profiler.BeginSample("ComponentInspector");

                var p = componentsArray.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!disabled)
                    {
                        p.isExpanded = GUILayout.Toggle(
                            p.isExpanded,
                            "",
                            EditorStyles.foldout,
                            GUILayout.ExpandWidth(false));
                        //{
                        //	p.isExpanded = !p.isExpanded;
                        //}
                    }

                    // draw name
                    string labelText = component.GetType().Name;
                    if (component.GetType().HasAttribute<ObsoleteAttribute>())
                    {
                        labelText = $"OBSOLETE {labelText}";
                    }

                    if (disabled)
                    {
                        labelText = $"DISABLED {labelText}";
                    }
                    
                    if (!safeForDelete)
                    {
                        labelText = $"[!] {labelText}";
                    }
                    
                    var label = new GUIContent(labelText);
                    
                    // todo: [bp] validation
                    //if (m_CompTargets[0].ValidationStatus.HasErrors)
                    //{
                    //    label.image = EditorGUIUtility.FindTexture("console.erroricon.sml");
                    //}
                    GUILayout.Label(label, "IN TitleText", GUILayout.ExpandWidth(false));

                    if ((Blueprint is BlueprintUnitFact) || (Blueprint is BlueprintUnitLoot))
                    {
                        p.isExpanded = true;
                    }

                    p.isExpanded &= !disabled;

                    if (!p.isExpanded)
                    {
                        if (targets.Length == 1)
                        {
                            var cmnt = m_ComponentsWrapped[index].FindProperty("Component.Comment");
                            if (cmnt != null)
                            {
                                GUILayout.Label(cmnt.stringValue, EditorStyles.miniLabel);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Space(4);

                        // this button simply indicates inherited component. It does not do anything
                        if (component.PrototypeLink != null)
                        {
                            GUILayout.Space(4);
                            var gc = new GUIContent(""); // todo: this used to display component source BP
                            GUILayout.Box(gc, OwlcatEditorStyles.Instance.OverrideButton, GUILayout.Width(16));
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (component is IRuntimeEntityFactComponentProvider)
                    {
                        bool enabled = !component.Disabled;
                        enabled = GUILayout.Toggle(enabled, GUIContent.none);
                        if (component.Disabled != !enabled)
                        {
                            component.Disabled = !enabled;
                            BlueprintsDatabase.SetDirty(Blueprint.AssetGuid);
                        }
                    }

                    DrawReorderButtons(componentsArray, index);

                    if (KnowledgeDatabaseSearch.GetDescription(p) is {} || KnowledgeDatabaseSearch.GetCodeDescription(p) is {})
                    {
                        PrototypedObjectEditorUtility.DrawDescriptionButton(p);
                    }

                    // draw settings button (reset/revert/etc)
                    if (GUILayout.Button("", OwlcatEditorStyles.Instance.SettingsButton, GUILayout.Width(16)))
                    {
                        ShowComponentContextMenu(componentsArray, index);
                    }

                    GUILayout.Space(4);
                }

                {
                    string description = targets.Length == 1
                        ? component.name.Split('$').LastOrDefault()?.Replace("(Clone)", "")
                        : m_ComponentsWrapped[index].targetObjects.Length + " components";
                    if (description != null)
                    {
                        GUILayout.Label(description, GUILayout.ExpandWidth(false));
                    }
                }

                if (p.isExpanded)
                {
                    var type = FieldFromProperty.GetActualValueType(p);
                    BlueprintEditorUtility.ShowType("Script", type);

                    var infoAttributes = type.GetAttributes<ClassInfoBox>();
                    foreach (var info in infoAttributes)
                    {
                        EditorGUILayout.HelpBox(new GUIContent(info.Text));
                    }

                    Profiler.BeginSample("ComponentInspector onGUI");
                    using (GuiScopes.UpdateObject(m_ComponentsWrapped[index]))
                    {
                        PrototypedObjectEditorUtility.ShowPropertyChildren(
                            m_ComponentsWrapped[index].FindProperty(nameof(BlueprintComponentEditorWrapper.Component)));
                        //editor.OnInspectorGUI();
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("BlueprintInspector.DrawActionsMenu()");
                    //DrawActionsForObject(p.objectReferenceValue);
                    Profiler.EndSample();
                }

                Profiler.EndSample();
            }
        }

        private static void DrawReorderButtons(SerializedProperty componentsArray, int index)
        {
            if (componentsArray.arraySize > 1)
            {
                int newIndex = index;

                if (GUILayout.Button("↑", GUILayout.Width(15)))
                {
                    newIndex = index - 1;
                }

                if (GUILayout.Button("↓", GUILayout.Width(15)))
                {
                    newIndex = index + 1;
                }

                if (index != newIndex && newIndex >= 0 && newIndex < componentsArray.arraySize)
                {
                    var p1 = componentsArray.GetArrayElementAtIndex(index);
                    var p2 = componentsArray.GetArrayElementAtIndex(newIndex);
                    (p1.isExpanded, p2.isExpanded) = (p2.isExpanded, p1.isExpanded);

                    componentsArray.MoveArrayElement(index, newIndex);
                    componentsArray.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void ShowComponentContextMenu(SerializedProperty componentsArray, int index)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove"), false, () => { RemoveComponent(index); });
            menu.AddItem(new GUIContent("Copy"), false, () => { CopyPasteController.CopyProperty(componentsArray.GetArrayElementAtIndex(index), null); });
            menu.AddItem(new GUIContent("Solo"), m_SoloIndex == index,
                () =>
                {
                    m_SoloIndex = m_SoloIndex < 0 ? index : -1;
                    if (m_SoloIndex >= 0)
                    {
                        Selection.activeObject = m_ComponentsWrapped[m_SoloIndex].targetObject;
                    }
                });
            menu.ShowAsContext();
        }

        private void RemoveComponent(int idx)
        {
            foreach (BlueprintEditorWrapper bw in targets)
            {
                var bp = (BlueprintScriptableObject)bw.Blueprint;
                var component = bp.ComponentsArray[idx];

                if (component.PrototypeLink)
                {
                    // sometimes components can be null, maybe the object creation glitched or something
                    bp.SetOverridden(component.PrototypeLink.name, true);
                }

                bp.ComponentsArray = bp.ComponentsArray.Where(c => c != component).ToArray();
                bp.SetDirty();
                bp.Cleanup();
                serializedObject.Update();
                UpdateComponentsWrappers();
            }
        }

        private void AddComponentFromMenu(Type userdata)
        {
            serializedObject.ApplyModifiedProperties();
            BlueprintComplex.AddComponentFromMenu(userdata);
            serializedObject.Update();
            UpdateComponentsWrappers();
        }

        public static void DrawActionsForObject(object obj)
        {
            var allMethods = obj.GetType()
                .GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod |
                    BindingFlags.Static);

            var buttons = new List<KeyValuePair<string, Action>>();
            foreach (var method in allMethods)
            {
                var attribute =
                    (BlueprintButtonAttribute)method.GetCustomAttributes(typeof(BlueprintButtonAttribute), true).FirstOrDefault();

                if (method.GetGenericArguments().Length > 0)
                {
                    continue;
                }

                if (attribute != null)
                {
                    var m = method;
                    var methodName = string.IsNullOrEmpty(attribute.Name) ? method.Name : attribute.Name;
                    buttons.Add(new KeyValuePair<string, Action>(methodName, () => m.Invoke(m.IsStatic ? null : obj, null)));
                }
            }
            if (buttons.Count > 0)
            {
                using (new GUILayout.VerticalScope())
                {
                    const int buttonsPerLine = 3;
                    for (int i = 0; i < buttons.Count; ++i)
                    {
                        if (i % buttonsPerLine == 0)
                        {
                            GUILayout.BeginHorizontal();
                        }

                        if (GUILayout.Button(buttons[i].Key))
                        {
                            buttons[i].Value.Invoke();
                        }

                        if (i % buttonsPerLine == 2 || i == buttons.Count - 1)
                        {
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
        }
    }
}