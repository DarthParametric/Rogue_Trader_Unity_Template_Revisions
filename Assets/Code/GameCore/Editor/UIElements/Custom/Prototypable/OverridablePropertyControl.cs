using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.PubSubSystem.Core.Interfaces;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Prototypable
{
	public class OverridablePropertyControl : OwlcatVisualElement, IOverrideProperty
	{
		public readonly OwlcatProperty PropertyElement;

		private readonly Button m_OverrideButton;
		private readonly Button m_RevertButton;

		public SerializedProperty Property
			=> PropertyElement.Property;

		public IHavePrototype TargetObject
            => UnwrapPrototypeable(Property.serializedObject.targetObject);

        IHavePrototype UnwrapPrototypeable(Object o)
        {
            switch (o)
            {
                case BlueprintEditorWrapper bew:
                    return bew.Blueprint as IHavePrototype;
                case BlueprintComponentEditorWrapper bcew:
                    return bcew.Component;
                default:
                    return null;
            }
        }

        public bool IsOverridden
			=> IsOverridable && TargetObject.IsOverridden(Property);

        private bool IsOverridable
            => TargetObject?.PrototypeLink != null && TargetObject.IsOverridable(Property);

		public OverridablePropertyControl(OwlcatProperty property)
		{
			PropertyElement = property;

			m_OverrideButton = new OwlcatSmallButton(SwitchOverridenState) { text = string.Empty };
			m_OverrideButton.Add(new Image() { image = UIElementsResources.OverrideIcon, scaleMode = ScaleMode.ScaleToFit });

			m_RevertButton = new OwlcatSmallButton(SwitchOverridenState) { text = string.Empty };
			m_RevertButton.Add(new Image() { image = UIElementsResources.RevertOverrideIcon, scaleMode = ScaleMode.ScaleToFit });

			Add(m_OverrideButton);
			Add(m_RevertButton);

			UpdateView();

			property.AddManipulator(new ContextualMenuManipulator(evt =>
			{
				if (TargetObject?.PrototypeLink!=null)
				{
					if (!IsOverridden)
					{
						evt.menu.AppendAction("Override", x => SwitchOverridenState());
					}
					else
					{
						evt.menu.AppendAction("Revert", x => SwitchOverridenState());
					}
				}
			}));


			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			EventBus.Subscribe(this);
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			EventBus.Unsubscribe(this);
		}

		void IOverrideProperty.OnOverrideStateChanged()
		{
			UpdateView();
		}

		private void SwitchOverridenState()
		{
			SetOverridden(!IsOverridden);
			UpdateView();
		}

		public void UpdateView()
		{
			bool overriden = IsOverridden;
			m_RevertButton.style.display = overriden ? DisplayStyle.Flex : DisplayStyle.None;
			m_OverrideButton.style.display = overriden ? DisplayStyle.None : DisplayStyle.Flex;

			style.display = IsOverridable ? DisplayStyle.Flex : DisplayStyle.None;
			if (IsOverridable)
			{
				PropertyElement.ContentContainer.SetEnabled(overriden);
				PropertyElement.ControlsContainer.SetEnabled(overriden);
				if (overriden)
				{
					PropertyElement.HeaderContainer.RemoveFromClassList("unity-disabled");
				}
				else
				{
					PropertyElement.HeaderContainer.AddToClassList("unity-disabled");
				}
			}
			else
			{
				PropertyElement.ContentContainer.SetEnabled(true);
				PropertyElement.ControlsContainer.SetEnabled(true);
				PropertyElement.HeaderContainer.RemoveFromClassList("unity-disabled");
			}
		}

		private void SetOverridden(bool overridden)
		{
			for (int ii = 0; ii < Property.serializedObject.targetObjects.Length; ii++)
			{
				var blueprint = UnwrapPrototypeable(Property.serializedObject.targetObjects[ii]);
				if (blueprint.IsOverridden(Property) == overridden)
				{
					continue;
				}

				blueprint.SetOverridden(Property, overridden);
				if (!overridden)
				{
                    // TODO
                    //((BlueprintEditorWrapper)Property.serializedObject.targetObjects[ii]).SyncPropertiesWithProto();
                    //var b = blueprint as BlueprintScriptableObject;
                    //if (b != null)
                    //{
                    //    b.Validate();
                    //} 
				}
			}
            Property.serializedObject.Update();
		}
	}
		
	public interface IOverrideProperty : ISubscriber
	{
		void OnOverrideStateChanged();
	}
}