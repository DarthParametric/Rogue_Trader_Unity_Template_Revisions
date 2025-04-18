using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.EditorPreferences;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(bool))]
	public class BoolDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			var result = new Toggle();
			result.BindProperty(property);
			return result.WrapToOwlcatProperty(property);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorPreferences.Instance.BigCheckbox ? OwlcatEditorStyles.Instance.BigCheckboxHeight : EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (EditorPreferences.Instance.BigCheckbox)
			{
				var b = EditorGUI.Toggle(position, label, property.boolValue, OwlcatEditorStyles.Instance.BigCheckbox);
				// ReSharper disable once RedundantCheckBeforeAssignment
				if (b != property.boolValue) // this is in case property has multiple values: we do not want to accidentally reset those
				{
					property.boolValue = b;
				}
			}
			else
			{
				EditorGUI.PropertyField(position, property, label);
			}
		}
	}
}