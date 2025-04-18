using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Elements;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Localization.Shared;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Properties
{
	public class LocalizedStringProperty : OwlcatProperty
	{
		private readonly LocalizedString m_LocString;

		private TextField m_TextField;

		private VisualElement m_SharedPart;

		private VisualElement m_SharedBtn;
		private VisualElement m_NotSharedPart;
		private VisualElement m_FixUpPart;

		private ObjectField m_SharedField;
		private Foldout m_CommentFoldout;
		private TraitsPartElement m_TraitsPart;

		public LocalizedStringProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			name = Property.displayName;
			m_LocString = PropertyResolver.GetPropertyObject<LocalizedString>(Property);
			m_TraitsPart = new TraitsPartElement(m_LocString, property);
			m_TraitsPart.UpdateData();

			AddToClassList("owlcat-box");
			AddToClassList("localizedString");
			TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			HeaderContainer.Add(LabelPart());
			ContentContainer.Add(TextField());
			ContentContainer.Add(m_TraitsPart);
			ContentContainer.Add( CommentField());
			ContentContainer.Add(ButtonsPart());
			CheckSharedState();
#endif
		}

#if UNITY_EDITOR && EDITOR_FIELDS
		private VisualElement CommentField()
		{
			m_CommentFoldout = new Foldout { text = "Comment", value = false, viewDataKey = PropertyPath + ".Comment"};
			UpdateCommentHeader(false);
			m_CommentFoldout.style.textOverflow = TextOverflow.Ellipsis;
			m_CommentFoldout.AddToClassList("localizedString");
			m_CommentFoldout.style.paddingLeft = 15;
			m_CommentFoldout.Add( new LocalizedStringCommentProperty(Property));
			m_CommentFoldout.RegisterValueChangedCallback(OnCommentFoldoutValueChanged);
			return m_CommentFoldout;
		}

		private void OnCommentFoldoutValueChanged(ChangeEvent<bool> evt)
		{
			UpdateCommentHeader(evt.newValue);
		}

		private void UpdateCommentHeader(bool isUnfolded)
		{
			if (isUnfolded)
			{
				m_CommentFoldout.text = "Comment";
			}
			else
			{
				string input = m_LocString.GetCommentOnCurrentLocale();
				if (string.IsNullOrEmpty(input))
				{
					m_CommentFoldout.text = $"Comment [{LocalizationManager.Instance.CurrentLocale}] (Empty)";
				}
				else
				{
					string comment = $"Comment [{LocalizationManager.Instance.CurrentLocale}]  {input}";
					int maxLength = 40;
					var neededEllipsis = comment.Length > maxLength;
					m_CommentFoldout.text = comment.Substring(0, Math.Min(maxLength, comment.Length)) + (neededEllipsis ? "..." : string.Empty);
				}
			}
		}

		private static VisualElement LabelPart()
		{
			var root = new VisualElement { style = { flexDirection = FlexDirection.Row } };
			var names = Enum.GetValues(typeof(Locale)).Cast<Locale>().ToList();
			var locPopup = new PopupField<Locale>(names, LocalizationManager.Instance.CurrentLocale);
			locPopup.binding = new PropertyBind<Locale>(
				() => LocalizationManager.Instance.CurrentLocale,
				val => LocalizationManager.Instance.CurrentLocale = val,
				locPopup);

			root.Add(locPopup);
			return root;
		}

		private VisualElement TextField()
		{
			var locale = LocalizationManager.Instance.CurrentLocale;
			string oldText = m_LocString.GetText(locale);
			m_TextField = new OwlcatTextField { value = oldText, style = { whiteSpace = WhiteSpace.Normal }, multiline = true };
			m_TextField.RegisterValueChangedCallback(e =>
			{
				if (m_LocString.UpdateText(Property, LocalizationManager.Instance.CurrentLocale, e.newValue))
				{
					var name = Property.serializedObject.targetObject.name + "_" + Property.propertyPath;
					UndoManager.Instance.RegisterUndo(name + " edit", () => m_LocString.UpdateText(Property, LocalizationManager.Instance.CurrentLocale, e.previousValue));
					
					m_TraitsPart.UpdateData();
					((ScriptableWrapperBase)Property.serializedObject.targetObject).SetBlueprintDirty();
				}
			});

			LocalizationManager.Instance.LocaleChanged += UpdateLocText;
			m_TextField.RegisterCallback<DetachFromPanelEvent>(e => LocalizationManager.Instance.LocaleChanged -= UpdateLocText);

			return m_TextField;
		}

		/*void DrawTextField()
		{
			var locale = LocalizationManager.CurrentLocale;
			_locString.Init(_prop);
			var oldText = _locString.GetText(locale);
			var updatedText = EditorGUILayout.TextArea(oldText);
			if (_locString.UpdateText(_prop, locale, updatedText))
			{
				AssetValidator.Revalidate();
				var name = _prop.serializedObject.targetObject.name + "_" + _prop.propertyPath;
				UndoManager.Instance.RegisterUndo(
					name + " edit",
					() =>
					{
						_locString.UpdateText(_prop, locale, oldText);
						AssetValidator.Revalidate();
					}
				);
			}
		}*/

		private void UpdateLocText(Locale newLoc)
		{
			m_TextField.SetValueWithoutNotify(m_LocString.GetText(newLoc));
			UpdateCommentHeader(m_CommentFoldout.value);
		}

		private VisualElement ButtonsPart()
		{
			var fieldInfo = FieldFromProperty.GetFieldInfo(Property);
			var root = new VisualElement {name = "Button Part", style = {flexDirection = FlexDirection.Row}};

			var sharedBtn = new Button { text = "Set Shared" };
			sharedBtn.clicked += () =>
			{
				AssetPicker.ShowAssetPicker(
					typeof(SharedStringAsset),
					fieldInfo,
					shared =>
					{
						m_LocString.SetShared(Property, (SharedStringAsset)shared);
						CheckSharedState();
					}
				);
			};
			m_SharedBtn = sharedBtn;

			m_NotSharedPart = GetNotSharedPart(Property, fieldInfo);
			m_SharedPart = GetSharedPart(Property);

			root.Add(sharedBtn);
			root.Add(m_NotSharedPart);
			root.Add(m_SharedPart);

			var fixButton = new Button {text = "String is broken. Try to fix"};
			fixButton.clicked += () =>
			{
				m_LocString.Fix(Property);
				CheckSharedState();
			};
			m_FixUpPart = fixButton;
			root.Add(m_FixUpPart);

			return root;
		}

		private VisualElement GetNotSharedPart(SerializedProperty prop, FieldInfo fieldInfo)
		{
			var root = new VisualElement() { name = "NotSharedPart", style = { flexDirection = FlexDirection.Row } };
			var makeShareBtn = new Button() { text = "Make Shared" };
			makeShareBtn.clicked += () =>
			{
				SharedStringAssetPropertyDrawer.ShowCreator(prop, fieldInfo.GetAttribute<StringCreateWindowAttribute>());
				CheckSharedState();
			};

			var deleteBtn = new Button(() =>
			{
				m_LocString.ClearData();
				m_LocString.MarkDirty(prop);
				m_TraitsPart.UpdateData();
				UpdateLocText(LocalizationManager.Instance.CurrentLocale);
			}) { text = "Delete String" };

			root.Add(makeShareBtn);
			root.Add(deleteBtn);

			return root;
		}

		private VisualElement GetSharedPart(SerializedProperty prop)
		{
			var root = new VisualElement {name = "SharedPart", style = {flexDirection = FlexDirection.Row}};

			m_SharedField = new ObjectField
			{
				value = m_LocString.Shared,
				objectType = typeof(SharedStringAsset),
				allowSceneObjects = false,
				style = { flexGrow = new StyleFloat(1), flexShrink = new StyleFloat(1) }
			};

			var clearShared = new Button(() =>
			{
				m_LocString.SetShared(prop, null);
				m_SharedField.SetValueWithoutNotify(null);
				CheckSharedState();
			})
			{ text = "Clear Shared" };

			m_SharedField.RegisterValueChangedCallback(e =>
			{
				m_LocString.Shared = e.newValue as SharedStringAsset;
				m_TextField.value = m_LocString.GetText(LocalizationManager.Instance.CurrentLocale);
				m_LocString.MarkDirty(prop);
			});

			root.Add(clearShared);
			root.Add(m_SharedField);
			return root;
		}

		private void CheckSharedState()
		{
			bool isShared = m_LocString.Shared;
			bool needsFixUp = !m_LocString.Check(Property);
			bool showShared = !needsFixUp && isShared && Property.serializedObject.targetObject is not SharedStringAsset;
			bool showNotShared = !needsFixUp && !isShared && Property.serializedObject.targetObject is not SharedStringAsset;
			m_SharedBtn.style.display = !needsFixUp && Property.serializedObject.targetObject is not SharedStringAsset ? DisplayStyle.Flex : DisplayStyle.None;
			m_SharedPart.style.display = showShared ? DisplayStyle.Flex : DisplayStyle.None;
			m_NotSharedPart.style.display = showNotShared ? DisplayStyle.Flex : DisplayStyle.None;
			m_FixUpPart.style.display = needsFixUp ? DisplayStyle.Flex : DisplayStyle.None;
			m_TextField.isReadOnly = needsFixUp;
			UpdateLocText(LocalizationManager.Instance.CurrentLocale);
		}
#endif
	}
}