using System;
using System.Collections.Generic;
using Kingmaker.Editor.Elements.SmartElementPopulation.Factories;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor
{
	public class ElementFactoryWithSourcePicker : ValuePicker<ElementFactoryWithSource>
	{
		public static void Show(
			Rect rect,
			string buttonText,
			Func<IEnumerable<ElementFactoryWithSource>> valuesCollector,
			Action<ElementFactoryWithSource> callback)
		{
			Button(GetWindow<ElementFactoryWithSourcePicker>, rect, buttonText, valuesCollector, callback, true);
		}

		public static void Show(
			VisualElement source,
			string buttonText,
			Func<IEnumerable<ElementFactoryWithSource>> valuesCollector,
			Action<ElementFactoryWithSource> callback)
		{
			ShowPickerMenu(source, GetWindow<ElementFactoryWithSourcePicker>, buttonText, valuesCollector, callback);
		}
	}
}