#if UNITY_EDITOR
using System;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Designers.EventConditionActionSystem.NamedParameters;
using Kingmaker.ElementsSystem;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Elements
{
	[CustomPropertyDrawer(typeof(ParametrizedContextSetter.ParamEvaluatorAttribute))]
	public class NamedParameterPropertyDrawer : ElementsBaseDrawer
	{
		protected override void HandleOnGUI(Type elementType, Rect position, SerializedProperty property, GUIContent label)
		{
		    if (property.hasMultipleDifferentValues)
		    {
		        EditorGUILayout.LabelField(label, new GUIContent("- multiple -"));
                return;
		    }

		    if (elementType == null)
		    {
		        EditorGUILayout.LabelField("Error","Cannot parse entry type");
                return;
		    }

		    EditorGUI.indentLevel++;
		    DrawElement(elementType, property, null, 0, label);
		    EditorGUI.indentLevel--;
		}

		protected override Type GetElementType(SerializedProperty property)
		{
			var entryProp = property.GetParent();
			var typeProp = entryProp?.FindPropertyRelative("Type");

			if (typeProp == null)
				return null;

			var type = (ParametrizedContextSetter.ParameterType)typeProp.intValue;
			Type evalType;
			switch (type)
			{
				case ParametrizedContextSetter.ParameterType.Unit:
					evalType = typeof(AbstractUnitEvaluator);
					break;
				case ParametrizedContextSetter.ParameterType.Locator:
					evalType = typeof(LocatorEvaluator);
					break;
				case ParametrizedContextSetter.ParameterType.MapObject:
					evalType = typeof(MapObjectEvaluator);
					break;
				case ParametrizedContextSetter.ParameterType.Position:
					evalType = typeof(PositionEvaluator);
					break;
				case ParametrizedContextSetter.ParameterType.Blueprint:
					evalType = typeof(BlueprintEvaluator);
					break;
				case ParametrizedContextSetter.ParameterType.Float:
					evalType = typeof(FloatEvaluator);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			return evalType;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
}
#endif