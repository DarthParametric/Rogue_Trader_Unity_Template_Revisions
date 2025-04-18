using Kingmaker.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints
{
	[CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
	public class ScriptableObjectInspector : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			return UIElementsUtility.CreateInspector(serializedObject);
		}
	}
}