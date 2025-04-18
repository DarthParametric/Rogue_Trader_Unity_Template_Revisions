using UnityEngine.UIElements;

namespace Kingmaker.Assets.Editor.UIElements.Custom.Elements
{
    public class HelpBox : Box
    {
        public HelpBox(string text)
        {
            const float margin = 2;
            const float marginTop = 5;
            const float marginBottom = 0;
            const float padding = 1;
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.marginBottom = marginBottom;
            style.marginRight = margin;
            style.marginLeft = margin;
            style.marginTop = marginTop;
            style.paddingTop = padding;
            style.paddingBottom = padding;
            style.paddingRight = padding;
            style.paddingLeft = padding;
            
            Add(new Label(text) { style = {fontSize = new StyleLength(10), flexGrow = 1, whiteSpace = WhiteSpace.Normal } });
        }
    }
}