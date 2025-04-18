using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatTitleLabel : Label
	{
		public OwlcatTitleLabel() : this(string.Empty)
		{
		}

		public OwlcatTitleLabel(string text) : base(text)
		{
			AddToClassList("owlcat-title-label");
			focusable = true;
		}
	}

	public class OwlcatTitleLabelSizeControl : Label
	{
		public OwlcatTitleLabelSizeControl() : base(" ")
		{
			AddToClassList("owlcat-title-label-size-control");
		}
	}
}