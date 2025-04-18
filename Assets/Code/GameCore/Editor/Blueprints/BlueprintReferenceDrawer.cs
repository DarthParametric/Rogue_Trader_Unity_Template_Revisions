#if UNITY_EDITOR
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.Utility;
using Kingmaker.Utility.CodeTimer;
using Owlcat.Editor.Utility;
using RectEx;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(BlueprintReferenceBase), true)]
	public class BlueprintReferenceDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new BlueprintReferenceProperty(property, fieldInfo);

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var type = BlueprintLinkDrawer.GetElementType(fieldInfo?.FieldType) ?? fieldInfo?.FieldType;

			var referencedType = type?.BaseType?.GetGenericArguments()[0];

			if (referencedType == null)
			{
				EditorGUI.LabelField(position, property.displayName, "[Cannot determine reference type]");
				return;
			}
			
			GUILayout.BeginHorizontal();

			var guidProp = new RobustSerializedProperty(property.FindPropertyRelative("guid"));
			var g = guidProp.Property.stringValue;
            var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(g);
            Rect[] chunkPositions = position.Row(new[] { 5f, 0.1f });
            
            using (ProfileScope.New("ShowObjectField"))
            {
	            BlueprintPicker.ShowObjectField(chunkPositions[0], bp, bp2 =>
	            {
		            guidProp.Property.stringValue = bp2?.AssetGuid ?? "";
		            guidProp.Property.serializedObject.ApplyModifiedProperties();
	            }, label, referencedType);
            }
            
            if(guidProp.Property.boxedValue.ToString() != "" 
               && GUI.Button(chunkPositions[1], "", OwlcatEditorStyles.Instance.OpenButton))
            {
	            BlueprintInspectorWindow.OpenFor(bp);
            }
            GUILayout.EndHorizontal();
		}
    }
}
#endif