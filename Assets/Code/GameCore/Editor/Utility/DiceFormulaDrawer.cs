using System.Collections.Generic;
using Kingmaker.RuleSystem;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(DiceFormula))]
	public class DiceFormulaDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
		    if (property.hasMultipleDifferentValues)
		    {
		        EditorGUI.LabelField(position, label, new GUIContent("- multiple -"));
		        return;
		    }

            var rollsProperty = property.FindPropertyRelative("m_Rolls");
			var diceProperty = property.FindPropertyRelative("m_Dice");
			Dictionary<SerializedProperty, string> properties = new Dictionary<SerializedProperty, string>
			{
				{rollsProperty, "d"},
				{diceProperty, ""},
			};

			float x = position.x;

			{
				var r = position;
				r.width = EditorGUIUtility.labelWidth;
				EditorGUI.LabelField(r, label);
				x += r.width;
			}

			bool isZero = diceProperty.intValue == 0;
			bool isOne = diceProperty.intValue == 1;

			const float fieldWidth = 40;
			const float postfixWidth = 20;
			foreach (var p in properties)
			{
				var r = position;

				r.x = x;
				r.width = fieldWidth;

				using (new EditorGUI.DisabledScope((isZero || isOne) && p.Key == rollsProperty))
				{
					using (GuiScopes.FixedWidth(0.1f, fieldWidth))
					{
						EditorGUI.PropertyField(r, p.Key, GUIContent.none);
					}
				}

				if (p.Key == rollsProperty)
				{
					if (isZero)
					{
						rollsProperty.intValue = 0;
					}
					else if (isOne || p.Key.intValue < 1)
					{
						rollsProperty.intValue = 1;
					}
				}

				r.x += r.width + postfixWidth * 0.4f;
				r.width = postfixWidth * 0.6f;
				GUI.Label(r, p.Value);

				x += fieldWidth + postfixWidth;
			}
		}
	}
}