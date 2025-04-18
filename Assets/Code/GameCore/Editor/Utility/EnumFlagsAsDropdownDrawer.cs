using Kingmaker.Utility.Attributes;
using UnityEngine;
using UnityEditor;
using System;
namespace Kingmaker.Utility
{
    [CustomPropertyDrawer(typeof(EnumFlagsAsDropdownAttribute))]
    public class EnumFlagsAsDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height), label);

            string text = property.hasMultipleDifferentValues?"-multiple-  ":property.intValue == 0 ? "None  " : "";

            foreach (string name in property.enumNames)
            {
                int value = (int)System.Enum.Parse(fieldInfo.FieldType, name);
                if ((property.intValue & value) == value) text += string.Format("{0}, ", name);
            }
            text = text.Remove(text.Length - 2, 2);
            Rect popupRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
            if (GUI.Button(popupRect, new GUIContent(text), (GUIStyle)"miniPopup"))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var name in property.enumNames)
                {
                    int value = (int)System.Enum.Parse(fieldInfo.FieldType, name);
                    bool has = (property.intValue & value) == value;
                    menu.AddItem(new GUIContent("None"), property.intValue == 0, () =>
                    {
                        property.intValue = 0;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                    menu.AddItem(new GUIContent(name), has, () =>
                    {
                        if (has) property.intValue ^= value;
                        else property.intValue |= value;
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.DropDown(popupRect);
            }
        }
    }
}
