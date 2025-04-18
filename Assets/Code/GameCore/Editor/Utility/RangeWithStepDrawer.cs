using Kingmaker.Utility.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
    [CustomPropertyDrawer(typeof(RangeWithStepAttribute))]
    public class RangeWithStepDrawer : PropertyDrawer
    {
        private int value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rangeAttribute = (RangeWithStepAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                value = EditorGUI.IntSlider(position, label, property.intValue, rangeAttribute.min, rangeAttribute.max);

                value = (value / rangeAttribute.step) * rangeAttribute.step;
                property.intValue = value;
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use with int.");
            }
        }
    }
}