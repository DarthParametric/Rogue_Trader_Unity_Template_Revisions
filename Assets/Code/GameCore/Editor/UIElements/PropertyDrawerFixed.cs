using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
    /// <summary>
    /// Inherit from this to create a custom drawer that uses UIElements.
    /// Using PropertyField from inside this drawer will not cause stack overflow, as
    /// opposed to normal PropertyDrawer.
    /// </summary>
    public class PropertyDrawerFixed: GUIDrawer
    {
        public FieldInfo Field { get; set; }
        public virtual VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            throw new NotImplementedException(nameof(CreatePropertyGUI));
        }
    }
}