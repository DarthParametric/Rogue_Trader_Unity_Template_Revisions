using Kingmaker.Utility.EditorPreferences;
using Owlcat.Editor.Core.Utility;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Code.GameCore.Editor
{
	// #if OWLCAT_MODS
	public class ModsPreferencesGUI
	{
		internal static class Preferences
		{
			private static EditorPreferences Prefs
				=> EditorPreferences.Instance;

			private static EditorPreferencesConfig Config
				=> EditorPreferencesConfigProvider.Config; 

			[SettingsProvider]
			public static UnityEditor.SettingsProvider CreateMyCustomSettingsProvider()
			{
				if (Config == null || !Config.ProjectIsModTemplate)
					return null;
				
				var initialized = EditorPrefs.GetBool($"{Config.ProjectName}/Initialized");
				if (!initialized)
					SetDefaults();

				var provider = new UnityEditor.SettingsProvider("Preferences/Rogue Trader Mods", SettingsScope.User)
				{
					guiHandler = searchContext => { PreferencesGUI(); },

					// Populate the search keywords to enable smart search filtering and label highlighting:
					keywords = new []{"Use New Editor"}
				};

				return provider;
			}

			private static void PreferencesGUI()
			{
				using (GuiScopes.LabelWidth(300))
				{
					Prefs.UseNewEditor =
						EditorGUILayout.Toggle("Use new UIElements editor (Experimental)", Prefs.UseNewEditor);
					Prefs.BigCheckbox = EditorGUILayout.Toggle("Show big checkbox", Prefs.BigCheckbox);
					Prefs.ValidateTexts = EditorGUILayout.Toggle("Validate texts", Prefs.ValidateTexts);
					Prefs.BlueprintIndexingServerProcessWindowStyle = (ProcessWindowStyle)EditorGUILayout.EnumPopup("Blueprint Indexing Server Window Style", Prefs.BlueprintIndexingServerProcessWindowStyle);
					Prefs.CloseBlueprintsServerOnExit = EditorGUILayout.Toggle("Close blueprints server on exit", Prefs.CloseBlueprintsServerOnExit);
					Prefs.SaveBlueprintsWithAssets = EditorGUILayout.Toggle("Save blueprints when Unity saves assets", Prefs.SaveBlueprintsWithAssets);
					if (GUILayout.Button("Reset To Defaults"))
					{
						SetDefaults();
					}
				}
				// Save the preferences
				if (GUI.changed)
				{
					Prefs.Save();
				}
			}

			private static void SetDefaults()
			{
				EditorPrefs.SetBool($"{Config.ProjectName}/Initialized", true);
				Prefs.UseNewEditor = Config.UseNewEditorDefault;
				Prefs.BigCheckbox = Config.BigCheckboxDefault;
				Prefs.ValidateTexts = Config.ValidateTextsDefault;
				Prefs.BlueprintIndexingServerProcessWindowStyle =
					Config.BlueprintIndexingServerProcessWindowStyleDefault;
				Prefs.CloseBlueprintsServerOnExit = Config.CloseBlueprintsServerOnExitDefault;
				Prefs.SaveBlueprintsWithAssets = Config.SaveBlueprintsWithAssetsDefault;
				Prefs.Save();
			}
		}
	}
	// #endif
}