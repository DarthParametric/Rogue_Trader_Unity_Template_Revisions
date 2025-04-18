using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.ElementsSystem.Interfaces;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class CaptionTitleProvider : OwlcatPropertyComponent, IOwlcatPropertyTitleProvider
	{
		int IOwlcatPropertyTitleProvider.Order { get; } = 0;

		protected override void OnAttached()
		{
		}

		string IOwlcatPropertyTitleProvider.GetTitle()
		{
            string caption=null;
            try
            {
                if (Property.Property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var captionHolder = Property.Property.objectReferenceValue as IHaveCaption;
                    caption = captionHolder?.Caption;
                }
                if (Property.Property.propertyType == SerializedPropertyType.ManagedReference)
                {
                    var captionHolder = FieldFromProperty.GetFieldValue(Property.Property) as IHaveCaption;
                    caption = captionHolder?.Caption;
                }

            }
            catch (System.Exception x)
            {
                PFLog.Default.Exception(x);
                caption = x.Message;
            }
            var pn = Property.Property.IsArrayElement()
	            ? (Property.Property.GetIndexInParentArray()+1).ToString()
                : Property.Property.displayName;
            return caption != null ? $"{pn}: {caption}" : pn;
        }
	}
}
