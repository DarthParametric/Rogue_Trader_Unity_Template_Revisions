using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(Rounds))]
	[PropertyLabelSuffix(" (rounds)")]
	public class RoundsPropertyDrawer : PropertyDrawer
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
			var valueProperty = property.FindPropertyRelative("m_Value");
			label.text += " (rounds)";
			EditorGUI.PropertyField(position, valueProperty, label);
		}
	}
}