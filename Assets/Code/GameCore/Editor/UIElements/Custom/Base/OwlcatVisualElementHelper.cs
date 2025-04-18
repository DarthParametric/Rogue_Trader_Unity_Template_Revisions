using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Owlcat.Runtime.Core.Logging;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Kingmaker.Utility.UnityExtensions;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public static class OwlcatVisualElementHelper
	{
		private static readonly LogChannel Logger = LogChannelFactory.GetOrCreate("Inspector");
		public static IEnumerable<OwlcatProperty> GetAllProperties(this VisualElement root)
		{
			if (root is OwlcatProperty property)
			{
				yield return property;
			}

			if (root.style.display == DisplayStyle.None)
			{
				yield break;
			}

			foreach (var e in root.Children())
			{
				foreach (var ee in e.GetAllProperties())
				{
					yield return ee;
				}
			}
		}

		[CanBeNull]
		public static OwlcatProperty GetFocusedProperty(this FocusController focusController)
		{
			var p = focusController?.focusedElement as VisualElement;
			while (p != null && !(p is OwlcatProperty))
			{
				p = p.parent;
			}

			return (OwlcatProperty)p;
		}

		[CanBeNull]
		public static Focusable GetFirstFocusableTitle(this VisualElement ve)
		{
			if (ve.style.display == DisplayStyle.None)
			{
				return null;
			}

			foreach (var child in ve.hierarchy.Children())
			{
				if (child.focusable && child is OwlcatTitleLabel)
				{
					return child;
				}

				var hierarchy = child.hierarchy;
				int num;
				if (hierarchy.parent != null)
				{
					var contentContainer = child.hierarchy.parent.contentContainer;
					num = child == contentContainer ? 1 : 0;
				}
				else
				{
					num = 0;
				}

				bool flag = num != 0;
				if (!flag)
				{
					var firstFocusableChild = GetFirstFocusableTitle(child);
					if (firstFocusableChild != null)
					{
						return firstFocusableChild;
					}
				}
			}

			return null;
		}

		public static OwlcatProperty WrapToOwlcatProperty(this VisualElement element, SerializedProperty property)
		{
			if (element is OwlcatProperty result && result.PropertyPath == property.propertyPath)
			{
				return result;
			}
			
			Logger.Warning("Wrapping element {0} created for property {1} with OwlcatProperty. Performance warning.", element, property);

			result = new OwlcatProperty(property);
			if (element is PropertyField propertyField && !propertyField.label.IsNullOrEmpty())
			{
				result.TitleLabel.text = propertyField.label;
				propertyField.label = string.Empty;
			}

			element.AddToClassList("owlcat-inner-field");
			result.ContentContainer.Add(element);
			
			element.OwlcatBind(property.serializedObject);

			return result;
		}

		public static bool IsImguiWrapper(this VisualElement element)
			=> element.childCount == 1 && element[0] is IMGUIField;

		public static IEnumerable<VisualElement> GetAllChildren(this VisualElement element)
		{
			return element.Children().Concat(element.Children().SelectMany(c => c.GetAllChildren()));
		}
	}
}