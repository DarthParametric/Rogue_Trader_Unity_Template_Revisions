namespace Kingmaker.Editor.Utility
{
    public class LayoutInspector : UnityEditor.Editor
    {
        private static readonly string[] s_Excluded = { "m_Script" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, s_Excluded);
            serializedObject.ApplyModifiedProperties();	
        }
    }
}