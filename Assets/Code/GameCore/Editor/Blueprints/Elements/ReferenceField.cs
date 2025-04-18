// Decompiled ObjectField and made this
using System;
using System.Linq;
using System.Reflection;
using Assets.Editor;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints.Elements
{
    public class ReferenceField : BaseField<string>
    {
        private Type m_objectType;
        private readonly ReferenceFieldDisplay m_ReferenceFieldDisplay;
        /// <summary>
        ///   <para>USS class name of elements of this type.</para>
        /// </summary>
        public new static readonly string ussClassName = "unity-object-field"; // reuse visuals from unity object field
        /// <summary>
        ///   <para>USS class name of labels in elements of this type.</para>
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        ///   <para>USS class name of input elements in elements of this type.</para>
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        ///   <para>USS class name of object elements in elements of this type.</para>
        /// </summary>
        public static readonly string objectUssClassName = ussClassName + "__object";
        /// <summary>
        ///   <para>USS class name of selector elements in elements of this type.</para>
        /// </summary>
        public static readonly string selectorUssClassName = ussClassName + "__selector";

        private SimpleBlueprint m_ReferencedBlueprint;

        private VisualElement VisualInput { get; }

        public SimpleBlueprint Blueprint
            => m_ReferencedBlueprint;

        public override void SetValueWithoutNotify(string newValue)
        {
            bool flag = newValue!=value;
            base.SetValueWithoutNotify(newValue);
            if (!flag)
                return;
            m_ReferencedBlueprint = BlueprintsDatabase.LoadById<SimpleBlueprint>(value);
            m_ReferenceFieldDisplay.Update();
        }

        /// <summary>
        ///   <para>The type of the objects that can be assigned.</para>
        /// </summary>
        public Type objectType
        {
            get => m_objectType;
            set
            {
                if (m_objectType == value)
                    return;
                m_objectType = value;
                m_ReferenceFieldDisplay.Update();
            }
        }

        /// <summary>
        ///   <para>Constructor.</para>
        /// </summary>
        public ReferenceField()
          : this((string)null)
        {
        }

        public ReferenceField(string label)
          : base(label, (VisualElement)null)
        {
            var info = typeof(ReferenceField).GetMethod("get_visualInput", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
            VisualInput = (VisualElement)info.Invoke(this, null);

            VisualInput.focusable = false;
            labelElement.focusable = false;
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            ReferenceFieldDisplay referenceFieldDisplay = new ReferenceFieldDisplay(this);
            referenceFieldDisplay.focusable = true;
            m_ReferenceFieldDisplay = referenceFieldDisplay;
            m_ReferenceFieldDisplay.AddToClassList(objectUssClassName);
            ObjectFieldSelector objectFieldSelector = new ObjectFieldSelector(this);
            objectFieldSelector.AddToClassList(selectorUssClassName);
            VisualInput.AddToClassList(inputUssClassName);
            VisualInput.Add((VisualElement)m_ReferenceFieldDisplay);
            VisualInput.Add((VisualElement)objectFieldSelector);
        }

        internal void ShowObjectSelector()
        {
            // todo: we should probably pull the fieldInfo here too
            BlueprintPicker.ShowAssetPicker(objectType, null, b => value = b?.AssetGuid ?? "", m_ReferencedBlueprint);
        } 
            //ObjectSelector.get.Show(this.value, this.objectType, (SerializedProperty)null, this.allowSceneObjects, (List<int>)null, (Action<UnityEngine.Object>)null, new Action<UnityEngine.Object>(this.OnObjectChanged));


        /// <summary>
        ///   <para>Instantiates an ObjectField using the data read from a UXML file.</para>
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ReferenceField, UxmlTraits>
        {
        }

        /// <summary>
        ///   <para>UxmlTraits for the ObjectField.</para>
        /// </summary>
        public new class UxmlTraits : BaseField<UnityEngine.Object>.UxmlTraits
        {
            private UxmlBoolAttributeDescription m_AllowSceneObjects;

            /// <summary>
            ///   <para>Initialize ObjectField properties using values from the attribute bag.</para>
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
            }

            /// <summary>
            ///   <para>Constructor.</para>
            /// </summary>
            public UxmlTraits()
            {
            }
        }

        private class ReferenceFieldDisplay : VisualElement
        {
            private readonly ReferenceField m_ReferenceField;
            private readonly Image m_ObjectIcon;
            private readonly Label m_ObjectLabel;
            public static readonly string ussClassName = "unity-object-field-display";
            public static readonly string iconUssClassName = ussClassName + "__icon";
            public static readonly string labelUssClassName = ussClassName + "__label";
            public static readonly string acceptDropVariantUssClassName = ussClassName + "--accept-drop";

            public ReferenceFieldDisplay(ReferenceField referenceField)
            {
                AddToClassList(ussClassName);
                Image image = new Image();
                image.scaleMode = ScaleMode.ScaleAndCrop;
                image.pickingMode = PickingMode.Ignore;
                m_ObjectIcon = image;
                m_ObjectIcon.AddToClassList(iconUssClassName);
                Label label = new Label();
                label.pickingMode = PickingMode.Ignore;
                m_ObjectLabel = label;
                m_ObjectLabel.AddToClassList(labelUssClassName);
                m_ReferenceField = referenceField;
                Update();
                Add((VisualElement)m_ObjectIcon);
                Add((VisualElement)m_ObjectLabel);
            }

            public void Update()
            {
                m_ObjectIcon.image = m_ReferenceField.m_ReferencedBlueprint ? OwlcatEditorStyles.Instance.BlueprintItemIcon: null;
                var n= m_ReferenceField.m_ReferencedBlueprint == null
                    ? string.IsNullOrEmpty(m_ReferenceField.value)
                        ? "None"
                        : "Missing " + m_ReferenceField.value
                    :m_ReferenceField.m_ReferencedBlueprint.name;
                m_ObjectLabel.text =
                    $"{n} [{(m_ReferenceField.m_ReferencedBlueprint?.GetType() ?? m_ReferenceField?.objectType)?.Name}]";
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);
                if (evt == null)
                    return;
                // ISSUE: explicit non-virtual call
                if (evt is MouseDownEvent mouseDownEvent && (mouseDownEvent.button) == 0)
                    OnMouseDown(evt as MouseDownEvent);
                else if (evt.eventTypeId == EventBase<KeyDownEvent>.TypeId())
                {
                    KeyDownEvent keyDownEvent1 = evt as KeyDownEvent;
                    if ((evt is KeyDownEvent keyDownEvent ? ((keyDownEvent.keyCode) == KeyCode.Space ? 1 : 0) : 0) != 0 
                        || (evt is KeyDownEvent keyDownEvent3 ? ((keyDownEvent3.keyCode) == KeyCode.KeypadEnter ? 1 : 0) : 0) != 0 
                        || evt is KeyDownEvent keyDownEvent2 && (keyDownEvent2.keyCode) == KeyCode.Return)
                    {
                        OnKeyboardEnter();
                    }
                    else
                    {
                        if (keyDownEvent1.keyCode != KeyCode.Delete && keyDownEvent1.keyCode != KeyCode.Backspace)
                            return;
                        OnKeyboardDelete();
                    }
                }
                else if (evt.eventTypeId == EventBase<DragUpdatedEvent>.TypeId())
                    OnDragUpdated(evt);
                else if (evt.eventTypeId == EventBase<DragPerformEvent>.TypeId())
                {
                    OnDragPerform(evt);
                }
                else
                {
                    if (evt.eventTypeId != EventBase<DragLeaveEvent>.TypeId())
                        return;
                    OnDragLeave();
                }
            }

            private void OnDragLeave() => RemoveFromClassList(acceptDropVariantUssClassName);

            private void OnMouseDown(MouseDownEvent evt)
            {
                var bp = m_ReferenceField.m_ReferencedBlueprint;
                UnityEngine.Object gameObject = bp ? BlueprintEditorWrapper.Wrap(bp) : null;
                if (gameObject == null)
                    return;
                
                if (evt.clickCount == 1)
                {
                    if (!evt.shiftKey && !evt.ctrlKey && (bool)gameObject)
                    {
                        BlueprintProjectView.Ping(bp);
                    }

                    evt.StopPropagation();
                }
                else
                {
                    if (evt.clickCount != 2)
                        return;
                    if ((bool)gameObject)
                    {
                        AssetDatabase.OpenAsset(gameObject); // todo: does this even make any sense?
                        GUIUtility.ExitGUI();
                    }
                    evt.StopPropagation();
                }
            }

            private void OnKeyboardEnter() => m_ReferenceField.ShowObjectSelector();

            private void OnKeyboardDelete() => m_ReferenceField.value = "";

            private UnityEngine.Object DNDValidateObject()
            {
                var dragged = DragAndDrop.objectReferences.FirstOrDefault();
                if(dragged is BlueprintEditorWrapper)
                {
                    // todo: check that wrapped type matches the reference type
                    return dragged;
                }
                return null; 
                //UnityEngine.Object target = EditorGUI.ValidateObjectFieldAssignment(DragAndDrop.objectReferences, this.m_ReferenceField.objectType, (SerializedProperty)null, EditorGUI.ObjectFieldValidatorOptions.None);
                //if (target != (UnityEngine.Object)null && (!this.m_ReferenceField.allowSceneObjects && !EditorUtility.IsPersistent(target)))
                //    target = (UnityEngine.Object)null;
                //return target;
            }

            private void OnDragUpdated(EventBase evt)
            {
                if (!(DNDValidateObject() != (UnityEngine.Object)null))
                    return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                AddToClassList(acceptDropVariantUssClassName);
                evt.StopPropagation();
            }

            private void OnDragPerform(EventBase evt)
            {
                UnityEngine.Object @object = DNDValidateObject();
                if (!(@object != (UnityEngine.Object)null))
                    return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                m_ReferenceField.value =BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(@object)?.AssetGuid??"";
                DragAndDrop.AcceptDrag();
                RemoveFromClassList(acceptDropVariantUssClassName);
                evt.StopPropagation();
            }
        }

        private class ObjectFieldSelector : VisualElement
        {
            private readonly ReferenceField m_ReferenceField;

            public ObjectFieldSelector(ReferenceField referenceField) => m_ReferenceField = referenceField;

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);
                if (!(evt is MouseDownEvent mouseDownEvent) || (mouseDownEvent.button) != 0)
                    return;
                m_ReferenceField.ShowObjectSelector();
            }
        }
    }
}
