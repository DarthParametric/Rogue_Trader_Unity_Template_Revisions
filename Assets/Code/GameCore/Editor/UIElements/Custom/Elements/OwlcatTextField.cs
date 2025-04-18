using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Elements
{
	public class OwlcatTextField : TextField
	{
		// ReSharper disable once InconsistentNaming
		public new bool multiline
		{
			get => base.multiline;
			set
			{
				base.multiline = value;
				if (value)
				{
					AddToClassList("owlcat-multiline");
				}
				else
				{
					RemoveFromClassList("owlcat-multiline");
				}
			}
		}

		// ReSharper disable once InconsistentNaming
		public new string value
		{
			get => base.value;
			set => base.value = value;
		}

		public OwlcatTextField()
		{
			AddToClassList("owlcat-text-area");
		}
	}
}