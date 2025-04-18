#if UNITY_EDITOR && EDITOR_FIELDS
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Localization;
using Owlcat.QA.Validation;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Utility.EditorPreferences;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.DialogSystem.Blueprints;

namespace Kingmaker.Editor
{
	public static class LocalizationEditorGUI
	{
		public static readonly GUIStyle TextAreaStyle = new(EditorStyles.textArea)
		{
			wordWrap = true,
			fontSize = 14
		};

		public static readonly GUIStyle BoldFoldoutStyle = new(EditorStyles.foldout)
		{
			fontStyle = FontStyle.Bold
		};

		private static readonly FieldInfo ActiveEditorField = typeof(EditorGUI).GetField("activeEditor", BindingFlags.Static|BindingFlags.NonPublic);

        private static string TagButton(bool showButton, string updatedText, int controlID, string keyLabel, string title, string tooltip, string tag, KeyCode keyCode, int moveCursorBy, GUIStyle style)
        {
            var buttonLabelStyle = new GUIStyle() 
			{ 
				alignment = TextAnchor.UpperRight,
				fontSize = 10,
				normal = { textColor = Color.green } 
			};

            var editor = (TextEditor)ActiveEditorField.GetValue(null);
            var eventType = Event.current.GetTypeForControl(controlID);
            bool hotkey = (eventType == EventType.KeyUp && Event.current.keyCode == keyCode && Event.current.alt);            
			bool button = false;

			if (showButton)
			{
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(style.fixedWidth));
                button = GUI.Button(rect, new GUIContent(title, tooltip), style);
                EditorGUI.LabelField(rect, keyLabel, buttonLabelStyle);
			}

            if (button || hotkey)
            {                
                var curText = editor.text;
                var newHead = curText.Substring(0, editor.cursorIndex);
                var newTail = curText.Substring(editor.cursorIndex, curText.Length - editor.cursorIndex);
                editor.text = newHead + tag + newTail;
                editor.cursorIndex += moveCursorBy;
                editor.selectIndex += moveCursorBy;

                if (hotkey)
                    Event.current.Use();

                return editor.text;
            }

            return updatedText;
        }

        public static void LocalizedStringField(SerializedProperty property, LocalizedString localizedString, Locale locale, bool showTags, params GUILayoutOption[] options)
		{
			Profiler.BeginSample("Localized String Field");
			try
			{
                var buttonStyle = new GUIStyle("Button") { fixedWidth = 30 };
                var longButtonStyle = new GUIStyle("Button") { fixedWidth = 50 };                

                if (EditorLocalizationManager.ShowTextStatus)
				{
					Profiler.BeginSample("Show Status");
					foreach (var trait in localizedString.GetLocaleTraits(locale))
					{
						GUILayout.Label(trait, GUI.skin.button, GUILayout.ExpandWidth(false));
					}
					Profiler.EndSample();
				}

				string oldText = localizedString.GetText(locale);
				
				bool needsFixUp = !localizedString.Check(property);
				if(needsFixUp)
					EditorGUI.BeginDisabledGroup(true);

				string updatedText = EditorGUILayout.TextArea(oldText, TextAreaStyle, options);
                int controlID = EditorGUIUtility.GetControlID(FocusType.Keyboard) - 1;

                if (!needsFixUp && localizedString.UpdateText(property, locale, updatedText))
				{
					AssetValidator.Revalidate();
					var name = property.serializedObject.targetObject.name + "_" + property.propertyPath;
					UndoManager.Instance.RegisterUndo(
						name + " edit",
						() =>
						{
							localizedString.UpdateText(property, locale, oldText);
							//AssetValidator.Revalidate();
						}
					);
				}

                var editor = (TextEditor)ActiveEditorField.GetValue(null);
				showTags = showTags && editor != null && editor.controlID == controlID;

				if (showTags)
					EditorGUILayout.LabelField("Tags: (shortcut Alt+Number)");
				else // to reserve vertical space
                    EditorGUILayout.LabelField("");

                using (showTags ? new EditorGUILayout.HorizontalScope() : null)
				{
					updatedText = TagButton(showTags, updatedText, controlID, "1", "mf", "Conditional part of text for male/female main character", "{mf||}", KeyCode.Alpha1, 4, buttonStyle);
					updatedText = TagButton(showTags, updatedText, controlID, "2", "name", "Main character name", "{name}", KeyCode.Alpha2, 6, longButtonStyle);
					updatedText = TagButton(showTags, updatedText, controlID, "3", "n", "Narrator's text", "{n}{/n}", KeyCode.Alpha3, 3, buttonStyle);
					updatedText = TagButton(showTags, updatedText, controlID, "4", "g", "Glossary link (term)", "{g|Encyclopedia:}{/g}", KeyCode.Alpha4, 17, buttonStyle);
					updatedText = TagButton(showTags, updatedText, controlID, "5", "d", "Glossary link (decision)", "{d|Encyclopedia}{/d}", KeyCode.Alpha5, 17, buttonStyle);
				}

                if (needsFixUp)
					EditorGUI.EndDisabledGroup();
			}
			finally
			{
				Profiler.EndSample();
			}
		}

        public static bool LocalizedStringPropertyField(Rect rect, GUIContent label, SerializedProperty property, LocalizedString localizedString)
		{
			/*
			const float spaceWidth = 10f;
			*/
			const float localeWidth = 60f;
			
			var headerRect = rect;
			headerRect.height = EditorGUIUtility.singleLineHeight;

			var foldoutRect = headerRect;
			foldoutRect.height = EditorGUIUtility.singleLineHeight;
			foldoutRect.width = EditorGUIUtility.labelWidth;
			// headerRect.xMin += EditorGUI.indentLevel * 15f;
			bool isExpanded = EditorPreferences.Instance.Scriptwriter || property.isExpanded;
			property.isExpanded = isExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, label, BoldFoldoutStyle);

			var localeRect = headerRect;
			localeRect.xMin = rect.width - localeWidth;
			localeRect.width = localeWidth;
			using (GuiScopes.FixedWidth(localeWidth, localeWidth))
			{
				var value = (Locale)EditorGUI.EnumPopup(localeRect, LocalizationManager.Instance.CurrentLocale);
				if (value != LocalizationManager.Instance.CurrentLocale)
				{
					LocalizationManager.Instance.CurrentLocale = value;
				}
			}

			bool needsFixUp = !LocalizedStringManipulation.Check(localizedString, property);
			if(needsFixUp)
				EditorGUI.BeginDisabledGroup(true);

			var locale = LocalizationManager.Instance.CurrentLocale;
			string oldText = localizedString.GetText(locale);
			string updatedText;

			if (!isExpanded)
			{
				var textRect = headerRect;
				textRect.xMin = EditorGUIUtility.labelWidth;
				textRect.width = rect.width - EditorGUIUtility.labelWidth - localeWidth;
				updatedText = GUI.TextField(textRect, oldText);
            }
			else
			{
                var textRect = rect;
				textRect.y += EditorGUIUtility.singleLineHeight;
				textRect.height -= EditorGUIUtility.singleLineHeight;                
                updatedText = EditorGUI.TextArea(textRect, oldText, TextAreaStyle);
            }

            if (needsFixUp)
			{
				EditorGUI.EndDisabledGroup();
				return isExpanded;
			}


            if (!LocalizedString.IsReferenceValue(property) &&
                localizedString.UpdateText(property, locale, updatedText))
            {
	            AssetValidator.Revalidate();
	            var name = property.serializedObject.targetObject.name + "_" + property.propertyPath;
	            UndoManager.Instance.RegisterUndo(
		            name + " edit",
		            () =>
		            {
			            localizedString.UpdateText(property, locale, oldText);
			            AssetValidator.Revalidate();
		            }
	            );
            }
            //
            // var bpComment = string.Empty;
            // var oldComment = localizedString.GetComment();
            // if (property.serializedObject.targetObject is BlueprintEditorWrapper {Blueprint: BlueprintScriptableObject so})
            // {
	           //  bpComment = so.Comment;
	           //  if (so is BlueprintAnswer or BlueprintCue &&
	           //      property.name.Equals("Description"))
		          //   return isExpanded;
            // }
            //
            // if (!LocalizedString.IsReferenceValue(property) &&
            //     !localizedString.Shared &&
            //     localizedString.UpdateComment(property, bpComment))
            // {
	           //  AssetValidator.Revalidate();
	           //  var name = property.serializedObject.targetObject.name + "_" + property.propertyPath;
	           //  UndoManager.Instance.RegisterUndo(
		          //   name + " edit",
		          //   () =>
		          //   {
			         //    localizedString.UpdateComment(property, oldComment);
			         //    AssetValidator.Revalidate();
		          //   }
	           //  );
            // }
            return isExpanded;
		}
	}
}
#endif