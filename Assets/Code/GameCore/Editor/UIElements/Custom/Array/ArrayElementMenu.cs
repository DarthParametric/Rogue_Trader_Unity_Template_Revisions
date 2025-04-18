using System;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.UIElements.Custom.Array
{
	public class ArrayElementMenu
	{
		public ArrayElementMenu(SerializedProperty array, Action onUpdate)
		{
			m_Array = new RobustSerializedProperty(array);
			m_OnUpdate = onUpdate;
			m_Menu = CreateMenu();
		}

		Action m_OnUpdate;

		ArrayElementComponent m_Context;

		RobustSerializedProperty m_Array;

		GenericMenu m_Menu;

		GenericMenu CreateMenu()
		{
			GenericMenu menu = new GenericMenu();

			if (m_Array.Property.propertyType == SerializedPropertyType.Generic)
			{
				menu.AddItem(new GUIContent("Move Up"), false, () =>
				{ PrototypedObjectEditorUtility.MoveArrayElement(m_Array, m_Context.ArrayElementIndex, m_Context.ArrayElementIndex - 1); PostSelect(); });
				menu.AddItem(new GUIContent("Move Down"), false, () =>
				{ PrototypedObjectEditorUtility.MoveArrayElement(m_Array, m_Context.ArrayElementIndex, m_Context.ArrayElementIndex + 1); PostSelect(); });
				menu.AddSeparator("");
			}

			var canResize = PrototypedObjectEditorUtility.CanResizeArrayProperty(m_Array);
			if (canResize)
			{
				menu.AddItem(new GUIContent("Remove"), false, () => RemoveElement(m_Context.ArrayElementIndex));
				menu.AddItem(new GUIContent("Add before"), false, () =>
				{ PrototypedObjectEditorUtility.AddArrayElement(m_Array, m_Context.ArrayElementIndex); PostSelect(); });
				menu.AddItem(new GUIContent("Add after"), false, () =>
				{ PrototypedObjectEditorUtility.AddArrayElement(m_Array, m_Context.ArrayElementIndex + 1); PostSelect(); });
			}

			if (m_Array.Property.propertyType == SerializedPropertyType.Generic)
			{
				if (canResize)
					menu.AddSeparator("");
				menu.AddItem(new GUIContent("Copy"), false, () =>
				{ PrototypedObjectEditorUtility.CopyArrayElement(m_Array, m_Context.ArrayElementIndex); PostSelect(); });
				menu.AddItem(new GUIContent("Paste"), false, () =>
				{ PrototypedObjectEditorUtility.PasteArrayElement(m_Array, m_Context.ArrayElementIndex); PostSelect(); });
			}

			return menu;
		}

		public void MoveUp(int index)
		{
			if (index - 1 >= 0)
			{
				PrototypedObjectEditorUtility.MoveArrayElement(m_Array, index, index - 1);
				PostSelect();
			}
		}

		public void MoveDown(int index)
		{
			if (m_Array.Property.arraySize > index + 1)
			{
				PrototypedObjectEditorUtility.MoveArrayElement(m_Array, index, index + 1); 
				PostSelect();
			}
		}

		public void RemoveElement(int index)
		{
			if (PrototypedObjectEditorUtility.CanResizeArrayProperty(m_Array))
			{
				PrototypedObjectEditorUtility.RemoveArrayElement(m_Array, index);
				PostSelect();
			}
		}

		void PostSelect()
		{
			m_OnUpdate();
			m_Context = default;
		}

		public void ShowMenu(ArrayElementComponent item)
		{
			m_Context = item;
			m_Menu.ShowAsContext();
		}
	}
}