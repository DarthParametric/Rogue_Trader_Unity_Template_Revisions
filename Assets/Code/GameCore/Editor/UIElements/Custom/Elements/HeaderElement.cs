using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class HeaderElement : Label
	{
		private const int HeaderSpace = 12;

		public HeaderElement(HeaderAttribute headerAtt) : this(headerAtt.header)
		{
		}
        
        public HeaderElement(string headerText) : base(headerText)
		{
			style.marginTop = HeaderSpace;
			style.unityFontStyleAndWeight = FontStyle.Bold;
			AddToClassList("unity-base-field");
		}
	}
}