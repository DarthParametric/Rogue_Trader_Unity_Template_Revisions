using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Blueprints.Creation;
using Kingmaker.Editor.Blueprints.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.Utility;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    public class BlueprintReferenceProperty : OwlcatProperty
    {
        public event Action OnValueChangedEvent = delegate { };

        private ReferenceField m_Field;

        private readonly RobustSerializedProperty m_GuidProp;

        private bool m_IsHaveNewButton;

        private bool m_IsNicolayButton;

        private string m_DefaultCreatedName;

        private string m_DefaultCreationPath;

        private int m_EventCount;

        public BlueprintReferenceProperty(SerializedProperty property, FieldInfo fieldInfo, RobustSerializedProperty guidProperty, System.Type refType = null) : base(property)
        {
            m_GuidProp = guidProperty;
            Init(fieldInfo, refType);
        }

        public BlueprintReferenceProperty(SerializedProperty property, FieldInfo fieldInfo, string guidName = "guid", System.Type refType = null) : base(property)
        {
            m_GuidProp = new RobustSerializedProperty(property.FindPropertyRelative(guidName));
            Init(fieldInfo, refType);
        }

        private void Init(FieldInfo fieldInfo, System.Type refType = null)
        {
            var guid = m_GuidProp.Property.stringValue;
            SimpleBlueprint asset = BlueprintsDatabase.LoadById<SimpleBlueprint>(guid);

            if (refType == null)
            {
                var type = BlueprintLinkDrawer.GetElementType(fieldInfo?.FieldType) ?? fieldInfo?.FieldType;
                refType = type?.BaseType?.GetGenericArguments()[0];
            }

            m_Field = new ReferenceField(string.Empty)
            {
                value = guid,
                objectType = refType,
                style = { flexShrink = new StyleFloat(1), flexGrow = new StyleFloat(1) },
            };

            var newBtn = CreateNewButton(fieldInfo);

            var expandBtn = CreateExpandButton();
            string strRegex = @"([0-9]+([A-Za-z]+[0-9]+)+)";
            Regex re = new Regex(strRegex);
            m_Field.RegisterValueChangedCallback(e =>
            {
                if (re.IsMatch(e.newValue) || string.IsNullOrEmpty(e.newValue))
                {
                    expandBtn.style.display = e.newValue == "" ? DisplayStyle.None : DisplayStyle.Flex;
                    newBtn.style.display = e.newValue != "" ? DisplayStyle.None : DisplayStyle.Flex;
                    var newGuid = e.newValue;
                    UnityEngine.Debug.Log(e.newValue);
                    m_GuidProp.Property.stringValue = newGuid;
                    m_GuidProp.Property.serializedObject.ApplyModifiedProperties();
                    OnValueChangedEvent.Invoke();
                }
            });

            expandBtn.style.display = asset == null ? DisplayStyle.None : DisplayStyle.Flex;
            newBtn.style.display = asset != null ? DisplayStyle.None : DisplayStyle.Flex;
            ContentContainer.Add(m_Field);
            ContentContainer.Add(newBtn);
            ContentContainer.Add(expandBtn);

            CreateContentManipulator();
            ContentContainer.style.flexGrow = 1;

            AddComponent(new CleanHandler(this));
        }

        private void CreateContentManipulator()
        {
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (m_Field.Blueprint != null)
                    evt.menu.AppendAction("Select on Project", x => SelectObject());

                if (m_Field.Blueprint != null)
                    evt.menu.AppendAction("Open on other Window", x => OpenOnOtherWindow());

                if (m_IsHaveNewButton)
                {
                    evt.menu.AppendAction("New", x =>
                    {
                        if (m_IsNicolayButton)
                        {
                            NicolayCreationLogic(m_DefaultCreatedName);
                        }
                        else
                        {
                            BaseCreationLogic(m_DefaultCreationPath, m_DefaultCreatedName);
                        }
                    });
                }
            }));
        }

        private VisualElement CreateExpandButton()
        {
            var btn = new Button() {text = string.Empty, style = {paddingLeft = 1, paddingRight = 1}};
            btn.clicked += OpenOnOtherWindow;

            var img = new Image() {image = UIElementsResources.NewWindowIcon, scaleMode = ScaleMode.ScaleToFit};
            btn.Add(img);

            return btn;
        }

        private VisualElement CreateNewButton(FieldInfo fieldInfo)
        {
            VisualElement element = default;
            CheckNewButton(fieldInfo);
            if (m_IsHaveNewButton)
            {
                var btn = new Button() {text = "new"};
                if (Property != null)
                {
                    m_DefaultCreatedName =
                        TextTemplates.ReplacePropertyNames(m_DefaultCreatedName ?? "", Property.serializedObject);
                    m_DefaultCreationPath =
                        TextTemplates.ReplacePropertyNames(m_DefaultCreationPath ?? "", Property.serializedObject);
                }

                if (m_IsNicolayButton)
                {
                    btn.clicked += () => NicolayCreationLogic(m_DefaultCreatedName);
                }
                else
                {
                    btn.clicked += () => BaseCreationLogic(m_DefaultCreationPath, m_DefaultCreatedName);
                }

                element = btn;
            }

            return element ?? new VisualElement();
        }

        private void CheckNewButton(FieldInfo fieldInfo)
        {
            m_DefaultCreationPath = default;
            m_DefaultCreatedName = default;

            var createPathAttribute =
                fieldInfo.GetCustomAttributes(typeof(CreatePathAttribute), true)
                    .FirstOrDefault() as CreatePathAttribute
                ?? fieldInfo.FieldType.GetCustomAttributes(typeof(CreatePathAttribute), true)
                    .FirstOrDefault() as CreatePathAttribute;

            m_DefaultCreationPath = m_DefaultCreationPath ?? createPathAttribute?.Path;

            var createNameAttribute =
                fieldInfo.GetCustomAttributes(typeof(CreateNameAttribute), true)
                    .FirstOrDefault() as CreateNameAttribute
                ?? fieldInfo.FieldType.GetCustomAttributes(typeof(CreateNameAttribute), true)
                    .FirstOrDefault() as CreateNameAttribute;

            m_DefaultCreatedName = m_DefaultCreatedName ?? createNameAttribute?.Name;
            m_IsNicolayButton = fieldInfo.FieldType.HasAttribute<ShowCreatorAttribute>() ||
                                fieldInfo.HasAttribute<ShowCreatorAttribute>();

            m_IsHaveNewButton = m_DefaultCreationPath != null || m_IsNicolayButton;
        }

        private void NicolayCreationLogic(string defaultCreatedName)
        {
            var creator = CreatorPicker.GetCreatorForType(m_Field.objectType);
            if (creator)
            {
                creator.SetRootObject(Property.serializedObject.targetObject);
                NewAssetWindow.ShowWindow(creator, defaultCreatedName, created =>
                {
                    var guid = (created as BlueprintScriptableObject).AssetGuid;
                    m_Field.value = guid;
                    m_GuidProp.Property.stringValue = guid;
                    Property.serializedObject.ApplyModifiedProperties();
                });
            }
        }

        private void BaseCreationLogic(string defaultCreationPath, string defaultCreatedName)
        {
            var created =
                BlueprintLinkDrawer.CreateAsset(m_Field.objectType, defaultCreationPath, defaultCreatedName);
            m_Field.value = created.AssetGuid;
            m_GuidProp.Property.stringValue = (created as SimpleBlueprint).AssetGuid;
            Property.serializedObject.ApplyModifiedProperties();
        }

        private void OpenOnOtherWindow()
        {
            if (m_Field.Blueprint != null)
            {
                BlueprintInspectorWindow.OpenFor(m_Field.Blueprint);
            }
        }

        private void SelectObject()
        {
            if (m_Field.value != null)
            {
                Selection.activeObject = BlueprintEditorWrapper.Wrap(m_Field.Blueprint);
            }
        }

        private class CleanHandler : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
        {
            private readonly BlueprintReferenceProperty m_Owner;

            int IOwlcatPropertyInputHandler.Order { get; } = 0;

            public CleanHandler(BlueprintReferenceProperty owner)
                => m_Owner = owner;

            void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
            {
                if (evt.keyCode == KeyCode.Delete && !string.IsNullOrEmpty(m_Owner.m_GuidProp.Property.stringValue))
                {
                    var oldGuid = m_Owner.m_GuidProp.Property.stringValue;
                    UndoManager.Instance.RegisterUndo($"Revert {m_Owner.m_GuidProp.Property.name} clear", () =>
                    {
                        m_Owner.m_GuidProp.Property.stringValue = oldGuid;
                        m_Owner.m_Field.value = oldGuid;
                        m_Owner.Property.serializedObject.ApplyModifiedProperties();
                    });

                    m_Owner.m_Field.value = default;
                    m_Owner.m_GuidProp.Property.stringValue = string.Empty;
                    m_Owner.Property.serializedObject.ApplyModifiedProperties();

                    evt.StopPropagation();
                }
            }
        }
    }
}