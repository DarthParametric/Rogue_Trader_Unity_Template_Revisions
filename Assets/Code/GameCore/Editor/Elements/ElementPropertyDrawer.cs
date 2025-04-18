#if UNITY_EDITOR
using System;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.ElementsSystem;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Elements
{
	[CustomPropertyDrawer(typeof(Element), true)]
	public class ElementPropertyDrawer : ElementsBaseDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new ElementProperty(property, fieldInfo);

		protected override void HandleOnGUI(Type elementType, Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.hasMultipleDifferentValues)
			{
				EditorGUILayout.LabelField(label, new GUIContent("- multiple -"));
				return;
			}

			DrawElement(elementType, property, null, 0, label);
		}

		protected override Type GetElementType(SerializedProperty property)
		{
			var type = fieldInfo.FieldType;
			if (type.IsArrayOrList() && !property.isArray)
			{
				type = type.GetElementType();
			}

			return type;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
	
	internal static class TypeHelper
	{
		public static bool IsArrayOrList(this Type listType)
		{
			if (listType.IsArray)
			{
				return true;
			}
			else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
			{
				return true;
			}
			return false;
		}
	}
}
#endif