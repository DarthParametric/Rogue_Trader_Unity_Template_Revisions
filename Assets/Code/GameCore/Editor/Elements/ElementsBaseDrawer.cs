#if UNITY_EDITOR
using JetBrains.Annotations;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility.UnityExtensions;
using Owlcat.Editor.Utility;
using System;
using System.Collections.Generic;
using Code.GameCore.Editor.Elements.Debug;
using Code.GameCore.ElementsSystem;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.EntitySystem.Properties;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.UnitLogic.Progression;
using Kingmaker.UnitLogic.Progression.Prerequisites;
// using Kingmaker.Editor.Arbiter;
// using Kingmaker.QA.Arbiter;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Owlcat.Editor.Core.Utility;
using Owlcat.Runtime.Core.Utility;

namespace Kingmaker.Editor.Elements
{
    public abstract class ElementsBaseDrawer : PropertyDrawer
    {
        private static readonly HashSet<string> s_FoldoutFields = new HashSet<string>();

        internal static bool RecursiveFoldout;

        internal static bool RecursiveFoldoutValue;

        private static UnityEditor.Editor m_CachedEditor;

        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var elementType = GetElementType(property);
            if (elementType != null)
                ElementDragAndDropController.OnBeforeElementGUI(elementType);

            try
            {
                HandleOnGUI(elementType, position, property, label);
                property.isExpanded = false;
            }
            finally
            {
                ElementDragAndDropController.OnAfterElementGUI();
            }
        }

        protected abstract void HandleOnGUI(
            Type elementType, Rect position, SerializedProperty property, GUIContent label);

        [CanBeNull]
        protected abstract Type GetElementType(SerializedProperty property);

        private string MakeFoldoutId(Object element)
        {
            return AssetDatabase.GetAssetPath(element) + element.name;
        }
        
        private string MakeFoldoutId(Element element)
        {
            return BlueprintsDatabase.GetAssetPath((SimpleBlueprint)element.Owner) + element.name;
        }

        private string MakeFoldoutId(SimpleBlueprint bp)
        {
            return BlueprintsDatabase.GetAssetPath(bp);
        }

        protected string MakeFoldoutId(SerializedProperty property)
        {
            var bp = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(property.serializedObject.targetObject);
            return MakeFoldoutId(bp) + property.propertyPath;
        }

        protected bool GetFoldoutStatus(Element element)
        {
            var id = MakeFoldoutId(element);
            return s_FoldoutFields.Contains(id);
        }

        protected FoldoutResult Foldout(
            SerializedProperty property, int listSize = -1, GUIStyle guiStyle = null, bool showContextMenu = false)
        {
            string caption = property.displayName;
            if (listSize >= 0)
                caption += " (" + listSize + ")";
            return Foldout(property, MakeFoldoutId(property), caption, null, 140, null, guiStyle);
        }

        protected FoldoutResult Foldout(
            SerializedProperty property,
            string addCaption = null,
            string description = null,
            GUIStyle guiStyle = null,
            bool showContextMenu = false)
        {
            string caption = property.displayName + addCaption;
            return Foldout(property, MakeFoldoutId(property), caption, null, 250, description, guiStyle);
        }

        protected FoldoutResult Foldout(
            SerializedProperty property,
            string addCaption,
            string tooltip,
            string description = null,
            GUIStyle guiStyle = null,
            bool showContextMenu = false)
        {
            string caption = property.displayName + addCaption;
            return Foldout(property, MakeFoldoutId(property), caption, tooltip, 250, description, guiStyle);
        }

        protected FoldoutResult Foldout(
            SerializedProperty property, string propertyName, Element element, GUIStyle guiStyle = null,
            bool showContextMenu = false)
        {
	        var debugInfo = ElementsDebuggerDatabase.Get(element);
	        
            Color oldColor = GUI.color;
            GUI.color = debugInfo == null
				? element.GetCaptionColor()
				: debugInfo.LastException != null
					 ? Color.red
					 : debugInfo.LastResult == 0 && element is Condition
						? Color.blue
						: Color.green;
            string caption = element.ToString();
            if (propertyName != "")
            {
                caption = propertyName + ": " + caption;
            }

            var foldout = Foldout(
                property, MakeFoldoutId(element), caption, null, 300, element.GetDescriptionSafe(), guiStyle, showContextMenu);

            GUI.color = oldColor;
            return foldout;
        }

        protected void DrawList(Color outlineColor, Type type, SerializedProperty list, bool instantAdd = false, bool useTypeAsTitle = false)
        {
            var p1 = GUILayoutUtility.GetLastRect().position;
            p1.x = EditorGUI.indentLevel * 15f - 6;
            p1.y += 20;

            CompactList(list);

            for (int i = 0; i < list.arraySize; ++i)
            {
                var element = list.GetArrayElementAtIndex(i);
                GUIContent label = null;
                if (useTypeAsTitle)
                {
                    label = new GUIContent(FieldFromProperty.GetActualValueType(element).Name);
                }

                DrawElement(type, element, list, i, label);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            if (instantAdd)
                DrawInstantAddButton(type, list);
            else
                DrawAddButton(type, list);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            var p2 = GUILayoutUtility.GetLastRect().position;
            p2.x = EditorGUI.indentLevel * 15f - 6;
            p2.y -= 8;

            if (list.arraySize > 0)
            {
                Color oldColor = Handles.color;
                Handles.color = outlineColor;
                Handles.DrawLine(p1, p2);
                Handles.color = oldColor;
            }
        }

        protected void DrawAddButton(Type type, SerializedProperty list)
        {
            bool nodeEditor = NodeEditorBase.Drawing;
            RobustSerializedProperty rsp = list;
            TypePicker.Button(
                "Add",
                () => TypeUtility.GetValidTypes(rsp, type),
                selectedType =>
                {
                    AddElementFromMenu(rsp, selectedType);
                    if (nodeEditor)
                        BlueprintNodeEditor.CheckForNewNodes();
                }
            );
        }

        protected void DrawInstantAddButton(Type type, SerializedProperty list)
        {
            bool nodeEditor = NodeEditorBase.Drawing;
            RobustSerializedProperty rsp = list;
            if (GUILayout.Button("Add"))
            {
                AddElementFromMenu(rsp, type);
                if (nodeEditor)
                    BlueprintNodeEditor.CheckForNewNodes();
            }
        }

        protected void DrawElement(
            Type type, SerializedProperty property, [CanBeNull] SerializedProperty parentList, int index,
            GUIContent label = null)
        {
            // todo: [bp] draw element property
            
            Element element = FieldFromProperty.GetFieldValue(property) as Element;

            var rsp = new RobustSerializedProperty(property);

            var elementType = GetElementType(property);
            bool markDroppable = ElementDragAndDropController.HasFactories(elementType) && !rsp.Property.IsArrayElement();
            if (element == null)
            {
                GUILayout.BeginHorizontal(markDroppable ? ElementDragAndDropController.PreDropStyle : GUIStyle.none);

                GUILayout.Space(EditorGUI.indentLevel * 15f);

                var style = new GUIStyle(EditorStyles.label);
                style.active = style.normal;

                var id = MakeFoldoutId(property);
                GUI.SetNextControlName(id);
                if (GUILayout.Button(label?.text ?? property.displayName, style))
                {
                    GUI.FocusControl(id);
                }

                var focused = GUI.GetNameOfFocusedControl() == id;
                // mark focus
                if (focused)
                {
                    var controlRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.DrawRect(new Rect(controlRect.xMin, controlRect.yMax, controlRect.width, 1),
                        new Color(0, 0.5f, 1, 0.6f));
                }

                GUILayout.FlexibleSpace();
                TypePicker.Button(
                    "Create",
                    () => TypeUtility.GetValidTypes(rsp, type),
                    selectedType => AddElementFromMenu(rsp, selectedType)
                );

                GUILayout.EndHorizontal();
                ProcessDragAndDrop(elementType, null, rsp, GUILayoutUtility.GetLastRect());

                if (focused)
                    CopyPasteController.Process(type, property);

                return;
            }

            string propertyName = label?.text ?? (parentList != null ? "" : property.displayName);
            var guiStyle = markDroppable
                ? ElementDragAndDropController.PreDropStyle
                : null;
            
	        (FoldoutResult foldout, Rect rect) = DrawHeader(property, propertyName, element, parentList, index, guiStyle);

            ProcessDragAndDrop(elementType, null, rsp, rect);

            if (GUI.GetNameOfFocusedControl() == MakeFoldoutId(element))
                CopyPasteController.Process(type, property);

            using (var content = ContentScope(foldout))
            {
                if (content.Foldout)
                {
                    using (var scope = new EditorGUILayout.VerticalScope())
                    {
                        if (scope.rect.height > 4)
                        {
                            Rect boxRect = scope.rect;
                            boxRect.x = EditorGUI.indentLevel * 15f;
                            GUI.Box(boxRect, "");
                        }

                        ShowPropertyChildren(property);
                    }
                }
            }
        }

        protected virtual bool ShowPropertyChildren(SerializedProperty property)
        {
	        return PrototypedObjectEditorUtility.ShowPropertyChildren(property);
        }

        protected void ProcessDragAndDrop(
            Type elementType,
            SimpleBlueprint owner,
            RobustSerializedProperty rsp,
            Rect rect,
            bool clearProperty = true
        )
        {
            //Requested by game designers. Array elements can not be replaced by drag and drop.
            if (rsp.Property.IsArrayElement()) return;
            if (!ElementDragAndDropController.HasProperElementDropped(elementType, rect)) return;

            var factories = ElementDragAndDropController.GetFactories(elementType);
            if (factories.Count == 0)
                return;

            if (factories.Count == 1)
            {
                OverrideProperty(owner, factories[0], rsp, clearProperty);
            }
            else
            {
                ElementFactoryWithSourcePicker.Show(
                    rect,
                    "Pick result",
                    () => factories,
                    selectedFactory => OverrideProperty(owner, selectedFactory, rsp, clearProperty)
                );
            }

            if (NodeEditorBase.Drawing)
                BlueprintNodeEditor.CheckForNewNodes();
        }

        private void OverrideProperty(
            SimpleBlueprint owner,
            ElementFactoryWithSource factory,
            RobustSerializedProperty rsp,
            bool clearProperty)
        {
            if (clearProperty)
                ClearProperty(rsp);

            UpdateProperty(owner, rsp, factory);
        }

        private void UpdateProperty(
            SimpleBlueprint owner, RobustSerializedProperty rsp, ElementFactoryWithSource factoryWithSource)
        {
            var element = factoryWithSource.Factory.Create(owner, factoryWithSource.Source);
            UpdateProperty(rsp, element);
        }

        private (FoldoutResult, Rect) DrawHeader(
            SerializedProperty property,
            string title,
            Element element,
            [CanBeNull] SerializedProperty parentList,
            int index,
            GUIStyle guiStyle = null)
        {
	        if (element.GetType().HasAttribute<ObsoleteAttribute>())
	        {
		        title = $"OBSOLETE {title}";
	        }

	        using var s = new EditorGUILayout.HorizontalScope();
	        Rect rect = s.rect;
	        var foldout = Foldout(property, title, element, guiStyle, true);

	        var condition = element as Condition;
	        if (condition != null)
	        {
		        using (GuiScopes.FixedWidth(30, EditorPreferences.Instance.BigCheckbox ? 32 : 16))
		        {
			        EditorGUILayout.PropertyField(property.FindPropertyRelative("Not"));
		        }
	        }

	        var guid = element.AssetGuidShort;
	        if (!guid.IsNullOrEmpty())
	        {
		        GUILayout.Box(guid, GUILayout.Width(40));
		        GUILayout.FlexibleSpace();
	        }

	        var mark = element is ContextAction or ContextCondition or Conditional
		        ? "[C]"
		        : null;
	        if (mark != null)
	        {
		        GUILayout.Box(mark, GUILayout.Width(25));
		        GUILayout.FlexibleSpace();
	        }

	        GUILayout.FlexibleSpace();

	        if (GUILayout.Button("Del", GUILayout.ExpandWidth(false)))
	        {
		        DestroyElement(element);
		        ClearProperty(property);
		        if (parentList != null)
		        {
			        parentList.DeleteArrayElementAtIndex(index);
		        }
		        property.serializedObject.ApplyModifiedProperties();

		        GUIUtility.ExitGUI();
		        return (new FoldoutResult(false), rect);
	        }

	        if (parentList != null && parentList.arraySize > 1)
	        {
		        if (GUILayout.Button("↑", GUILayout.Width(15)))
		        {
			        if (index > 0)
			        {
				        parentList.MoveArrayElement(index, index - 1);
			        }
		        }

		        if (GUILayout.Button("↓", GUILayout.Width(15)))
		        {
			        if (index < parentList.arraySize - 1)
			        {
				        parentList.MoveArrayElement(index, index + 1);
			        }
		        }
	        }
	        
	        PrototypedObjectEditorUtility.DrawDescriptionButton(property);

	        return (foldout, rect);
        }

        private FoldoutResult Foldout(
			SerializedProperty property, 
			string id, 
			string caption, 
            string tooltip,
			float width, 
			string description = null, 
			GUIStyle guiStyle = null,
			bool showContextMenu = false)
		{
			GUILayout.Space(EditorGUI.indentLevel * 15f);

			var style = guiStyle ?? EditorStyles.foldout;
			using (GuiScopes.FixedWidth(0, width))
			{
				var captionSettings = GetResultAndColor(property);
				if (!captionSettings.result.IsNullOrEmpty())
					caption = $"[{captionSettings.result}] {caption}";
				
				GUI.SetNextControlName(id);
				if (NodeEditorBase.Drawing)
				{
					using (GuiScopes.Color(captionSettings.color))
						EditorGUILayout.Foldout(true, caption, true, style);

					return new FoldoutResult(true);
				}
				else
				{
					bool startRecursive = Event.current.alt && !RecursiveFoldout;
					bool oldFoldoutValue = s_FoldoutFields.Contains(id);
					
					bool foldoutValue;
					using (GuiScopes.Color(captionSettings.color))
						foldoutValue = EditorGUILayout.Foldout(oldFoldoutValue, new GUIContent(caption, tooltip), true, style);

					var controlRect = GUILayoutUtility.GetLastRect();

                    // mark focus
                    if (GUI.GetNameOfFocusedControl() == id)
                    {
                        EditorGUI.DrawRect(new Rect(controlRect.xMin,controlRect.yMax,controlRect.width,1), new Color(0, 0.5f, 1, 0.6f));
                    }
                    // mark copied element
                    // hack: if showing elements list, the copied prop would be the actual list
                    var copiedProp =
                        property.type == nameof(ActionList)
                            ? property.FindPropertyRelative(nameof(ActionList.Actions))
                            : property.type == nameof(ConditionsChecker)
                                ? property.FindPropertyRelative(nameof(ConditionsChecker.Conditions))
                                : property;
                    if (CopyPasteController.IsThisCopied(copiedProp))
                    {
                        EditorGUI.DrawRect(controlRect, new Color(0.1f,1,0.1f,0.1f));
                    }

					if (description != null)
					{
						GUI.Label(controlRect, new GUIContent("", description));
					}

					if (RecursiveFoldout)
					{
						foldoutValue = RecursiveFoldoutValue;
					}

					if (oldFoldoutValue != foldoutValue)
					{
						if (foldoutValue)
							s_FoldoutFields.Add(id);
						else
							s_FoldoutFields.Remove(id);

						if (startRecursive)
						{
							return new FoldoutResult(foldoutValue, true);
						}
					}

					return new FoldoutResult(foldoutValue);
				}
			}
		}

		protected static ContentScope ContentScope(FoldoutResult foldout)
		{
			return new ContentScope(foldout);
		}

		private static void CompactList(SerializedProperty list)
		{
		}

		private void AddElementFromMenu(RobustSerializedProperty property, Type type)
		{
            var element = Element.CreateInstance(type);
            element.Owner = BlueprintsDatabase.LoadById<SimpleBlueprint>(
                (property.serializedObject.targetObject as ScriptableWrapperBase)?.GetOwnerBlueprintId());
            ElementWorkspaceContextualPopulationController.PrefillWithTargets(element, element.Owner);
			UpdateProperty(property, element);
		}

		private static void ClearProperty(RobustSerializedProperty property)
		{
            property.Property.managedReferenceValue = null;
        }

            private static void DestroyElement(Element element)
		{
			element.Delete();
		}

		private void UpdateProperty(RobustSerializedProperty property, Element element)
		{
			using (GuiScopes.UpdateObject(property.serializedObject))
			{
				if (property.Property.isArray)
				{
					property.Property.arraySize++;
                    property.serializedObject.ApplyModifiedProperties();
                    FieldFromProperty.SetFieldValue(property.Property.GetArrayElementAtIndex(property.Property.arraySize - 1), element);
                    property.serializedObject.Update();
                        //property.Property.GetArrayElementAtIndex(property.Property.arraySize - 1).managedReferenceValue = element;
                }
				else
				{
                    property.serializedObject.ApplyModifiedProperties();
                    FieldFromProperty.SetFieldValue(property.Property, element);
                    property.serializedObject.Update();
                }
                s_FoldoutFields.Add(MakeFoldoutId(element));
			}
		}

		private static (string result, Color color) GetResultAndColor(SerializedProperty property)
		{
			object value = FieldFromProperty.GetFieldValue(property);
			switch (value)
			{
				case ElementsList list:
					var listInfo = ElementsDebuggerDatabase.Get(list);
					return GetResultAndColor(list, listInfo?.LastResult, listInfo?.LastException);
				case Element element:
					var elementInfo = ElementsDebuggerDatabase.Get(element);
					return GetResultAndColor(element, elementInfo?.LastResult, elementInfo?.LastException);
				default:
					return ("", GUI.color);
			}
		}

		public static (string result, Color color) GetResultAndColor(
			[NotNull] ElementsList list, int? result, [CanBeNull] Exception exception)
		{
			if (exception == null && result == null)
				return ("", GUI.color);
			
			if (exception != null)
				return ("exception", Color.red);

			if (list is ConditionsChecker or PrerequisitesList or PropertyCalculator {IsBool: true})
				return result == 0 ? ("false", Color.yellow) : ("true", Color.green);

			if (list is PropertyCalculator)
				return (result.ToString(), Color.green);

			return ("success", Color.green);
		}

		public static (string result, Color color) GetResultAndColor(
			[NotNull] Element element, int? result, [CanBeNull] Exception exception)
		{
			if (exception == null && result == null)
				return ("", GUI.color);
			
			if (exception != null)
				return ("exception", Color.red);

			if (element is Condition or Prerequisite)
				return result == 0 ? ("false", Color.yellow) : ("true", Color.green);

			if (element is PropertyGetter or IntEvaluator)
				return (result.ToString(), Color.green);

			return ("success", Color.green);
		}
    }

	public struct FoldoutResult
	{
		public readonly bool Value;
		public readonly bool StartRecursive;

		public FoldoutResult(bool value) : this()
		{
			Value = value;
			StartRecursive = false;
		}

		public FoldoutResult(bool value, bool startRecursive)
		{
			Value = value;
			StartRecursive = startRecursive;
		}
	}

	public class ContentScope : IDisposable
	{
		private readonly FoldoutResult m_Foldout;

		public bool Foldout
			=> m_Foldout.Value;

		public ContentScope(FoldoutResult foldout)
		{
			m_Foldout = foldout;
			if (foldout.StartRecursive)
			{
				ElementsBaseDrawer.RecursiveFoldout = true;
				ElementsBaseDrawer.RecursiveFoldoutValue = foldout.Value;
			}
		}

		public void Dispose()
		{
			if (m_Foldout.StartRecursive)
			{
				ElementsBaseDrawer.RecursiveFoldout = false;
			}
		}
	}
	
	internal static class SerializedPropertyHelper
	{
		public static bool IsArrayElement(this SerializedProperty property) 
			=> property.propertyPath.Contains("Array");
	}
}
#endif