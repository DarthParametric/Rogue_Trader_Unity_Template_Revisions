using System;
using System.Collections.Generic;
using Kingmaker.Blueprints.Items.Armors;
using Kingmaker.EntitySystem.Properties;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Enums;
using Kingmaker.UnitLogic.Levelup.Selections;
using Kingmaker.UnitLogic.Mechanics.Damage;
using Kingmaker.Utility.Attributes;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Visual.HitSystem;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(EnumPickerAttribute))]
	[CustomPropertyDrawer(typeof(StatType))]
	[CustomPropertyDrawer(typeof(ModifierDescriptor))]
	[CustomPropertyDrawer(typeof(FeatureGroup))]
	[CustomPropertyDrawer(typeof(ArmorProficiencyGroup))]
	[CustomPropertyDrawer(typeof(UnitTag))]
	[CustomPropertyDrawer(typeof(HitLevel))]
	[CustomPropertyDrawer(typeof(BloodType))]
	[CustomPropertyDrawer(typeof(DamageType))]
	[CustomPropertyDrawer(typeof(WeaponCategory))]
	[CustomPropertyDrawer(typeof(UnitCondition))]
	[CustomPropertyDrawer(typeof(EntityProperty))]
	[CustomPropertyDrawer(typeof(MechanicsFeatureType))]
	public class EnumPickerDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect fieldRect;
			if (!string.IsNullOrEmpty(label.text))
			{
				var labelRect = new Rect(
					position.x, 
					position.y, 
					EditorGUIUtility.labelWidth, 
					position.height
				);
				fieldRect = new Rect(
					position.x + EditorGUIUtility.labelWidth,
					position.y,
					position.width - EditorGUIUtility.labelWidth,
					position.height
				);
				EditorGUI.LabelField(labelRect, label);
			}
			else
			{
				fieldRect = position;
			}

			var type = fieldInfo.FieldType;
			if (type.IsArray)
			{
				type = type.GetElementType();
			}

			IEnumerable<Enum> displayOrder;
			if (type == typeof(StatType))
			{
				displayOrder = StatTypeHelper.DisplayOrder;
			}
			else
			{
				displayOrder = EnumUtils.GetValues(type);
			}

			string currentName;
		    if (property.hasMultipleDifferentValues)
		    {
		        currentName = "[Multiple]";
            }
            else 
            {
                currentName = Enum.GetName(type, property.intValue)??$"<invalid> ({property.intValue})"; //valuesArray[property.intValue].ToString();
            }

			var p = new RobustSerializedProperty(property);
			EnumPicker.Button(
				fieldRect,
				currentName,
				() => displayOrder,
				e =>
				{
					using (GuiScopes.UpdateObject(p.serializedObject))
					{
						p.Property.intValue = Convert.ToInt32(e);
					}
				},
				style: EditorStyles.popup
			);
		}
	}
}