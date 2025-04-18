using System.Linq;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.ElementsSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class ConditionListProperty : ElementListProperty<Condition>
	{
		public ConditionListProperty(SerializedProperty property, string listPropName) : base(property, listPropName)
		{
			var operProp = property.FindPropertyRelative("Operation");
			if (operProp != null)
			{
				var oper = new EnumField(string.Empty);
				oper.style.flexShrink = 1;
				oper.style.width = 50;
                oper.BindProperty(operProp);
				Array.HeaderContainer.Add(oper);
			}
		}
	}

	public class ElementListProperty<T> : OwlcatProperty where T : Element
	{
		public ElementListProperty(SerializedProperty property, string listPropName) : base(property, Layout.Vertical, false, true)
		{
			var list = property.FindPropertyRelative(listPropName);
			var types = TypeUtility.GetValidTypes(list, typeof(T)).ToArray();
			Array = new OwlcatArrayProperty(list, types, property.displayName);
			ContentContainer.Add(Array);
			HeaderContainer.style.display = DisplayStyle.None;
			Array.AddComponent(new PasteHandlerComponent(typeof(T), Array.RebuildContent));
			Array.AddComponent(new CopyHandlerComponent());
		}

		protected OwlcatArrayProperty Array;
	}
}