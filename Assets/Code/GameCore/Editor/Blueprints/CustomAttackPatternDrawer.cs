using Kingmaker.Blueprints;
using Kingmaker.Editor.GridPatterns;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    [CustomPropertyDrawer(typeof(CustomAttackPattern))]
    public class CustomAttackPatternDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var prop = property.FindPropertyRelative("cells");
            if (GUILayout.Button("Edit"))
            {
                GridPatternsEditor.ShowWindow(prop);
            }
        }
    }
}