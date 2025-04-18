using System;
using System.Reflection;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Utility.Attributes;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor
{
	public static class ConditionalAttributeExtension
	{
		public static bool IsVisible(SerializedProperty property)
		{
			var field = FieldFromProperty.GetFieldInfo(property);
			var attr = field?.GetAttribute<ConditionalAttribute>();
			var visibilityCondition = attr?.CalculateCondition(property) ?? true;
			return
				attr == null ||
				visibilityCondition && (attr is ConditionalShowAttribute || attr is ShowIfAttribute) ||
				!visibilityCondition && (attr is ConditionalHideAttribute || attr is HideIfAttribute);
		}

		public static bool IsFieldVisible(this ConditionalAttribute attribute, SerializedProperty property)
		{
			var condition = attribute.CalculateCondition(property);
			return attribute.ValueForVisible ? condition : !condition;
		}

		public static bool CalculateCondition(this ConditionalAttribute attribute, SerializedProperty property)
        {
			bool result = false;
			try
            {
                var parent = property.GetParent();
                var v = parent == null
                    ? property.serializedObject.targetObject
                    : FieldFromProperty.GetFieldValue(parent);
                var field = GetField(v.GetType(), attribute.ConditionSource);
                result = (bool)GetValue(field, v);
            }
			catch (Exception e)
			{
				Debug.LogErrorFormat("ConditionalAttribute: can't extract value {1} from '{0}' (look at exception below)", property.propertyPath, attribute.ConditionSource);
				Debug.LogException(e);
			}
			return result;
		}

	    private static MemberInfo GetField(Type t, string name)
		{
			if (t == null || t == typeof(ScriptableObject) || t == typeof(MonoBehaviour))
			{
				return null;
			}

			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
										BindingFlags.Instance | BindingFlags.DeclaredOnly |
										BindingFlags.GetField | BindingFlags.GetProperty;

			return t.GetMember(name, flags).FirstOrDefault() ?? GetField(t.BaseType, name);
		}

		private static object GetValue(MemberInfo member, object fromObject)
		{
			return (member as PropertyInfo)?.GetValue(fromObject, null) ?? (member as FieldInfo)?.GetValue(fromObject);
		}
	}
}