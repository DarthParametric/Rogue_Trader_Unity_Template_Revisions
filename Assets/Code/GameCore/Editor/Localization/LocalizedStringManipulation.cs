using JetBrains.Annotations;
using Kingmaker.Blueprints.Base;
using Kingmaker.Editor.Localization.FixUp;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using UnityEditor;

namespace Kingmaker.Editor.Localization
{
	public static class LocalizedStringManipulation
	{
#if EDITOR_FIELDS

		public static void MakeNewShared(this LocalizedString locStr, SerializedProperty property, SharedStringAsset shared, bool deleteOldString = true)
		{
			var data = locStr.GetData();

			var sharedProperty = new SerializedObject(shared).FindProperty("String");
			if (data != null) 
				shared.String.Initialize(sharedProperty, data);

			locStr.SetShared(property, shared, null, deleteOldString);
		}

		public static void SetShared(this LocalizedString locStr, SerializedProperty property, [CanBeNull] SharedStringAsset shared, string deletionTrait = null, bool deleteOldString = true)
		{
			bool deleteJson = deletionTrait == null;
			if (!deleteJson && !locStr.Shared)
			{
				var data = locStr.GetData();
				if (data != null)
				{
					data.AddStringTrait(StringTrait.Invalid.ToString());
					data.AddStringTrait(deletionTrait);
					locStr.SaveJson(property);
				}
			}
			locStr.ClearData(deleteJson && deleteOldString);

			locStr.Shared = shared;
			locStr.MarkDirty(property);
			AssetDatabase.SaveAssetIfDirty(property.serializedObject.targetObject);
		}

		public static bool Check(this LocalizedString str, SerializedProperty prop)
		{
			if (LocalizedString.IsReferenceValue(prop))
			{
				str.Validation = null;				
				return true;
			}

			if (str.Validation is {} validation)
				return validation.IsOk;
			
			bool isOk = CheckImpl(str, prop);
			str.Validation = new LocalizedString.ValidationInfo(isOk);
			return isOk;
		}

		private static bool CheckImpl(LocalizedString str, SerializedProperty prop)
		{
	        if (!SharedIsEmpty.Check(str))
		        return false;

	        if (!JsonFileExists.Check(str))
		        return false;

	        if (!OldTryFixDuplicated.Check(str, prop))
		        return false;

	        if (!JsonPathIsOk.Check(str, prop))
		        return false;

	        if (!DataIsEmpty.Check(str))
		        return false;

	        if (str.Shared)
	        {
		        var so = new SerializedObject(str.Shared);
		        var p = so.FindProperty("String");
		        return Check(str.Shared.String, p);
	        }

	        return true;
        }


        public static void Fix(this LocalizedString str, SerializedProperty prop)
        {
            if (LocalizedString.IsReferenceValue(prop))
            {
                SyncWithProto(prop);
                return;
            }

            SharedIsEmpty.Fix(str, prop);

            JsonFileExists.Fix(str, prop);
            
            OldTryFixDuplicated.Fix(str, prop);

            JsonPathIsOk.Fix(str, prop);

            DataIsEmpty.Fix(str, prop);

            AssetDatabase.SaveAssetIfDirty(prop.serializedObject.targetObject);
            
            if (str.Shared)
            {
	            var so = new SerializedObject(str.Shared);
	            var p = so.FindProperty("String");
	            Fix(str.Shared.String, p);
	            str.Validation = null;
            }
        }
        
        private static void SyncWithProto([NotNull] SerializedProperty property)
        {
	        if (property.serializedObject.targetObject is ScriptableWrapperBase sw && sw)
		        sw.SyncPropertiesWithProto();
        }
#endif
	}
}
