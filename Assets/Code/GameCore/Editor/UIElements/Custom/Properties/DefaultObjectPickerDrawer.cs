using Kingmaker.Code.Editor.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
    [CustomPropertyDrawer(typeof(Sprite))]
    public class DefaultObjectPickerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new OwlcatObjectWithDefaultPickerField(property.displayName, property.GetFieldInfo());
            field.BindProperty(property); // should be OwlcatBind, but it does not work for object fields because magic
            return field;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.ObjectField(position, property, label);
        }
    }
}