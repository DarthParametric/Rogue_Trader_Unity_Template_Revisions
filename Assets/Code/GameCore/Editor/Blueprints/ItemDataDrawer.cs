using JetBrains.Annotations;
using Kingmaker.Blueprints.Loot;
using RectEx;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(TrashLootSettings.TypeChance.ItemData)), UsedImplicitly]
	public class ItemDataDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var labelAndValue = position.CutFromLeft(EditorGUIUtility.labelWidth);

			EditorGUI.LabelField(labelAndValue[0], label);

			var weightRect = labelAndValue[1].CutFromLeft(30);
			var typeAndItemQualityRect = weightRect[1].CutFromRight(EditorGUIUtility.labelWidth/2f);
			var weight = property.FindPropertyRelative("Weight");
			var type = property.FindPropertyRelative("Type");
			var itemQuality = property.FindPropertyRelative("ItemQuality");

			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			EditorGUI.PropertyField(weightRect[0], weight, GUIContent.none);
			EditorGUI.PropertyField(typeAndItemQualityRect[0], type, GUIContent.none);
			EditorGUI.PropertyField(typeAndItemQualityRect[1], itemQuality, GUIContent.none);

			EditorGUI.indentLevel = indent;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}