using Kingmaker.RuleSystem.Rules.Modifiers;
using Kingmaker.UnitLogic.Mechanics;
using Owlcat.Editor.Core.Utility;
using RectEx;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
	[CustomPropertyDrawer(typeof(ContextValue), true)]
	public class ContextValueDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawContextValueProperty(position, property, label);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public static void DrawContextValueProperty(Rect position, SerializedProperty property, GUIContent label)
		{
		    if (property.hasMultipleDifferentValues)
		    {
		        EditorGUI.LabelField(position, label, new GUIContent("- multiple -"));
		        return;
		    }
			var type = property.FindPropertyRelative(nameof(ContextValue.ValueType));
			var value = property.FindPropertyRelative(nameof(ContextValue.Value));
			var rank = property.FindPropertyRelative(nameof(ContextValue.ValueRank));
			var shared = property.FindPropertyRelative(nameof(ContextValue.ValueShared));
			var unitProperty = property.FindPropertyRelative(nameof(ContextValue.Property));
			var customUnitProperty = property.FindPropertyRelative("m_CustomProperty");
			var propertyName = property.FindPropertyRelative(nameof(ContextValue.PropertyName));

			bool enabled = true;
			if (property.FindPropertyRelative(nameof(ContextValueModifier.Enabled)) is {} enabledProperty)
			{
				var parts = position.CutFromRight(15);
				position = parts[0];

				using (GuiScopes.FixedWidth(1f, 0f))
				{
					enabled = enabledProperty.boolValue = EditorGUI.Toggle(parts[1], GUIContent.none, enabledProperty.boolValue);
				}
			}

			bool prevEnabled = GUI.enabled;
			try
			{
				GUI.enabled = enabled;
				
				if (property.FindPropertyRelative(nameof(ContextValueModifierWithType.ModifierType)) is {} typeProperty)
				{
					var parts = position.CutFromRight(75);
					position = parts[0];

					using (GuiScopes.FixedWidth(1f, 0f))
					{
						typeProperty.enumValueIndex = (int)(ModifierType)EditorGUI.EnumPopup(
							parts[1], (ModifierType)typeProperty.enumValueIndex);
					}
				}

				Rect[] chunkPositions;
				if (label != GUIContent.none)
				{
					var labelAndFieldPositions = position.CutFromLeft(EditorGUIUtility.labelWidth);
					EditorGUI.LabelField(labelAndFieldPositions[0], label);
					chunkPositions = labelAndFieldPositions[1].Row(new[] {2f, 1f});
				}
				else
				{
					chunkPositions = position.Row(new[] {2f, 1f});
				}

				SerializedProperty p;
				switch ((ContextValueType)type.intValue)
				{
					case ContextValueType.Simple:
					case ContextValueType.CasterBuffRank:
					case ContextValueType.TargetBuffRank:
						p = value;
						break;
					case ContextValueType.Rank:
						p = rank;
						break;
					case ContextValueType.Shared:
						p = shared;
						break;
					case ContextValueType.CasterProperty:
					case ContextValueType.TargetProperty:
						p = unitProperty;
						break;
					case ContextValueType.CasterCustomProperty:
					case ContextValueType.TargetCustomProperty:
						p = customUnitProperty;
						break;
					case ContextValueType.CasterNamedProperty:
					case ContextValueType.TargetNamedProperty:
					case ContextValueType.ContextProperty:
						p = propertyName;
						break;
					default:
						p = null;
						break;
				}

				using (GuiScopes.FixedWidth(1f, 0f))
				{
					if (p != null)
					{
						EditorGUI.PropertyField(chunkPositions[0], p, GUIContent.none);
					}
					else
					{
						EditorGUI.LabelField(chunkPositions[0], "unsupported type");
					}

					EditorGUI.PropertyField(chunkPositions[1], type, GUIContent.none);
				}
			}
			finally
			{
				GUI.enabled = prevEnabled;
			}
		}
	}

	[CustomPropertyDrawer(typeof(ContextDiceValue), true)]
	public class ContextDiceValueDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var diceCount = property.FindPropertyRelative(nameof(ContextDiceValue.DiceCountValue));
			var diceType = property.FindPropertyRelative(nameof(ContextDiceValue.DiceType));
			var bonus = property.FindPropertyRelative(nameof(ContextDiceValue.BonusValue));
			
			var labelAndFieldPositions = position.CutFromLeft(EditorGUIUtility.labelWidth);
			EditorGUI.LabelField(labelAndFieldPositions[0], label);

			var chunkPositions = labelAndFieldPositions[1].Row(new[] {2.5f, 1f, 2.5f });
			
			ContextValueDrawer.DrawContextValueProperty(chunkPositions[0], diceCount, GUIContent.none);
			using (GuiScopes.FixedWidth(13f, 0f))
			{
				EditorGUI.PropertyField(chunkPositions[1], diceType, new GUIContent("d"));
				ContextValueDrawer.DrawContextValueProperty(chunkPositions[2], bonus, new GUIContent("+"));
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUIUtility.singleLineHeight;
		}
	}
}