using Code.GameCore.ElementsSystem;
using Kingmaker.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using System;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.EntitySystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility.EditorAttributes;
using Owlcat.Runtime.Core.Utility;
using UnityEngine.PlayerLoop;
using HelpBox = Kingmaker.Assets.Editor.UIElements.Custom.Elements.HelpBox;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class ComponentElement : OwlcatPropertyLayout
    {
        public BlueprintComponent Component
        {
            get
            {
                var comp = ((BlueprintComponentEditorWrapper)SerializedObject.targetObject).GetCanonicalInstance(); // we want the actual component instance on the blueprint

                // todo: compare index too? Do we need to?
				return comp?.name == m_ComponentName ? comp : null; // the editor only works when it's the same component at the same property index
            }
        }

        public readonly SerializedObject SerializedObject;
        public readonly int ComponentIndex;
		
		private VisualElement m_Content;

		private Label m_Comment;
        private string m_ComponentName;
        public event Action<ComponentElement> OnMoveUpEvent = delegate { };
		public event Action<ComponentElement> OnMoveDownEvent = delegate { };
		public event Action<ComponentElement> OnRemoveEvent = delegate { };
		public event Action<ComponentElement> OnCopyEvent = delegate { };

		public ComponentElement(BlueprintComponent component, SerializedObject serializedObject, int index) : base(Layout.Vertical, true)
		{
            m_ComponentName = component.name;
            ComponentIndex = index;
            var wrapper = BlueprintComponentEditorWrapper.Wrap(component);
            SerializedObject = new SerializedObject(wrapper);
            
            name = Component.name;

			AddToClassList("owlcat-component");
			AddToClassList("owlcat-box");

			HeaderContainer.AddToClassList("owlcat-component");
			ContentContainer.AddToClassList("owlcat-component");
			ControlsContainer.AddToClassList("owlcat-component");
			
			TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			
			UpdateTitle();
			SetupHeader();
			SetupControls();

            var type = new IMGUIContainer(() => BlueprintEditorUtility.ShowType("Script", Component.GetType()));
            ContentContainer.Add(type);
            
            var infoAttributes = Component.GetType().GetAttributes<ClassInfoBox>();
            foreach (var info in infoAttributes)
            {
                ContentContainer.Add(new HelpBox(info.Text));
            }

			var componentPath = "Component";
            var rootProperty = SerializedObject.FindProperty(componentPath);
            rootProperty.Next(true);
            OwlcatInspectorRoot.SetupContent(ContentContainer, rootProperty, true);
            IsExpanded = GetSavedExpandedState();
            UpdateExpanded();
		}

		protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
		{
			base.OnAttachToPanelInternal(evt);
			this.OwlcatBind(SerializedObject);
		}

		private void SetupHeader()
		{
			m_Comment = new Label {name = "Comment"};
			HeaderContainer.Add(m_Comment);
		}

		private void SetupControls()
		{
			var enabledToggle = new Toggle();
			enabledToggle.SetValueWithoutNotify(!Component.Disabled);
			
			var moveUp = new OwlcatSmallButton(() => OnMoveUpEvent(this)) { text = "↑" };
			var moveDown = new OwlcatSmallButton(() => OnMoveDownEvent(this)) { text = "↓" };
			var remove = new OwlcatSmallButton(() => OnRemoveEvent(this)) { text = "X" };
			remove.AddToClassList("red-button");
			
			var copy = new OwlcatSmallButton(() => OnCopyEvent(this)) { text = "Сopy" };
			copy.style.width = 50;
			
			ControlsContainer.Add(enabledToggle);
			ControlsContainer.Add(copy);
			ControlsContainer.Add(moveUp);
			ControlsContainer.Add(moveDown);
			ControlsContainer.Add(remove);
			
			enabledToggle.RegisterValueChangedCallback(
				evt => 
				{
					if (Component.Disabled != !evt.newValue)
					{
						Component.Disabled = !evt.newValue;
						BlueprintsDatabase.SetDirty(Component.OwnerBlueprint.AssetGuid);
						
						UpdateTitle();
						UpdateExpanded();
					}
				});
		}

		private void UpdateTitle()
		{
			TitleLabel.text = Component.GetType().Name;
			
			bool disabled = Component.Disabled;
			if (disabled)
			{
				TitleLabel.text = $"DISABLED {TitleLabel.text}";
			}
			
			bool safeForDelete = (Component as IOverrideOnActivateMethod)?.IsOverrideOnActivateMethod ?? false;
			if (!safeForDelete)
			{
				TitleLabel.text = $"[!] {TitleLabel.text}";
			}
		}

		private void UpdateExpanded()
		{
			IsExpanded &= !Component.Disabled;
		}

		protected override void OnIsExpandedChanged()
		{
			if (IsExpanded && Component.Disabled)
			{
				IsExpanded = false;
				return;
			}

			base.OnIsExpandedChanged();
			if (m_Comment != null)
			{
				var prop = SerializedObject.FindProperty("Component.Comment"); // todo: look in THIS COMPONENT instead
				m_Comment.text = prop?.stringValue ?? string.Empty;
				m_Comment.style.display = IsExpanded ? DisplayStyle.None : DisplayStyle.Flex;
			}
		}
		
		protected override void SwitchExpanded(MouseDownEvent evt)
		{
			base.SwitchExpanded(evt);
			UIElementsUtility.SetExpandedState(GetExpandedPath(), IsExpanded);
		}

		protected override string GetExpandedPath()
		{
			return "ComponentsContainer/" + name.Split("$")[1] + $"[{ComponentIndex}]";
		}
    }
}