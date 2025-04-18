using System;
using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Kingmaker.Localization;
using UnityEditor;

namespace Kingmaker.Editor.Localization.FixUp
{
	public static class JsonPathIsOk
	{
#if EDITOR_FIELDS
		public static bool Check([NotNull] LocalizedString str, [NotNull] SerializedProperty prop)
			=> Check(str, prop, false);

		private static bool Check([NotNull] LocalizedString str, [NotNull] SerializedProperty prop, bool strict)
		{
			if (str.IsTrulyEmpty || str.Shared)
				return true;

			string ownerPropertyPath = LocalizedString.CalcPropertyPath(prop);
			string ownerAssetPath = LocalizedString.CalcOwnerPath(prop);
			if (ownerAssetPath == "")
				throw new InvalidOperationException("Cant calculate json path");

			string prefix = LocalizedString.ToJsonPrefix(ownerAssetPath);

			if (prop.serializedObject.targetObject is SharedStringAsset && !strict)
			{
				if (Regex.Match(str.JsonPath, $@"{Regex.Escape(prefix)}{Regex.Escape(ownerPropertyPath)}(_\d+)?\.json")
					    .Success)
				{
					string jsonPathBase = $"{prefix}{ownerPropertyPath}.json";
					if (!File.Exists(jsonPathBase))
						return false;
					return true;
				}

				if (Regex.Match(str.JsonPath, $@"{Regex.Escape(prefix)}(_\d+)?\.json").Success)
				{
					string jsonPathBase = $"{prefix}.json";
					if (!File.Exists(jsonPathBase))
						return false;
					return true;
				}

				return false;
			}

			{
				if (!Regex.Match(str.JsonPath, $@"{Regex.Escape(prefix)}{Regex.Escape(ownerPropertyPath)}(_\d+)?\.json").Success) 
					return false;
				string jsonPathBase = $"{prefix}{ownerPropertyPath}.json";
				if (!File.Exists(jsonPathBase) && strict)
					return false;

				return true;
			}
		}
		

		public static void Fix([NotNull] LocalizedString str, SerializedProperty prop)
		{
			if (Check(str, prop, true))
				return;

			string ownerPropertyPath = LocalizedString.CalcPropertyPath(prop);
			string ownerAssetPath = LocalizedString.CalcOwnerPath(prop);
			if (ownerAssetPath == "")
				throw new InvalidOperationException("Cant calculate json path");

			string prefix = LocalizedString.ToJsonPrefix(ownerAssetPath);
			if (Regex.Match(str.JsonPath, $@"{Regex.Escape(prefix)}{Regex.Escape(ownerPropertyPath)}(_\d+)?\.json").Success
			    && $"{prefix}{ownerPropertyPath}.json" is var jsonPathBase && File.Exists(jsonPathBase))
			{
				return;
			}

			var data = str.GetLoadedData() ?? str.LoadData();

			if (data == null)
				throw new InvalidOperationException("Failed to load data");

			string jsonPath = LocalizedString.GenerateJsonPath(prop);

			File.Delete(str.JsonPath);

			str.JsonPath = jsonPath;
			str.MarkDirty(prop);
			str.SaveJson(prop);
			AssetDatabase.SaveAssetIfDirty(prop.serializedObject.targetObject);
		}
#endif
	}
}