using JetBrains.Annotations;
using Kingmaker.Localization;
using UnityEditor;

namespace Kingmaker.Editor.Localization.FixUp
{
	public static class DataIsEmpty
	{
#if EDITOR_FIELDS
		public static bool Check([NotNull] LocalizedString str)
		{
			return true;
			if (str.IsTrulyEmpty || str.Shared)
				return true;
			var data = str.GetData() ?? str.LoadData();
			return data.Languages.Count > 0;
		}

		public static void Fix(LocalizedString str, SerializedProperty prop)
		{
			if (Check(str))
				return;

			return;
			str.ClearData();
			str.MarkDirty(prop);
		}
#endif
	}
}