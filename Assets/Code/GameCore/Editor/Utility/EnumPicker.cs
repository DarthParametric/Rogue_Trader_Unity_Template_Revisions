using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor
{
	public class EnumPicker : ValuePicker<Enum>
	{
		public static void Button(
			string buttonText,
			Func<IEnumerable<Enum>> valuesCollector,
			Action<Enum> callback,
			bool showNow = false,
			params GUILayoutOption[] options)
		{
			Button(
				GetWindow<EnumPicker>,
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
			Func<IEnumerable<Enum>> valuesCollector,
			Action<Enum> callback,
			bool showNow = false,
			params GUILayoutOption[] options)
		{
			Button(
				GetWindow<EnumPicker>,
				buttonText,
				valuesCollector,
				callback,
				showNow,
				EditorStyles.toolbarDropDown,
				options
			);
		}

		public static void Button(
			Rect rect,
			string buttonText,
			Func<IEnumerable<Enum>> valuesCollector,
			Action<Enum> callback,
			bool showNow = false,
			GUIStyle style = null)
		{
			Button(
				GetWindow<EnumPicker>,
				rect,
				buttonText,
				valuesCollector,
				callback,
				showNow,
				style
			);
		}
	}
}