using Kingmaker.Editor.Blueprints;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Kingmaker.Blueprints.Encyclopedia
{
    [BlueprintCustomEditor(typeof(BlueprintEncyclopediaNode), true)]
    public class BlueprintEncyclopediaNodeInspector : BlueprintInspectorCustomGUI
    {
        protected SerializedProperty m_Pages;
        protected ReorderableList m_PageList;
        protected SerializedObject SerializedObject;
        public override void OnEnable(BlueprintInspector ed)
        {
            if(ed==null)
                return;

            SerializedObject = ed.serializedObject;
            m_Pages = SerializedObject.FindProperty("Blueprint.ChildPages");
            m_PageList = new ReorderableList(SerializedObject, m_Pages,true,true,true, true);
            m_PageList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, m_Pages.displayName); };
            m_PageList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 1;
                rect.height -= 1;
                SerializedProperty element = m_Pages.GetArrayElementAtIndex(index);
                GUIContent content = new GUIContent((BlueprintReferenceBase.GetPropertyValue(element) as BlueprintEncyclopediaNode)?.Title.ToString() ?? element.displayName);
                EditorGUI.PropertyField(rect, element, content, true);
            };
            m_PageList.elementHeightCallback = (int index) =>
            {
                SerializedProperty element = m_Pages.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(element, true) + 2;
            };
        }

        public override void OnBeforeComponents(SimpleBlueprint bp)
        {
            SerializedObject.Update();
            m_PageList.DoLayoutList();
            SerializedObject.ApplyModifiedProperties();
        }
    }
}
