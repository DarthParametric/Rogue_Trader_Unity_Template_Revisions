using Kingmaker.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints
{
	[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
	public class ScriptableObjectInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			PrototypedObjectEditorUtility.DisplayProperties(serializedObject);
		}
		
		public override VisualElement CreateInspectorGUI()
		{
			return UIElementsUtility.CreateInspector(serializedObject);
		}
	}
}