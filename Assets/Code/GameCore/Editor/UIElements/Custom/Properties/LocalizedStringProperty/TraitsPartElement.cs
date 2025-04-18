using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Localization.Shared;
using System;
using System.Linq;
using Kingmaker.Editor.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class TraitsPartElement : VisualElement
	{
		public TraitsPartElement(LocalizedString locString, SerializedProperty prop)
		{
			m_LocString = locString;
			m_Property = prop;

			var traitsRoot = new Foldout { text = "Traits", value = false, visible = true};
			traitsRoot.AddToClassList("localizedString");
			traitsRoot.style.paddingLeft = 15;

			m_LocaleTraitsPart = new VisualElement { style = { flexDirection = FlexDirection.Row } };
			m_StringPart = new VisualElement { style = { flexDirection = FlexDirection.Row } };

			m_LocaleTraitsPart.AddToClassList("baseColoredBorder");
			m_StringPart.AddToClassList("baseColoredBorder");

			var buttonsRoot = GetButtonsPart();

			traitsRoot.Add(buttonsRoot);
			traitsRoot.Add(m_StringPart);
			traitsRoot.Add(m_LocaleTraitsPart);
			Add(traitsRoot);

			RegisterCallback<AttachToPanelEvent>(e =>
			{
				LocalizationManager.Instance.LocaleChanged += FillLocaleTraits;
				OnAllUpdateEvent += UpdateData;
			});

			RegisterCallback<DetachFromPanelEvent>(e =>
			{
				LocalizationManager.Instance.LocaleChanged -= FillLocaleTraits;
				OnAllUpdateEvent -= UpdateData;
			});
		}

		private static event Action OnAllUpdateEvent = delegate { };

		private readonly LocalizedString m_LocString;

		private readonly SerializedProperty m_Property;

		private readonly VisualElement m_LocaleTraitsPart;

		private readonly VisualElement m_StringPart;

		public void UpdateData()
		{
			if (!m_LocString.IsEmpty())
			{
				style.display = DisplayStyle.Flex;
				FillStringTraits(m_StringPart);
				FillLocaleTraits(LocalizationManager.Instance.CurrentLocale);
			}
			else
			{
				style.display = DisplayStyle.None;
			}
		}

		private VisualElement GetButtonsPart()
		{
			var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };
			var updateBtn = new Button { text = "Update Current" };
			updateBtn.clicked += UpdateData;

			var updateAll = new Button { text = "Update All" };
			updateAll.clicked += () =>
			{
				TraitUtility.ReloadTraits();
				OnAllUpdateEvent();
			};

			root.Add(updateBtn);
			root.Add(updateAll);

			return root;
		}

		private static VisualElement GetRoot()
			=> new() {style = {flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap,}};

		private void FillStringTraits(VisualElement root)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			m_StringPart.Clear();

			var label = new OwlcatTitleLabel("String: ");
			var traitsRoot = GetRoot();
			string[] stringTraits = m_LocString.GetStringTraits();
			foreach (string trait in TraitUtility.StringTraits)
			{
				var btn = GetButton(stringTraits, true, trait);
				traitsRoot.Add(btn);
			}

			root.Add(label);
			root.Add(traitsRoot);
#endif
		}

		private void FillLocaleTraits(Locale locale)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			m_LocaleTraitsPart.Clear();

			string[] localeTraits = m_LocString.GetLocaleTraits(locale);

			var traitsRoot = GetRoot();
			foreach (string trait in TraitUtility.Values.Concat(TraitUtility.LocaleTraits).Distinct())
			{
				var btn = GetButton(localeTraits, false, trait);
				traitsRoot.Add(btn);
			}

			var label = new OwlcatTitleLabel("Locale: ");
			m_LocaleTraitsPart.Add(label);
			m_LocaleTraitsPart.Add(traitsRoot);
#endif
		}

		private VisualElement GetButton(string[] data, bool isString, string trait)
		{
			bool isChecked = data.IndexOf(trait) >= 0;
			var btn = new Button
			{
				text = trait,
			};
			if(isChecked)
				btn.AddToClassList("button-checked");
			btn.clicked += () => OnLocaleTraitClick(m_LocString, m_Property, btn, isString, trait);

			return btn;
		}

		private static void OnLocaleTraitClick(LocalizedString locString, SerializedProperty prop, VisualElement btn, bool isStringTrait, string traitText)
		{
			bool newValue = ToggleTrait(locString, prop, isStringTrait, traitText);
			if(newValue)
				btn.AddToClassList("button-checked");
			else
				btn.RemoveFromClassList("button-checked");
		}

		public static bool ToggleTrait(LocalizedString locString, SerializedProperty prop, bool isStringTrait, string traitText)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			if (isStringTrait)
			{
				bool exists = locString.HasTrait(traitText);
				locString.ToggleTrait(prop, traitText, !exists);
				return !exists;
			}
			else
			{
				var loc = LocalizationManager.Instance.CurrentLocale;
				bool exists = locString.HasTrait(traitText, loc); 
				locString.ToggleTrait(prop, traitText, !exists, loc);
				return !exists;
			}
#else
			return false;
#endif
		}
	}
}
