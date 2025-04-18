using Kingmaker.ResourceLinks;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    [CustomPropertyDrawer(typeof(UIViewLink<,>), true)]
    public class UIViewLinkDrawer : WeakLinkDrawer<GameObject>
    {
        GUIContent targetLabel = new GUIContent(" ");
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
                
            var targetProperty = property.FindPropertyRelative("Target");
            
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.objectField);
            EditorGUI.PropertyField(rect, targetProperty, targetLabel);
        }
    }
}