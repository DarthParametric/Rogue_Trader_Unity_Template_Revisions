using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints.Base;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Elements;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.EditorPreferences;
using Kingmaker.Utility.FlagCountable;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility;

namespace Kingmaker.Editor.UIElements
{
	public static class UIElementsUtility
	{
		public class InitializationProcessFlag : IDisposable
		{
			public static CountableFlag Flag { get; } = new CountableFlag();

			public static InitializationProcessFlag Require()
			{
				Flag.Retain();
				return new InitializationProcessFlag();
			}

			void IDisposable.Dispose()
			{
				Flag.Release();
			}
		}

		public static readonly VisualElement UseDefaultBehavior = new VisualElement {name = "use-default-behavior"};
		private static HashSet<string> m_ExpandedStateSet = new();
		
		public static OwlcatInspectorRoot CreateInspector(
			SerializedObject serializedObject,
			[CanBeNull] Action<OwlcatInspectorRoot> initializer = null,
			bool isHideScriptField = false, bool force = false)
		{
			if (!EditorPreferences.Instance.UseNewEditor && !force)
			{
				return null;
			}

			using (InitializationProcessFlag.Require())
			{
				OwlcatInspectorRoot inspector;
				if (serializedObject.targetObject is BlueprintEditorWrapper)
				{
					inspector = new BlueprintInspectorRoot(serializedObject);
				}
				else
				{
					inspector = new OwlcatInspectorRoot(serializedObject, isHideScriptField);
				}

				initializer?.Invoke(inspector);

				return inspector;
			}
		}

		public static OwlcatProperty CreatePropertyElement(SerializedProperty property, bool isArrayElement)
		{
			Profiler.BeginSample($"Create UIElement");
			var info = property.GetFieldInfo();
			if (info != null &&
			    info.FieldType.IsEnum &&
			    property.enumValueIndex == -1)
			{
				FixEnum(info, property);
			}

			var visualElement = TryCreateCustomVisualElement(property, info);
			if (visualElement == null || visualElement == UseDefaultBehavior)
			{
				if (property.isArray && property.propertyType != SerializedPropertyType.String)
				{
					var elementType = PropertyToFieldMatcher.GetMatcher(property.serializedObject.targetObject)
						.GetMatchingField(property).GetElementType();
					
					if (property.serializedObject.targetObject is not ScriptableWrapperBase ||
					    property.propertyType == SerializedPropertyType.ManagedReference)
					{
						var types = TypeUtility.GetValidTypes(property, elementType).ToArray();
						visualElement = new OwlcatArrayProperty(property, types);
					}
					else
					{
						visualElement = new OwlcatArrayProperty(property);
					}
				}
				else if ((property.propertyType == SerializedPropertyType.Generic && property.hasChildren ||
				          property.propertyType == SerializedPropertyType.ManagedReference))
				{
					visualElement = OwlcatProperty.CreateGeneric(property);
				}
				else if (property.propertyType == SerializedPropertyType.ObjectReference && info != null)
				{
					visualElement = new OwlcatObjectProperty(property, info);
				}
				else
				{
					visualElement = CreateDefaultField(property);
					if (property.propertyPath == "m_Script" && property.serializedObject.targetObject != null)
					{
						visualElement.SetEnabled(false);
					}
				}
			}

			Profiler.EndSample();

			var result = visualElement.WrapToOwlcatProperty(property);
			if (info != null && !isArrayElement)
			{
				result.Attributes = info.GetCustomAttributes().ToArray();
			}

			return result;
		}

		private static VisualElement CreateDefaultField(SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Enum:
					var info = property.GetFieldInfo();
					if (info?.FieldType.GetAttribute<FlagsAttribute>() != null)
					{
						var mixedValue = Enum.ToObject(info?.FieldType, property.enumValueFlag) as Enum;
						var flagsField = new EnumFlagsField(property.displayName, mixedValue) {bindingPath = property.propertyPath};
						flagsField.BindProperty(property);
						return flagsField;
					}
					else
					{
						return OwlcatProperty.CreateDefault(property);
					}

				default:
					return OwlcatProperty.CreateDefault(property);
			}
		}

		private static VisualElement TryCreateCustomVisualElement(SerializedProperty property, FieldInfo fieldInfo)
		{
			if (fieldInfo == null)
			{
				return null;
			}

			var attributes = fieldInfo.GetCustomAttributes();
			foreach (var att in attributes)
			{
				switch (att)
				{
					case TextAreaAttribute _:
					case MultilineAttribute _:
						return new OwlcatTextAreaProperty(property);
				}
			}

			// check for custom drawer attribute
			var attrs = fieldInfo.GetCustomAttributes<PropertyAttribute>();
			foreach (var attr in attrs)
			{
				if (attr is ITitleAttribute)
					continue;

				var ve = UICustomDrawerUtility.TryGetCustomVisualElement(attr.GetType(), attr, fieldInfo, property);
				if (ve != null)
					return ve;
			}

			// check for custom drawer on type
			var type = fieldInfo.FieldType;
			if (!property.isArray)
			{
				var fieldType = fieldInfo.FieldType;
				if (fieldType.IsArray)
				{
					type = fieldType.GetElementType();
				}
				else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
				{
					type = fieldType.GetGenericArguments()[0];
				}
				else
				{
					type = fieldInfo.FieldType;
				}
			}

			return UICustomDrawerUtility.TryGetCustomVisualElement(type, null, fieldInfo, property);
		}

		private static void FixEnum(FieldInfo info, SerializedProperty prop)
		{
			if (info.FieldType.GetAttribute<FlagsAttribute>() != null)
			{
				if (!IsFlagsValid(info.FieldType, prop.intValue))
				{
					ResetEnumValue(info, prop);
				}
			}
			else if (!IsEnumValueDefined(info.FieldType, prop.intValue))
			{
				ResetEnumValue(info, prop);
			}
		}

		private static void ResetEnumValue(FieldInfo info, SerializedProperty prop)
		{
			var values = Enum.GetValues(info.FieldType);
			if (values.Length > 0)
			{
				prop.enumValueIndex = 0;
				prop.intValue = Convert.ToInt32(values.GetValue(0));
				prop.serializedObject.ApplyModifiedProperties();
			}
		}

		private static bool IsFlagsValid(Type enumType, int value)
		{
			bool valid = true;
			int maxBit = Convert.ToInt32(Math.Pow(2, Math.Ceiling(Math.Log(value) / Math.Log(2)))) >> 2;
			int i = 1;
			do
			{
				int ordinalValue = (1 << i);
				if (0 != (value & ordinalValue))
				{
					valid = (Enum.IsDefined(enumType, ordinalValue));
					if (!valid)
					{
						break;
					}
				}

				i++;
			} while (maxBit > i);

			return valid;
		}

		private static bool IsEnumValueDefined(Type enumType, int value)
		{
			object valueObject = null;
			var innerType = Enum.GetUnderlyingType(enumType);
			if (innerType == typeof(byte))
			{
				valueObject = (byte)value;
			}

			valueObject = valueObject ?? value;
			return Enum.IsDefined(enumType, valueObject);
		}

		public static void SetExpandedState(string path, bool isExpanded)
		{
			if (isExpanded)
				m_ExpandedStateSet.Add(path);
			else
				m_ExpandedStateSet.Remove(path);
		}

		public static bool GetExpandedState(string path)
		{
			return m_ExpandedStateSet.Contains(path);
		}
	}

	public class ErrorElement : VisualElement
	{
		private readonly string m_ErrorText;

		public ErrorElement(string label, string errorText)
		{
			style.flexDirection = FlexDirection.Row;
			Add(new Label(label));
			var btn = new Button();
			btn.text = "?ERROR?";
			btn.style.backgroundColor = Color.red;
			btn.style.unityTextAlign = TextAnchor.MiddleCenter;
			btn.style.width = 70;
			btn.clicked += ShowMenu;
			m_ErrorText = errorText;
			Add(btn);
			PFLog.Default.Error($"Fail create inspector field. [{label}] {errorText}");
		}

		private void ShowMenu()
		{
			EditorGUIUtility.systemCopyBuffer = m_ErrorText;
			Debug.LogError(m_ErrorText);
		}
	}
}