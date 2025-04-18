using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints.Base;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.Attributes;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public static class OwlcatArrayHelper
	{
		[CanBeNull]
		public static IOwlcatPropertyTitleProvider TryCreateElementCustomTitleProvider(
			this OwlcatArrayProperty array, OwlcatProperty element, ArrayElementComponent arrayElementComponent)
		{
			//Крашит, невозможно получить boxedValue в блюпринтах
			/*if (element.RobustProperty.Property.boxedValue is not ScriptableWrapperBase)
				return new ArrayElementTitleProvider(arrayElementComponent,
					index => $"[{index}] {element.RobustProperty.Property.boxedValue.GetType().Name}");*/

			foreach (var child in element.ContentContainer.Children())
			{
				var p = child as OwlcatProperty;
				if (p == null)
				{
					continue;
				}

				if (!p.Attributes.HasItem(a => a is ArrayElementNameProviderAttribute))
				{
					continue;
				}

				return new ArrayElementTitleProvider(
					arrayElementComponent,
					index => 
					{
						if (p.Property.propertyType == SerializedPropertyType.String)
						{
							return p.Property.stringValue;
						}

						if (p.Property.propertyType == SerializedPropertyType.Enum &&
						    p.Property.intValue < p.Property.enumDisplayNames.Length)
						{
							return p.Property.enumDisplayNames[p.Property.intValue];
						}

						return $"Element {index}";
					});
			}

			var titleProvider = array.Attributes?.OfType<ArrayElementNamePrefixAttribute>().FirstOrDefault();
			if (titleProvider != null)
			{
				return new ArrayElementTitleProvider(arrayElementComponent, titleProvider.GetName);
			}

			return new ArrayElementTitleProvider(
				arrayElementComponent,
				_ => element.Property.displayName);
		}
	}
}