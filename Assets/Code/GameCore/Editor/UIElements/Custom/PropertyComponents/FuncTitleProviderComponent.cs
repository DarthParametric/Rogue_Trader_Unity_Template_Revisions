using System;
using Kingmaker.Editor.UIElements.Custom.Base;

namespace Kingmaker.Editor.UIElements.Custom.PropertyComponents
{
    public class FuncTitleProviderComponent : IOwlcatPropertyTitleProvider
    {
        public FuncTitleProviderComponent(Func<string> titleFunc)
        {
            m_TitleFunc = titleFunc;
        }

        private Func<string> m_TitleFunc;
        
        void IOwlcatPropertyComponent.AttachToProperty(OwlcatProperty property)
        {
        }

        string IOwlcatPropertyTitleProvider.GetTitle()
            => m_TitleFunc.Invoke();

        int IOwlcatPropertyTitleProvider.Order => 0;
    }
}
