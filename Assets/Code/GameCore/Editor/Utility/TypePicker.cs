using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor
{
	public class TypePicker : ValuePicker<Type>
	{
	    public static void Button(
	        string buttonText,
	        Func<IEnumerable<Type>> valuesCollector,
	        Action<Type> callback,
	        bool showNow = false,
	        params GUILayoutOption[] options)
	    {
	        Button(
	            GetWindow<TypePicker>,
	            buttonText,
	            valuesCollector,
	            callback,
	            showNow,
	            GUI.skin.button,
	            options
            );
	    }

	    public static void ToolbarButton(
	        string buttonText,
	        Func<IEnumerable<Type>> valuesCollector,
	        Action<Type> callback,
	        bool showNow = false,
	        params GUILayoutOption[] options)
	    {
	        Button(
	            GetWindow<TypePicker>,
	            buttonText,
	            valuesCollector,
	            callback,
	            showNow,
                EditorStyles.toolbarDropDown,
	            options
	        );
	    }

		public static VisualElement CreatePickerButton(string buttonText, Func<IEnumerable<Type>> valuesCollector, Action<Type> callback)
			=> CreateButton(GetWindow<TypePicker>, buttonText, valuesCollector, callback);

		public static void Show(
			Rect rect,
			string buttonText,
			Func<IEnumerable<Type>> valuesCollector,
			Action<Type> callback)

		{
			Button(
				GetWindow<TypePicker>,
				rect,
				buttonText,
				valuesCollector,
				callback,
				true
			);
		}

		public static void ShowPickerWindow(VisualElement source, string winTitle, Func<IEnumerable<Type>> valuesCollector, Action<Type> callback)
			=> ShowPickerMenu(source, GetWindow<TypePicker>, winTitle, valuesCollector, callback);

		protected override string GetValueName(Type value)
		{
			return value.Name;
		}
	}
}