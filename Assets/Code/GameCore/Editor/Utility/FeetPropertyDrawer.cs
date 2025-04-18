using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(Feet))]
	[PropertyLabelSuffix(" (feet)")]
	public class FeetPropertyDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var element = new IntegerField();
			var valueProperty = property.FindPropertyRelative("m_Value");
			element.BindProperty(valueProperty);
			
			return element.WrapToOwlcatProperty(property);
		}
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var feetValueProperty = property.FindPropertyRelative("m_Value");
			label.text += " (feet)";
			EditorGUI.PropertyField(position, feetValueProperty, label);
		}
	}
}