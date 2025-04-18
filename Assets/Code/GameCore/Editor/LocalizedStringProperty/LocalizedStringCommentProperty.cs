using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class LocalizedStringCommentProperty : OwlcatProperty
	{
		private readonly LocalizedString m_LocString;

		private TextField m_TextField;

		public LocalizedStringCommentProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			name = Property.displayName;
			m_LocString = PropertyResolver.GetPropertyObject<LocalizedString>(Property);

			AddToClassList("owlcat-box");
			AddToClassList("localizedString");
			TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			ContentContainer.Add(TextField());
			CheckSharedState();
			OnLocaleChanged(LocalizationManager.Instance.CurrentLocale);
			LocalizationManager.Instance.LocaleChanged += OnLocaleChanged;
#endif
		}

#if UNITY_EDITOR && EDITOR_FIELDS
		private void OnLocaleChanged(Locale locale)
		{
			m_TextField.value = m_LocString.GetCommentOnCurrentLocale();
			if (locale == Locale.ruRU || locale == Locale.dev)
			{
				TitleLabel.text = "Comment"; 
				m_TextField.isReadOnly = false;
			}
			else
			{
				TitleLabel.text = $"Comment <color=red>Not editable in [{locale}]</color>";
				m_TextField.isReadOnly = true;
			}
		}


		private VisualElement TextField()
		{
			string oldText = m_LocString.GetCommentOnCurrentLocale();
			m_TextField = new OwlcatTextField { value = oldText, style = { whiteSpace = WhiteSpace.Normal }, multiline = true };
			m_TextField.RegisterValueChangedCallback(e =>
			{
				if (m_LocString.UpdateComment(Property, e.newValue))
				{
					string propName = Property.serializedObject.targetObject.name + "_" + Property.propertyPath;
					UndoManager.Instance.RegisterUndo(propName + " comment edit", () => m_LocString.UpdateComment(Property, e.previousValue));
				}
			});

			return m_TextField;
		}

		private void CheckSharedState()
		{
			bool needsFixUp = !m_LocString.Check(Property);
			m_TextField.isReadOnly = needsFixUp;
		}
#endif
	}
}