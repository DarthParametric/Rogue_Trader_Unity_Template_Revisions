using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Utility.CodeTimer;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements
{
	public static class UICustomDrawerUtility
	{
		private readonly struct DrawerKeySet
		{
			public readonly Type Drawer;
			public readonly Type Type;

			public DrawerKeySet(Type drawer, Type type)
			{
				Drawer = drawer;
				Type = type;
			}
		}

		private static readonly Dictionary<Type, DrawerKeySet> DrawerTypeForType = new();

		// Called on demand
		private static void BuildDrawerTypeForTypeDictionary()
		{
			using var _ = ProfileScope.New("Init UIEPropertyUtility");

			foreach (var type in TypeCache.GetTypesDerivedFrom<GUIDrawer>())
			{
				//Debug.Log("Drawer: " + type);
				object[] attrs = type.GetCustomAttributes(typeof(CustomPropertyDrawer), true);
				foreach (CustomPropertyDrawer editor in attrs)
				{
					var drawerType = editor.GetType();
					var fieldInfo = drawerType.GetField("m_Type",
						BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
					var attributedType = fieldInfo?.GetValue(editor) as Type;
                 //   Debug.Log("Base type: " + attributedType);

					fieldInfo = drawerType.GetField("m_UseForChildren",
						BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
					var useForChildren = fieldInfo?.GetValue(editor) as bool?;
					if (attributedType == null || useForChildren == null)
						continue;

					DrawerTypeForType[attributedType] = new DrawerKeySet(type, attributedType);

					if (!useForChildren.Value)
						continue;

					var candidateTypes = TypeCache.GetTypesDerivedFrom(attributedType);
					foreach (var candidateType in candidateTypes)
					{
						//Debug.Log("Candidate Type: "+ candidateType);
						if (DrawerTypeForType.ContainsKey(candidateType)
							&& (attributedType.IsAssignableFrom(DrawerTypeForType[candidateType].Type)))
						{
							//  Debug.Log("skipping");
							continue;
						}

						//Debug.Log("Setting");
						DrawerTypeForType[candidateType] = new DrawerKeySet(type, attributedType);
					}
				}
			}
		}

		[CanBeNull]
		public static VisualElement TryGetCustomVisualElement(
			Type propertyType, PropertyAttribute attribute, FieldInfo info, SerializedProperty prop)
		{
			if (propertyType != null && propertyType.IsArray)
				return default;

			Type drawerType = GetDrawerTypeForType(propertyType);
			if (drawerType == null)
			{
				return default;
			}

			if (typeof(PropertyDrawer).IsAssignableFrom(drawerType))
			{
				var propertyDrawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
				var fieldInfo = drawerType.GetField("m_FieldInfo",
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
				// ReSharper disable once PossibleNullReferenceException
				fieldInfo.SetValue(propertyDrawer, info);

				if (attribute != null)
				{
					var attrField = drawerType.GetField("m_Attribute",
						BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
					// ReSharper disable once PossibleNullReferenceException
					attrField.SetValue(propertyDrawer, attribute);
				}

				var propField = propertyDrawer.CreatePropertyGUI(prop) ?? OwlcatProperty.CreateDefault(prop);
				propField?.Bind(prop.serializedObject);

				var suffixAttribute = drawerType.GetCustomAttribute<PropertyLabelSuffixAttribute>();
				if (suffixAttribute != null && propField is OwlcatProperty owlcatProperty)
					owlcatProperty.AddComponent(new CustomLabelSuffixProvider(suffixAttribute.Suffix));
				
				return propField;
			}
				
			if (typeof(PropertyDrawerFixed).IsAssignableFrom(drawerType))
			{
				var propertyDrawer = (PropertyDrawerFixed)Activator.CreateInstance(drawerType);
				propertyDrawer.Field = info;

				var propField = propertyDrawer.CreatePropertyGUI(prop);
				//propField?.Bind(prop.serializedObject);
				return propField;
			}

			return default;
		}

		private static Type GetDrawerTypeForType(Type type)
		{
			if (DrawerTypeForType == null || DrawerTypeForType.Count == 0)
				BuildDrawerTypeForTypeDictionary();

			DrawerKeySet drawerType;
			DrawerTypeForType.TryGetValue(type, out drawerType);
			if (drawerType.Drawer != null)
				return drawerType.Drawer;

			// now check for base generic versions of the drawers...
			if (type.IsGenericType)
				DrawerTypeForType.TryGetValue(type.GetGenericTypeDefinition(), out drawerType);

			return drawerType.Drawer;
		}
	}

	public class IMGUIField : PropertyField
	{
		public IMGUIField(SerializedProperty prop) : base(prop) { }
	}
	
	[AttributeUsage(AttributeTargets.Class)] 
	public class PropertyLabelSuffixAttribute : Attribute
	{
		public string Suffix { get; }

		public PropertyLabelSuffixAttribute(string suffix)
		{
			Suffix = suffix;
		}
	}
	
	public class CustomLabelSuffixProvider : OwlcatPropertyComponent, IOwlcatPropertyTitleProvider
	{
		private readonly string m_Suffix;
		
		public CustomLabelSuffixProvider(string suffix)
		{
			m_Suffix = suffix;
		}

		public string GetTitle()
		{
			return Property.TitleLabel.text + m_Suffix;
		}

		public int Order => 0;
	}
}