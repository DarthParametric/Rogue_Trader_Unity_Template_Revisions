using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.Attributes;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
	public class ConditionalVisibilityElement : OwlcatVisualElement, IBindable
	{
		public IBinding binding { get; set; }

		public string bindingPath { get; set; }

		public ConditionalVisibilityElement(OwlcatProperty propertyElement, ConditionalAttribute conditionalVisibility)
		{
			binding = new ConditionalVisibilityBinding(propertyElement, conditionalVisibility);
			Add(propertyElement);
		}
	}

	public class ConditionalVisibilityBinding : IBinding
	{
		private readonly OwlcatProperty m_Property;

		private readonly ConditionalAttribute m_VisibilityAttribute;

		private bool m_Visible;
		
		public ConditionalVisibilityBinding(OwlcatProperty property, ConditionalAttribute conditionalVisibility)
		{
			m_Property = property;
			m_VisibilityAttribute = conditionalVisibility;
			m_Visible = conditionalVisibility.IsFieldVisible(m_Property.Property);
			m_Property.style.display = m_Visible ? DisplayStyle.Flex : DisplayStyle.None;
		}

		void IBinding.PreUpdate() { }

		void IBinding.Release() { }

		void IBinding.Update()
		{
			bool value = m_VisibilityAttribute.IsFieldVisible(m_Property.Property);
			if (value != m_Visible)
			{
				m_Visible = value;
				m_Property.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
	}
}