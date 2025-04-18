using UnityEngine;
using UnityEditor;
namespace Kingmaker.Blueprints.Encyclopedia.Blocks
{
    [CustomEditor(typeof(BlueprintEncyclopediaBlock), true)]
    public class BlueprintEncyclopediaBlockInspector : UnityEditor.Editor
    {
        public virtual void OnEnable()
        {
                      
        }

        public override void OnInspectorGUI()
        {
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(GetHeight()));           
            OnDraw(rect);
        }

        public virtual void OnDraw(Rect rect, params string[] ignoreList)
        {
            serializedObject.Update();
            EditorGUI.DrawRect(new Rect( rect.x, rect.y+1, rect.width, 1),Color.gray);
            var allSerializedProperty = serializedObject.GetIterator();
            bool first = true;
            Rect eRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(eRect, target.name);
            eRect.y += eRect.height + 2;

            while (allSerializedProperty.NextVisible(first))
            {
                first = false;
                if (System.Array.Exists(ignoreList, x => x == allSerializedProperty.name || x == "m_Script")) break;
                eRect.height = EditorGUI.GetPropertyHeight(allSerializedProperty,true);               
                EditorGUI.PropertyField(eRect, allSerializedProperty, true);
                eRect.y += eRect.height + 2;
            }
            serializedObject.ApplyModifiedProperties();
        }
        public virtual float GetHeight(params string[] ignoreList)
        {
            bool first = true;
            float h = EditorGUIUtility.singleLineHeight + 4;
            var allSerializedProperty = serializedObject.GetIterator();
            while (allSerializedProperty.NextVisible(first))
            {
                first = false;
                if (System.Array.Exists(ignoreList, x => x == allSerializedProperty.name || x == "m_Script")) break;
                h += EditorGUI.GetPropertyHeight(allSerializedProperty, true) + 2;
            }
            return h;
        }
    }
}
