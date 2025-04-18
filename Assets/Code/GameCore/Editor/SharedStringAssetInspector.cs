using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Localization;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor
{
#if UNITY_EDITOR && EDITOR_FIELDS
	[CustomEditor(typeof(SharedStringAsset))]
	public class SharedStringAssetInspector : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			if (!EditorPreferences.Instance.UseNewEditor)
				return base.CreateInspectorGUI();

			var lp = serializedObject.FindProperty("String");

			var owlcatInspectorRoot = UIElementsUtility.CreateInspector(serializedObject, isHideScriptField: true);
			//owlcatInspectorRoot.Insert(0, new LocalizedStringCommentProperty(lp));
			return owlcatInspectorRoot;
		}

		public override void OnInspectorGUI()
		{
			var sharedString = serializedObject.targetObject as SharedStringAsset;
			if (sharedString == null)
			{
				return;
			}

			var lp = serializedObject.FindProperty("String");
			var ls = sharedString.String;

			bool needsFixUp = !LocalizedStringManipulation.Check(ls, lp);
			if(needsFixUp)
				EditorGUI.BeginDisabledGroup(true);

			string oldComment = ls.GetCommentOnCurrentLocale();
			GUILayout.Label("Comment");

			string updatedText = EditorGUILayout.TextArea(oldComment, GUILayout.MinHeight(48));
			if (!needsFixUp && ls.UpdateComment(lp, updatedText))
			{
				UndoManager.Instance.RegisterUndo(
					serializedObject.targetObject.name + " edit",
					() => ls.UpdateComment(lp, oldComment)
				);
			}
			if(needsFixUp)
				EditorGUI.EndDisabledGroup();

			DrawPropertiesExcluding(serializedObject, "m_Script");
		}
	}
#endif
}