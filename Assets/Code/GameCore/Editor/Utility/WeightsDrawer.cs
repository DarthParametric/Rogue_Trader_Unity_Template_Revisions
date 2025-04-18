using Kingmaker.Code.Editor.Utility;
using Kingmaker.Utility.Attributes;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(WeightsAttribute))]
	public class WeightsDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			var propertyArray = property.GetParent().GetParent();
			int arraySizeProp = propertyArray.FindPropertyRelative("size").intValue;
			float weightSum = 0;
			for (int i = 0; i < arraySizeProp; i++)
			{
				weightSum += propertyArray.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").intValue;
			}
			float percent = (weightSum > 0) ? property.intValue / weightSum * 100 : 0.0f;
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(property);
			EditorGUILayout.LabelField(percent.ToString() + "%");
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndProperty();
		}
	}
}