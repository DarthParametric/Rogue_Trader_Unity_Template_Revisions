using Kingmaker.Editor.UIElements.Custom.Base;
using System.Reflection;
using JetBrains.Annotations;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class OwlcatObjectProperty : OwlcatProperty
	{
		public OwlcatObjectProperty(SerializedProperty prop, [NotNull] FieldInfo info) : base(prop)
		{
			ObjectField = new OwlcatObjectField(prop.displayName, info);
			ObjectField.style.flexGrow = 1;
			ObjectField.style.flexShrink = 1;
			ContentContainer.Add(ObjectField);
			
			ObjectField.BindProperty(prop); // should be OwlcatBind, but it does not work for object fields because magic
		}

		public OwlcatObjectField ObjectField { get; }
	}

	public class OwlcatObjectField : ObjectField
	{
		public OwlcatObjectField(string label, [NotNull] FieldInfo fieldInfo) : base(label)
		{
			m_FieldINfo = fieldInfo;
			this.objectType = fieldInfo?.GetElementType();
			var info = typeof(OwlcatObjectField).GetMethod("get_visualInput", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty);
			var control = info.Invoke(this, null) as VisualElement;
			var point = control.hierarchy[1];
			control.Remove(point);

			var newPoint = new VisualElement();
			foreach (var clss in point.GetClasses())
			{
				newPoint.AddToClassList(clss);
			}

			style.flexGrow = 1;
			newPoint.RegisterCallback<MouseDownEvent>(OpenPicker);
			control.Add(newPoint);
		}

		void OpenPicker(MouseDownEvent evt)
		{
			AssetPicker.ShowAssetPicker(this.objectType, m_FieldINfo, x => value = x, value);
		}

		FieldInfo m_FieldINfo;
	}
}