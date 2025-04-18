using System;
using System.Reflection;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Utility.Attributes;
using Kingmaker.Visual.CharacterSystem;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(Skeleton))]
	[CustomPropertyDrawer(typeof(EquipmentEntity))]
	[CustomPropertyDrawer(typeof(StringsContainer), true)]
	[CustomPropertyDrawer(typeof(AssetPickerAttribute))]
	public class AssetPickerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var attr = fieldInfo.GetCustomAttribute<AssetPickerAttribute>();
			Func<AssetPicker.HierarchyEntry, bool> filter = null;
			if (!string.IsNullOrEmpty(attr?.Path))
				filter = he => he.Path.Contains(attr?.Path);

            AssetPicker.ShowPropertyField(
				position, property, fieldInfo,
				label, fieldInfo.FieldType,
				filter
			);
		}
	}
}