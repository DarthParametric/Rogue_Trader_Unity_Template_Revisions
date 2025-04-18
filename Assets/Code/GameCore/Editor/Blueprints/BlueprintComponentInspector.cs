#if UNITY_EDITOR
using Kingmaker.Blueprints;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
	[CustomEditor(typeof(BlueprintComponent), true)]
	public class BlueprintComponentInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var sp = serializedObject.FindProperty("m_Script");
			if ((sp != null) && (sp.objectReferenceValue == null))
				EditorGUILayout.HelpBox(
					"This component script could not be loaded. Don't use it, Unity is unable to save such components.\n\nPut all component classes into individual files with file name equal to class name.",
					MessageType.Error);

			// show errors
			//var thisComponent = (BlueprintComponent)serializedObject.targetObject;
			//if (thisComponent.ValidationStatus.Errors.Any())
			//{
			//	var errors = thisComponent.ValidationStatus.Errors
			//		.Aggregate("", (r, e) => string.Concat(r, (r.Empty() ? "- " : "\n- "), e));
			//	EditorGUILayout.HelpBox(errors, MessageType.Error);
			//}
			//else
			//{
			//	// dummy label to prevent focus from jumping when validation changes while editing
			//	EditorGUILayout.LabelField("", "", GUILayout.Height(0));
			//}

			PrototypedObjectEditorUtility.DisplayProperties(serializedObject);
		}
	}
}
#endif