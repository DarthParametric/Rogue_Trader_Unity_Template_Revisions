using Kingmaker.Code.Editor.Utility;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using System.Reflection;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem.Interfaces;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;

namespace Kingmaker.Editor.UIElements.Custom
{
    public class ElementProperty : OwlcatProperty
    {
        #region Constructor

        public ElementProperty(SerializedProperty property, FieldInfo fieldInfo)
            : base(property, Layout.Vertical, true, false)
        {
            AddCreateButton();
            if (property.HasManagedReference())
            {
                CreateFillPart();
            }
            else
            {
                ContentContainer.style.display = DisplayStyle.None;
            }

            AddComponent(new CaptionTitleProvider());
            AddComponent(new OwlcatPropertyTooltip(GetTooltip));
            AddComponent(new CopyHandlerComponent());
            AddComponent(new PasteHandlerComponent(fieldInfo.FieldType, PastProcess));
        }

        protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
        {
            base.OnAttachToPanelInternal(evt);
            if (!HasComponent<ArrayElementComponent>())
            {
                CreateRemoveBtn();
                //Requested by game designers. Array elements can not be replaced by drag and drop.
                CreateDragAndDrop();
            }
            HeaderContainer.AddManipulator(
                new ContextualMenuManipulator(
                    @event => 
                        {
                            if (Application.isPlaying)
                            {
                                var type = SerializableTypesCollection.GetType(Property)??FieldFromProperty.GetActualValueType(Property);
                                if (type.IsSubclassOf(typeof(Condition)))
                                {
                                    @event.menu.AppendAction("Evaluate",
                                        _ => { PrototypedObjectEditorUtility.DebugCondition(FieldFromProperty.GetFieldValue(Property) as Condition); });
                                }
                                if (type.GetInterfaces().FirstOrDefault(v => v.IsGenericType && v.GetGenericTypeDefinition() == typeof(IEvaluator<>)) is {} iFace)
                                {
                                    @event.menu.AppendAction("Evaluate",
                                        _ => { PrototypedObjectEditorUtility.DebugEvaluator(iFace, FieldFromProperty.GetFieldValue(Property) as Element); });
                                }
                            }
                        }));

        }

        void PastProcess()
        {
            if (m_ContentRoot != null)
            {
                RemoveContent();
            }

            if (Property.HasManagedReference())
            {
                CreateFillPart();
            }
        }

        void CreateRemoveBtn()
        {
            m_RemoveBtn = new OwlcatSmallButton { text = "X" };
            m_RemoveBtn.clicked += RemoveContent;
            m_RemoveBtn.AddToClassList("red-button");
            //.style.backgroundColor = System.Drawing.Color.LightCoral.ToUnityColor();
            //m_RemoveBtn.style.color = System.Drawing.Color.Black.ToUnityColor();

            ControlsContainer.Add(m_RemoveBtn);
            m_RemoveBtn.style.display = !Property.hasChildren ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void CreateDragAndDrop()
        {
            var type = FieldFromProperty.GetDeclaredType(Property);
            m_Component = new FactoryDragAndDropComponent(type, CreateFillPart, RemoveContent);
            AddComponent(m_Component);
        }

        #endregion Constructor
        #region Fiedls

        DragAndDropComponent m_Component = default;

        VisualElement m_ContentRoot;

        VisualElement m_CreateBtn;

        VisualElement m_ControlsBtn;

        VisualElement m_Mark;

        OwlcatSmallButton m_RemoveBtn;

        #endregion Fields
        #region Methods

        void AddCreateButton()
        {
            var type = FieldFromProperty.GetDeclaredType(Property);
            m_CreateBtn = TypePicker.CreatePickerButton(
                "Create",
                () => TypeUtility.GetValidTypes(Property, type),
                sType =>
                {
                    TypeUtility.AddElementFromMenu(Property, sType);
                    CreateFillPart();
                });

            ControlsContainer.Add(m_CreateBtn);
        }

        void RemoveContent()
        {
            var element = GetElement();
            var serializedObject = Property.serializedObject;
            if (element != null)
            {
                element.Delete();
                serializedObject.Update();
            }

            Property.managedReferenceValue = null;
            serializedObject.ApplyModifiedProperties();

            if (m_ContentRoot != null)
            {
                ContentContainer.Remove(m_ContentRoot);
            }

            if (m_ControlsBtn != null)
            {
                ControlsContainer.Remove(m_ControlsBtn);
            }
            
            if (m_Mark != null)
            {
                HeaderContainer.Remove(m_Mark);
            }

            m_ControlsBtn = default;
            m_Mark = default;
            m_ContentRoot = default;
            m_CreateBtn.style.display = DisplayStyle.Flex;
            m_RemoveBtn.style.display = DisplayStyle.None;

            TitleLabel.text = Property.displayName;

            if (m_Component != null)
            {
                m_Component.IsEnable = true;
            }
        }

        private void RebuildParentArray()
        {
            void CheckThis(VisualElement e)
            {
                if(e is OwlcatArrayProperty a)
                {
                    a.RebuildContent();
                }
                else if(e.parent!=null)
                {
                    CheckThis(e.parent);
                }
            }

            CheckThis(parent);
        }
        
        private Element GetElement()
        {
            return FieldFromProperty.GetFieldValue(Property) as Element;
        }

        void CreateFillPart()
        {
            m_CreateBtn.style.display = DisplayStyle.None;
            if (m_RemoveBtn != null)
            {
                m_RemoveBtn.style.display = DisplayStyle.Flex;
            }

            m_ContentRoot = new VisualElement() { name = "ElementContent" };

            var so = Property.serializedObject;

            var title = Property.displayName;

            var element = GetElement();
            if (element is IHaveCaption caption)
            {
                try
                {
                    title = caption.Caption;
                }
                catch (System.Exception x)
                {
                    PFLog.Default.Exception(x);
                    title = x.Message;
                }
                /*if (element is IHaveDescription description)
     title = $"{title}: {description.Description}";*/
            }

            TitleLabel.text = title;
            m_ControlsBtn = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            CreateConditionalOperator(Property);
            CreateGuid(element);
            CreateContextMark(element); 
            CreateCodeButton(element);
            CreateEditor();

            ContentContainer.Add(m_ContentRoot);
            ControlsContainer.Insert(0, m_ControlsBtn);
            if (m_Component != null)
            {
                m_Component.IsEnable = false;
            }
        }

        void CreateGuid(Element element)
        {
            var guid = element.AssetGuidShort;
            if (!guid.IsNullOrEmpty())
            {
                var guidLabel = new Label(guid);
                guidLabel.style.width = 40;
                guidLabel.style.color = Color.black;
                guidLabel.style.marginTop = 1;
                guidLabel.style.marginBottom = 1;
                guidLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                guidLabel.style.backgroundColor = new StyleColor(new Color(150 / 255f, 150 / 255f, 150 / 255f));
                m_ControlsBtn.Add(guidLabel);
            }
        }

        void CreateConditionalOperator(SerializedProperty prop)
        {
            var propNot = prop.FindPropertyRelative("Not");
            if (propNot != null)
            {
                var elementNot = new Toggle() { text = "Not", value = propNot.boolValue, style = { flexShrink = 1, flexGrow = 0 } };
                elementNot.BindProperty(propNot);
                m_ControlsBtn.Add(elementNot);
            }
        }

        void CreateContextMark(Element element)
        {
            var mark = element is ContextAction || element is ContextCondition || element is Conditional ? "[C]" : null;
            if (mark != null)
            {
                m_Mark = new Label(mark);
                HeaderContainer.Add(m_Mark);
            }
        }

        void CreateCodeButton(Element element)
        {
            // todo: [bp] open code button (is it even needed?)
        }

        void CreateEditor()
        {
            var editor = new OwlcatInspectorRoot(Property);
            
            if (editor.childCount == 0)
            {
                editor.contentContainer.style.display = DisplayStyle.None;
                ContentContainer.style.display = DisplayStyle.None;
            }
            else
            {
                ContentContainer.style.display = DisplayStyle.Flex;
            }

            m_ContentRoot.Add(editor);
        }

        string GetTooltip() =>"";
//            => Property.objectReferenceValue != null && Property.objectReferenceValue is IHaveDescription descriptionHolder ?
//              descriptionHolder.Description : string.Empty;
    }

    #endregion Methods
}