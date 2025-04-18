using System;
using Kingmaker.Utility.Attributes;
using RectEx;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
    [CustomPropertyDrawer(typeof(EnumFlagsAsButtonsAttribute))]
    public class EnumFlagsAsButtonsDrawer : PropertyDrawer
    {
        private const int Spacing = 3;

		private int GetRowsCount(SerializedProperty property)
		{
			var flagAttribute = (EnumFlagsAsButtonsAttribute)attribute;
			int valuesCount = Enum.GetNames(fieldInfo.FieldType).Length;
			return valuesCount / flagAttribute.ColumnCount + (valuesCount % flagAttribute.ColumnCount != 0 ? 1 : 0);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int rowsCount = GetRowsCount(property);
			return rowsCount * EditorGUIUtility.singleLineHeight + Math.Max(0, rowsCount - 1) * Spacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.hasMultipleDifferentValues)
            {
                var r = CutFromExtensions.SliceFromLeft(ref position, EditorGUIUtility.labelWidth);
                EditorGUI.LabelField(r, label);
                r = CutFromExtensions.SliceFromLeft(ref position, 70);
                GUI.Label(r, new GUIContent("- multiple -"));
                r = CutFromExtensions.SliceFromLeft(ref position, 50);
                r.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(r, "Reset"))
                {
                    property.intValue = property.intValue;
                }
                return;
            }

            var enumNames = Enum.GetNames(fieldInfo.FieldType);
            var enumValues = Enum.GetValues(fieldInfo.FieldType);

			var attr = (EnumFlagsAsButtonsAttribute)attribute;
			
            int enumLength = enumNames.Length;
			int rowsCount = GetRowsCount(property);
			bool[] buttonPressed = new bool[enumLength];

			var labelAndFieldPositions = position.CutFromLeft(EditorGUIUtility.labelWidth);
			EditorGUI.LabelField(labelAndFieldPositions[0], label);

			var cells = labelAndFieldPositions[1].Grid(rowsCount, attr.ColumnCount, Spacing);
			bool isNone = property.intValue == 0;
			for (int j = 0, index = 1; j < rowsCount; ++j)
			{
				for (int k = 0; k < attr.ColumnCount; ++k, ++index)
				{
					if (index >= enumValues.Length)
						index = 0;

					int enumValueInt = (int)enumValues.GetValue(index);

					// Check if the button is/was pressed
					buttonPressed[index] =
						isNone && enumValueInt == 0 ||
						!isNone && enumValueInt != 0 && (property.intValue & enumValueInt) == enumValueInt;
					
					EditorGUI.BeginChangeCheck();
					buttonPressed[index] = GUI.Toggle(cells[j, k], buttonPressed[index], enumNames[index], "Button");
					if (EditorGUI.EndChangeCheck())
					{
						if (buttonPressed[index])
						{
							property.intValue |= enumValueInt;
							isNone = enumValueInt == 0;
						}
						else
						{
							property.intValue &= ~enumValueInt;
						}
					}

					if (index == 0)
						break;
				}
			}

			if (isNone)
			{
				property.intValue = 0;
			}
		}
    }
}