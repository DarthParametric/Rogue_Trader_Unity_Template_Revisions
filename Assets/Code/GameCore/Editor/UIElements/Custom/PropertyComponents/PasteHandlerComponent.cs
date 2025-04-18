using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class PasteHandlerComponent : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
	{
		private readonly Type m_ValidType;

		private readonly Action m_PastCallback;

		int IOwlcatPropertyInputHandler.Order { get; } = 1;
		
		public PasteHandlerComponent(Type validType, Action pastCallback)
		{
			m_PastCallback = pastCallback;
			m_ValidType = validType;
		}

		void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.V && evt.ctrlKey)
			{
				if (CopyPasteController.PasteProperty(m_ValidType, Property.Property))
				{
					m_PastCallback();
				}

				evt.StopPropagation();
			}
		}
	}
}