using System;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Designers.EventConditionActionSystem.Actions
{
	[CustomPropertyDrawer(typeof(ActionAndWeight))]
	public class ActionAndWeightDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var w = property.FindPropertyRelative("Weight");
			var c = property.FindPropertyRelative("Conditions");
			var a = property.FindPropertyRelative("Action");

			using (var scope = new EditorGUILayout.VerticalScope())
			{
				var p = scope.rect;
				p.x += 25;
				GUI.Box(p, GUIContent.none);

				EditorGUILayout.PropertyField(w);
				EditorGUILayout.PropertyField(c);
				EditorGUILayout.PropertyField(a);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			//m_SavedProperty = property;

			var prop = OwlcatProperty.CreateGeneric(property);
			
			var result = prop.WrapToOwlcatProperty(property);
			return prop;
			
			//propertyField.style.flexGrow = 1;
			
			//prop.HeaderContainer.style.display = DisplayStyle.None;
			
			
			/*var w = property.FindPropertyRelative("Weight");
			var c = property.FindPropertyRelative("Conditions");
			var a = property.FindPropertyRelative("Action");
			var weightProperty = new OwlcatProperty(w);
			weightProperty.style.display = DisplayStyle.Flex;
			prop.TitleLabel.text = "Weight";
			prop.ContentContainer.Add(weightProperty);
			prop.ContentContainer.Add(new OwlcatProperty(c));
			prop.ContentContainer.Add(new OwlcatProperty(a));*/
			//return prop;
			/*var newVE = new OwlcatProperty(property);

			var w = property.FindPropertyRelative("Weight");
			var c = property.FindPropertyRelative("Conditions");
			var a = property.FindPropertyRelative("Action");
			newVE.Add(new OwlcatProperty(w));

			return newVE;*/
		}
	}
}