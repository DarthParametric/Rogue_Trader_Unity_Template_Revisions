using System.IO;
using JetBrains.Annotations;
using Kingmaker.Localization;
using UnityEditor;

namespace Kingmaker.Editor.Localization.FixUp
{
	public static class JsonFileExists
	{
#if EDITOR_FIELDS
		public static bool Check([NotNull] LocalizedString str)
			=> str.IsTrulyEmpty || File.Exists(str.JsonPath);

		public static void Fix(LocalizedString str, SerializedProperty prop)
		{
			if (Check(str)) 
				return;

			str.ClearData(false);
			str.MarkDirty(prop);
		}
#endif
	}
}