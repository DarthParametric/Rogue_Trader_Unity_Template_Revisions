using System;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.Attributes;
using Owlcat.Runtime.Core.Utility.EditorAttributes;
using UnityEngine;
using UnityEngine.UIElements;
using HelpBox = Kingmaker.Assets.Editor.UIElements.Custom.Elements.HelpBox;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatContentContainer : OwlcatVisualElement
	{
		public new void Add(VisualElement element)
		{
			var property = element as OwlcatProperty;
            bool imguiDrawersNotUsed = property?.Attributes != null && !property.IsImguiWrapper();
            if (imguiDrawersNotUsed)
			{
				var header = GetAttribute<HeaderAttribute>(property);
				if (header != null)
				{
					base.Add(new HeaderElement(header));
				}

				var visibility = GetAttribute<ConditionalAttribute>(property);
				if (visibility != null)
				{
					element = new ConditionalVisibilityElement(property, visibility);
				}

				var infoBox = GetAttribute<InfoBoxAttribute>(property);
				if (infoBox != null)
				{
					base.Add(new HelpBox(infoBox.Text));
				}
			}

			base.Add(element);
        }

		private static T GetAttribute<T>(OwlcatProperty property) where T : Attribute
			=> (T)property.Attributes?.FirstItem(a => a is T);
	}
}