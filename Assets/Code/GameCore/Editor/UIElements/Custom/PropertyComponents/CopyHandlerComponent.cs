using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class CopyHandlerComponent : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
	{
		int IOwlcatPropertyInputHandler.Order { get; } = 1;

		void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.C && evt.ctrlKey)
			{
				CopyPasteController.CopyProperty(Property.Property, null);
				evt.StopPropagation();
			}
		}
	}
}