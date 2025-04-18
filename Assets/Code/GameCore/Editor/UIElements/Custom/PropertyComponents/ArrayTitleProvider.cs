using System;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class ArrayElementTitleProvider : OwlcatPropertyComponent, IOwlcatPropertyTitleProvider
	{
        private readonly ArrayElementComponent m_ArrayElementComponent;

        private readonly Func<int, string> m_TitleProvider;

		int IOwlcatPropertyTitleProvider.Order { get; } = 0;

        public ArrayElementTitleProvider(ArrayElementComponent arrayElementComp, Func<int, string> titleProvider)
		{
            m_TitleProvider = titleProvider;
			m_ArrayElementComponent = arrayElementComp;
		}

		string IOwlcatPropertyTitleProvider.GetTitle()
			=> m_TitleProvider(m_ArrayElementComponent.ArrayElementIndex);
	}
}