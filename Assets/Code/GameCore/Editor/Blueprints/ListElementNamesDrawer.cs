using Kingmaker.Code.Editor.Utility;
using Kingmaker.Utility.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(ArrayElementNamePrefixAttribute), true)]
	public class ListElementNamesDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ArrayElementNamePrefixAttribute prefixAttribute = attribute as ArrayElementNamePrefixAttribute;
            label = new GUIContent(property.IsArrayElement()
                ? prefixAttribute.GetName(property.GetIndexInParentArray())
                : property.displayName, label.tooltip);
            EditorGUI.PropertyField(position, property, label);
        }
	}
}