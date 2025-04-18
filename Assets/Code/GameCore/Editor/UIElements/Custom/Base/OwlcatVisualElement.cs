using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatVisualElement : VisualElement
	{
		public OwlcatInspectorRoot Root
		{
			get
			{
				VisualElement p = this;
				while (p != null && !(p is OwlcatInspectorRoot))
				{
					p = p.parent;
				}

				return (OwlcatInspectorRoot)p;
			}
		}
	}
}