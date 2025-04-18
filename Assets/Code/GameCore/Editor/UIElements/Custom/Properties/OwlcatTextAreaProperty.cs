using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class OwlcatTextAreaProperty : OwlcatProperty
	{
		public OwlcatTextAreaProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
			if (property.propertyType != SerializedPropertyType.String)
				throw new System.Exception("No valid value for TextAreaProperty");

			var textField = new OwlcatTextField
			{
				multiline = true,
				value = property.stringValue
			};
			textField.BindProperty(property);
			ContentContainer.Add(textField);
		}
	}
}