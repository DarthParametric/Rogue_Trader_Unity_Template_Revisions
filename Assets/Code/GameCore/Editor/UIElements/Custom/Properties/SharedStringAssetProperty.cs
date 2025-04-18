using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class SharedStringAssetProperty : OwlcatProperty
	{
		private LocalizedStringProperty m_LocProp;

		private ObjectField m_RefField;

		public SharedStringAssetProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
			var asset = PropertyResolver.GetPropertyObject<SharedStringAsset>(Property);
			var field = new ObjectField()
			{
				label = property.displayName,
				objectType = typeof(SharedStringAsset),
				value = asset
			};

			Add(field);
			UpdateView(asset);
			AddComponent(new CleanHandler(m_RefField));
		}

		protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
		{
			m_RefField.RegisterValueChangedCallback(x => UpdateView(x.newValue as SharedStringAsset));
		}

		private void UpdateView(SharedStringAsset value)
		{
			if (m_LocProp != null)
			{
				Remove(m_LocProp);
			}

			if (value != null)
			{
				var str = new SerializedObject(value).FindProperty("String");
				m_LocProp = new LocalizedStringProperty(str);
				Add(m_LocProp);
			}
		}

		private class CleanHandler : OwlcatPropertyComponent, IOwlcatPropertyInputHandler
		{
			private readonly ObjectField m_Field;

			int IOwlcatPropertyInputHandler.Order { get; } = 0;

			public CleanHandler(ObjectField field)
			{
				m_Field = field;
			}

			void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
			{
				if (evt.keyCode == KeyCode.Delete && m_Field.value != null)
				{
					m_Field.value = null;
					evt.StopPropagation();
				}
			}
		}
	}
}