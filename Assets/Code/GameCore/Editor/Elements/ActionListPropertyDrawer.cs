#if UNITY_EDITOR
using System;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Editor.UIElements.Custom;
using Kingmaker.Editor.Utility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.ElementsSystem;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Elements
{
	[CustomPropertyDrawer(typeof(ActionList))]
	public class ActionListPropertyDrawer : ElementsBaseDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new ElementListProperty<GameAction>(property, "Actions");

		protected override void HandleOnGUI(Type elementType, Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.hasMultipleDifferentValues)
			{
				EditorGUILayout.LabelField(label, new GUIContent("- multiple -"));
				return;
			}

			var list = property.FindPropertyRelative("Actions");
			var rsp = new RobustSerializedProperty(list);
			var owner = rsp.targetObject.MaybeOwner();

			if (NodeEditorBase.Drawing)
			{
				using (GuiScopes.Horizontal())
				{
					GUILayout.FlexibleSpace();
					GUILayout.Label(label);
					DrawAddButton(typeof(GameAction), list);
					NodeEditorBase.OnPropertyDrawn(property);
				}

				Rect lastRect = GUILayoutUtility.GetLastRect();
				ProcessDragAndDrop(elementType, owner, rsp, lastRect, clearProperty: false);
				return;
			}

			FoldoutResult foldout;
			GUIStyle style = ElementDragAndDropController.HasFactories(typeof(GameAction))
				? ElementDragAndDropController.PreDropStyle
				: null;
			//Hack for drag and drop mechanism. 
			//Foldout consumes drag and drop events. As workaround - memorize rect before foldout.
			var rect = EditorGUILayout.BeginHorizontal();
			if (list.arraySize == 1)
			{
				string caption = null;
				string description = null;
				try
				{
                    var action = (FieldFromProperty.GetFieldValue(list.GetArrayElementAtIndex(0)) as GameAction);
                    caption = action.GetCaptionSafe();
                    description = action.GetDescriptionSafe();
                }
				catch (Exception e)
				{
					PFLog.Default.Exception(e);
				}
				foldout = Foldout(property, ": " + caption, description, style, true);
			}
			else
			{
				foldout = Foldout(property, list.arraySize, style, true);
			}

			GUILayout.FlexibleSpace();
			PrototypedObjectEditorUtility.DrawDescriptionButton(property);
			EditorGUILayout.EndHorizontal();
			ProcessDragAndDrop(elementType, owner, rsp, rect, clearProperty: false);

			if (GUI.GetNameOfFocusedControl() == MakeFoldoutId(property))
				CopyPasteController.Process(typeof(GameAction), list);

			using (var content = ContentScope(foldout))
			{
				if (!content.Foldout)
					return;
				EditorGUI.indentLevel++;
				DrawList(Color.white, typeof(GameAction), list);
				EditorGUI.indentLevel--;
			}
		}

		protected override Type GetElementType(SerializedProperty property)
		{
			return typeof(GameAction);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 0;
		}
	}
}
#endif