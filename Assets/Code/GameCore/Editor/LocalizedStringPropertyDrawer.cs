#if EDITOR_FIELDS
using System;
using System.Linq;
using Kingmaker.Editor.Localization;
using Kingmaker.Editor.UIElements;
using Kingmaker.Editor.UIElements.Custom.Properties;
using Kingmaker.Editor.Utility;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor
{
	[CustomPropertyDrawer(typeof(LocalizedString))]
	public class LocalizedStringPropertyDrawer : PropertyDrawer
	{
		private const float SplitterHeight = 0;
		private const float MinTextHeight = 63;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
			=> new LocalizedStringProperty(property);

		private bool m_ShowStringTraits;
		private bool m_ShowLocaleTraits;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.targetObjects.Length > 1)
			{
				EditorGUI.LabelField(position, label, new GUIContent("- multiple texts -"));
				return;
			}

			var localizedString = PropertyResolver.GetPropertyObject<LocalizedString>(property);
			if (localizedString == null)
			{
				return;
			}

			position.yMin += SplitterHeight;
			position.height -= 2 * EditorGUIUtility.singleLineHeight + SplitterHeight;
			if (localizedString.Shared != null)
			{
				label.text += " (Shared)";
			}
			bool isExpanded = LocalizationEditorGUI.LocalizedStringPropertyField(position, label, property, localizedString);
			if (!isExpanded)
			{
				return;
			}

			// voice over
			Rect pos = position;
			pos.yMin += pos.height;
			pos.height = EditorGUIUtility.singleLineHeight;

			pos.yMin += EditorGUIUtility.singleLineHeight;
			pos.height = EditorGUIUtility.singleLineHeight;
			pos.xMin += EditorGUI.indentLevel * 15f;
			pos.width = 100;


			var robustProp = new RobustSerializedProperty(property);
			if (!localizedString.Check(property))
			{
				var pos2 = pos;
				pos2.width = 200;
				if (GUI.Button(pos2, "String is broken. Try to fix"))
				{
					localizedString.Fix(property);
				}
				pos2.x += 200;
				pos2.width += 100;
				if (!localizedString.Shared)
				{
					if (GUI.Button(pos2, "Make new Shared String Duplicate", EditorStyles.miniButton))
					{
						var shared = SharedStringAssetPropertyDrawer.CreateShared(robustProp);
						localizedString.MakeNewShared(robustProp, shared, false);
					}
				}

				return;
			}

			{
				string[] stringTraits = localizedString.GetStringTraits();
				m_ShowStringTraits = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowStringTraits, "String traits");
				if (m_ShowStringTraits)
				{
					EditorGUILayout.BeginHorizontal();
					int rowLen = 0;
					foreach (string trait in TraitUtility.StringTraits)
					{
						bool isSet = stringTraits.IndexOf(trait) >= 0;
						bool newSet = GUILayout.Toggle(isSet, trait, GUI.skin.button);
						if (newSet != isSet)
							TraitsPartElement.ToggleTrait(localizedString, property, true, trait);
						if (rowLen++ >= 2)
						{
							rowLen = 0;
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
						}

					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.EndFoldoutHeaderGroup();
			}

			{
				var currentLocale = LocalizationManager.Instance.CurrentLocale;
				string[] localeTraits = localizedString.GetLocaleTraits(currentLocale);
				m_ShowLocaleTraits =
					EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowLocaleTraits, $"{currentLocale} traits");
				if (m_ShowLocaleTraits)
				{
					EditorGUILayout.BeginHorizontal();
					int rowLen = 0;
					foreach (string trait in TraitUtility.Values.Concat(TraitUtility.LocaleTraits).Distinct())
					{
						bool isSet = localeTraits.IndexOf(trait) >= 0;
						bool newSet = GUILayout.Toggle(isSet, trait, GUI.skin.button);
						if (newSet != isSet)
							TraitsPartElement.ToggleTrait(localizedString, property, false, trait);
						if (rowLen++ >= 2)
						{
							rowLen = 0;
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
						}

					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.EndFoldoutHeaderGroup();
			}


			// shared strings
			if (property.serializedObject.targetObject is SharedStringAsset)
				return;
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			try
			{
				if (GUI.Button(pos, "Set Shared", EditorStyles.miniButton))
				{
					AssetPicker.ShowAssetPicker(
						typeof(SharedStringAsset),
						fieldInfo,
						shared =>
						{
							localizedString.SetShared(robustProp, (SharedStringAsset)shared);
						}
					);
				}

				if (!localizedString.Shared)
				{
					pos.x += 100;
					if (GUI.Button(pos, "Make Shared", EditorStyles.miniButtonLeft))
					{
						var shared = SharedStringAssetPropertyDrawer.CreateShared(robustProp);
						localizedString.MakeNewShared(robustProp, shared);
					}
					pos.x += 100;
					if (GUI.Button(new Rect(pos.x, pos.y, 14, pos.height), "▾", EditorStyles.miniButtonRight))
					{
						var shared = SharedStringAssetPropertyDrawer.CreateSharedWithFolderDialog(robustProp, fieldInfo);
						if (shared)
							localizedString.MakeNewShared(robustProp, shared);
					}

					pos.x += 14;
					if (GUI.Button(pos, "Delete String", EditorStyles.miniButton))
					{
						localizedString.ClearData();
						localizedString.MarkDirty(property);
					}
				}
				else
				{
					pos.x += 100;
					if (GUI.Button(pos, "Clear Shared", EditorStyles.miniButton))
					{
						localizedString.SetShared(robustProp, null);
					}

					pos.x += 100;
					pos.x -= EditorGUI.indentLevel * 15f;
                    using (new EditorGUI.DisabledScope(localizedString.Shared))
					{
						EditorGUI.ObjectField(pos, localizedString.Shared, typeof(SharedStringAsset), false);
					}
                    if(localizedString.Shared && GUI.Button(pos,"", GUIStyle.none)) // ping string (built-in pinging is disabled by DisabledScope)
					{
                        EditorGUIUtility.PingObject(localizedString.Shared);
                    }
				}

				string path = localizedString.Shared?.String.JsonPath ?? localizedString.JsonPath;
				pos.x += 100;
				if (!path.IsNullOrEmpty() && GUI.Button(pos, "Show File", EditorStyles.miniButton))
				{
					EditorUtility.RevealInFinder(path);
				}
			}
			catch (Exception e)
			{
				PFLog.Default.Exception(e);
			}
			finally
			{
				EditorGUI.indentLevel = oldIndent;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.serializedObject.targetObjects.Length > 1)
			{
				return EditorGUIUtility.singleLineHeight;
			}

			if (!property.isExpanded)
			{
				return SplitterHeight * 2 + EditorGUIUtility.singleLineHeight;
			}

			var localizedString = PropertyResolver.GetPropertyObject<LocalizedString>(property);
			if (localizedString == null)
			{
				return 100;
			}
			float textHeight = 12f + LocalizationEditorGUI.TextAreaStyle.CalcHeight(
				new GUIContent(localizedString.GetText(LocalizationManager.Instance.CurrentLocale)),
				EditorGUIUtility.currentViewWidth - EditorGUI.indentLevel * 15
			);
			textHeight = Mathf.Max(textHeight, MinTextHeight);

			float h = SplitterHeight; // splitter
			h += EditorGUIUtility.singleLineHeight; // header
			h += textHeight; // text, huh
			h += EditorGUIUtility.singleLineHeight; // voice over sound
			h += EditorGUIUtility.singleLineHeight; // shared buttons

			return h;
		}
	}
}
#endif