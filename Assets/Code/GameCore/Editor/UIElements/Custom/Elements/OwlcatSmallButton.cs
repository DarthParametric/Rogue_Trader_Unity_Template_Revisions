using System;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatSmallButton : Button
	{
		public OwlcatSmallButton() : this(null)
		{
		}

		public OwlcatSmallButton(Action clickEvent) : base(clickEvent)
		{
			AddToClassList("owlcat-button");
		}
	}
}