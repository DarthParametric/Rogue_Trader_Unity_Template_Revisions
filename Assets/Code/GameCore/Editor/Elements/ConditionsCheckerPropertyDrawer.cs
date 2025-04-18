#if UNITY_EDITOR
using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Owlcat.Editor.Core.Utility;
using System;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Elements
{
	[CustomPropertyDrawer(typeof(ConditionsChecker))]
	public class ConditionsCheckerPropertyDrawer : ElementsBaseDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new ConditionListProperty(property, "Conditions");

		protected override void HandleOnGUI(
			Type elementType, Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.hasMultipleDifferentValues)
			{
				EditorGUILayout.LabelField(label, new GUIContent("- multiple -"));
				return;
			}

			var list = property.FindPropertyRelative("Conditions");
			var op = property.FindPropertyRelative("Operation");
			var rsp = new RobustSerializedProperty(list);
			var owner = rsp.targetObject.MaybeOwner();

			if (NodeEditorBase.Drawing)
			{
				using (GuiScopes.Horizontal())
				{
					GUILayout.FlexibleSpace();
					GUILayout.Label(label);
					using (GuiScopes.FixedWidth(20, 20))
					{
						EditorGUILayout.PropertyField(op, new GUIContent());
					}

					DrawAddButton(typeof(Condition), list);
					NodeEditorBase.OnPropertyDrawn(property);
				}

				ProcessDragAndDrop(
					elementType,
					owner,
					rsp,
					GUILayoutUtility.GetLastRect(),
					clearProperty: false
				);
				return;
			}

			//Hack for drag and drop mechanism. 
			//Foldout consumes drag and drop events. As workaround - memorize event type and rect before foldout.
			GUIStyle style = ElementDragAndDropController.HasFactories(elementType)
				? ElementDragAndDropController.PreDropStyle
				: null;
			var rect = EditorGUILayout.BeginHorizontal();
			var foldout = Foldout(property, list.arraySize, style, true);

			using (GuiScopes.FixedWidth(20, 20))
			{
				EditorGUILayout.PropertyField(op, new GUIContent());
			}

			property.serializedObject.ApplyModifiedProperties();

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			ProcessDragAndDrop(
				elementType,
				owner,
				rsp,
				rect,
				clearProperty: false
			);

			if (GUI.GetNameOfFocusedControl() == MakeFoldoutId(property))
				CopyPasteController.Process(typeof(Condition), list);

			using (var content = ContentScope(foldout))
			{
				if (!content.Foldout)
					return;
				Color color = op.intValue == 0 ? Color.green : Color.yellow;
				EditorGUI.indentLevel++;
				DrawList(color, typeof(Condition), list);
				EditorGUI.indentLevel--;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}

		protected override Type GetElementType(SerializedProperty property)
		{
			return typeof(Condition);
		}
	}
}
#endif