using JetBrains.Annotations;
using Kingmaker.Localization;
using UnityEditor;

namespace Kingmaker.Editor.Localization.FixUp
{
#if EDITOR_FIELDS
	public static class SharedIsEmpty
	{
		public static bool Check([NotNull] LocalizedString str)
			=> !str.Shared || str.IsTrulyEmpty;

		public static void Fix(LocalizedString str, SerializedProperty prop)
		{
			if (Check(str))
				return;

			str.ClearData();
			str.MarkDirty(prop);
		}
		
	}
#endif
}