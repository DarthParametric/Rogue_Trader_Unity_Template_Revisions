using System;
using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;

namespace Kingmaker.Editor.Localization
{
	public class EditorPreferencesLocalizationStorageProvider : ILocaleStorageProvider
	{
		private static readonly EditorPreferencesLocalizationStorageProvider Instance = new();
		
		public Locale Locale
		{
			get => EditorPreferences.Instance.Locale;
			set 
			{ 
				EditorPreferences.Instance.Locale = value; 
				EditorPreferences.Instance.Save();
				Changed?.Invoke(value);
			}
		}

		public event Action<Locale> Changed;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			LocalizationManager.Instance.RegisterLocaleStorageProvider(Instance);
		}
	}
}