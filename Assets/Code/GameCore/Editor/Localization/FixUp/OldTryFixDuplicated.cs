using System;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Localization;
using Owlcat.Runtime.Core.Logging;
using UnityEditor;

namespace Kingmaker.Editor.Localization.FixUp
{
	public static class OldTryFixDuplicated
	{
#if EDITOR_FIELDS
        private static readonly LogChannel Logger = LogChannelFactory.GetOrCreate("Localization");

        public static bool Check(LocalizedString str, SerializedProperty prop)
        {
            if (str.IsTrulyEmpty)
                return true;

            if (!str.KeyIsOkay)
                return false;
            
            if (str.Shared)
                return true;

            if (!str.IsPropertyPathOk(prop))
                return false;

            if (!str.IsOwnerStringOk(prop))
                return false;

            var oldData = str.GetLoadedData() ?? str.LoadData();

            if (!string.Equals(str.OwnerString, oldData.OwnerGuid, StringComparison.Ordinal))
                return false;

            string ownerString = LocalizedString.CalcOwnerString(prop);

            // this is a string from different object entirely, clear it
            if (!string.Equals(ownerString, str.OwnerString, StringComparison.Ordinal) ||
                oldData.OwnerGuid != "" && !string.Equals(ownerString, oldData.OwnerGuid, StringComparison.Ordinal))
            {
                if (!string.Equals(str.OwnerString, oldData.OwnerGuid, StringComparison.Ordinal) ||
                    (!string.IsNullOrWhiteSpace(AssetDatabase.GUIDToAssetPath(str.OwnerString)) &&
                     !string.Equals(AssetDatabase.GUIDToAssetPath(str.OwnerString), LocalizedString.CalcOwnerPath(prop),
                         StringComparison.Ordinal)))
                {
                    return false;
                }
            }

            string actualPath = LocalizedString.CalcPropertyPath(prop);
            return string.Equals(actualPath, str.OwnerPropertyPath, StringComparison.Ordinal);
        }

        public static void Fix(LocalizedString str, SerializedProperty prop)
        {
            if (Check(str, prop))
                return;

            if (TryFixDuplicated(str, prop) && !str.IsTrulyEmpty) 
                str.SaveJson(prop);
        }

        private static bool TryFixDuplicated(LocalizedString str, SerializedProperty prop)
        {
            if (str.Shared || str.IsTrulyEmpty)
                return false;

            var oldData = str.GetLoadedData() ?? str.LoadData();
            var owner = prop.serializedObject.targetObject;

            string ownerString = LocalizedString.CalcOwnerString(prop);
            // hack: strings in converted elements would have #name appended, but they do not need to anymore
            // do not consider this a duplicate, fix old owner string
            if (owner is BlueprintEditorWrapper && str.OwnerString.Contains("#"))
            {
                str.OwnerString = str.OwnerString[..str.OwnerString.IndexOf('#')];
            }

            // this is a string from different object entirely, clear it
            if (!string.Equals(ownerString, str.OwnerString, StringComparison.Ordinal) ||
                oldData.OwnerGuid != "" && !string.Equals(ownerString, oldData.OwnerGuid, StringComparison.Ordinal))
            {
                Logger.Warning(
                    "Detected localized string duplication - clearing text. {0}[{1}], was {2}[{3}]",
                    ownerString,
                    LocalizedString.CalcPropertyPath(prop),
                    str.OwnerString,
                    str.OwnerPropertyPath);

                str.ClearData(false);
                str.MarkDirty(prop);
                return true;
            }

            string actualPath = LocalizedString.CalcPropertyPath(prop);
            if (!string.Equals(actualPath, str.OwnerPropertyPath, StringComparison.Ordinal))
            {
                // this is a string from a different property on a same object. Do not clear it, maybe create a copy instead

                Logger.Log(CheckPathForSameArray(str, actualPath, out int indexActual, out int indexCurrent)
                    ? $"String moved in array {actualPath}: was at {indexCurrent}, now at {indexActual}"
                    : $"String moved from {str.OwnerPropertyPath} to {actualPath}");

                return TryFixPropertyPathChange(str, prop, actualPath, indexActual, indexCurrent);
            }

            if (!string.Equals(str.Key, str.GetLoadedData()!.Key))
            {
                str.Key = str.GetLoadedData()!.Key = Guid.NewGuid().ToString();
                str.MarkDirty(prop);
                return true;
            }
            return false;
        }

        private static bool TryFixPropertyPathChange(LocalizedString str, SerializedProperty property, string actualPath, int actualIndex, int currentIndex)
        {
            // the string used to be at path m_OwnerPropertyPath, but is now at actualPath
            var oldProp = property.serializedObject.FindProperty(str.OwnerPropertyPath);
            var replacingString = oldProp == null ? null : FieldFromProperty.GetFieldValue(oldProp) as LocalizedString;
            if (replacingString == null)
            {
                // we were not replaced by any string, just fix the json to match new property and we're good to go
                str.OwnerPropertyPath = actualPath;
                JsonPathIsOk.Fix(str, property);
                return true;
            }
            
            // if we've moved UP in the array, fix the other property first (it's probably moved up too)
            if (currentIndex>=0 && replacingString.OwnerPropertyPath != str.OwnerPropertyPath && currentIndex > actualIndex)
            {
                TryFixDuplicated(replacingString, oldProp);
            }
            
            // either the replacement property is already correct, or we've moved down.
            // in any case, make this string a copy so that we do not lose the data
            str.OwnerPropertyPath = actualPath;
            if (replacingString.JsonPath == str.JsonPath)
            {
                // if the replacing property is the same as this one, create a new string with the same data
                str.JsonPath = "";
                str.Key = str.GetLoadedData()!.Key = Guid.NewGuid().ToString();
            }

            JsonPathIsOk.Fix(str, property);
            return true;
        }

        private static bool CheckPathForSameArray(LocalizedString str, string actualPath, out int indexActual, out int indexCurrent)
        {
            indexActual = indexCurrent = -1;

            // check if original path is the same array but in a different place
            int idx = 0;
            // find first divergence
            while (idx < actualPath.Length && idx < str.OwnerPropertyPath.Length && actualPath[idx] == str.OwnerPropertyPath[idx])
            {
                idx++;
            }

            if (idx == actualPath.Length || idx == str.OwnerPropertyPath.Length || idx == 0)
                return false;

            // count back to first non-digit
            while (char.IsDigit(actualPath[idx]))
                idx--;
            
            // check it's [
            if (actualPath[idx] != '[' || str.OwnerPropertyPath[idx] != '[')
                return false;

            // skip until ]
            int ida = idx + 1;
            int idb = idx + 1;
            while (actualPath[ida] != ']')
            {
                ida++;
            }
            while (str.OwnerPropertyPath[idb] != ']')
            {
                idb++;
            }

            // check that everything after the index is also the same
            if (!actualPath.Substring(ida).Equals(str.OwnerPropertyPath.Substring(idb), StringComparison.Ordinal))
                return false;

            indexActual = int.Parse(actualPath.Substring(idx + 1, ida - idx - 1));
            indexCurrent = int.Parse(str.OwnerPropertyPath.Substring(idx + 1, idb - idx - 1));
            return true;
        }
#endif
	}
}