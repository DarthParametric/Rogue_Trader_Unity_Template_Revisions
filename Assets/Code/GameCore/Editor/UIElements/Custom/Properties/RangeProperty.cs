// using Kingmaker.Editor.UIElements.CustomElements.Base;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine.UIElements;
//
// namespace Kingmaker.Editor.UIElements.CustomElements
// {
// 	public class RangeProperty : OwlcatProperty
// 	{
// 		public RangeProperty(SerializedProperty property) : base(property)
// 		{
// 			AddToClassList("unity-base-field");
// 			style.flexDirection = FlexDirection.Row;
// 			
// 			// var label = new Label(property.displayName);
// 			// label.AddToClassList("owlcat-label");
// 			// label.focusable = true;
// 			// Add(label);
//
// 			var slider = property.propertyType == SerializedPropertyType.Float
// 				? (VisualElement)new Slider(property.displayName)
// 				: new SliderInt(property.displayName);
// 			slider.style.width = new StyleLength(StyleKeyword.Auto);
// 			slider.style.flexGrow = 1;
// 			Add(slider);
//
// 			var field = property.propertyType == SerializedPropertyType.Float
// 				? (VisualElement)new FloatField()
// 				: new IntegerField();
// 			field.style.width = 50;
// 			Add(field);
// 		}
// 	}
// }