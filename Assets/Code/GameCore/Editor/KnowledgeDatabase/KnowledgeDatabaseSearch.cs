using System;
using System.Text.RegularExpressions;
using Code.Editor.KnowledgeDatabase.Data;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;

namespace Code.Editor.KnowledgeDatabase
{
	public static class KnowledgeDatabaseSearch
	{
		private static readonly Regex BlueprintComponentsPathRegex = new(@"Blueprint\.Components\.Array\.data\[[0-9]+\]");
		
		
		[CanBeNull]
		private static KnowledgeDatabaseType GetTypeRecord([NotNull] Type type)
		{
			if (KnowledgeDatabaseCache.TypeGuidsPair.Empty())
			{
				KnowledgeDatabaseCache.UpdateTypeGuidCache();
			}

			if (!KnowledgeDatabaseCache.TypeGuidsPair.TryGetValue(type, out string guid) || guid == null)
				return null;

			if (KnowledgeDatabase.Instance == null) 
			{
				KnowledgeDatabase.ReloadDatabase();
			}

			return KnowledgeDatabase.Instance?.Records?.Get(guid);
		}

		[CanBeNull]
		private static KnowledgeDatabaseField GetFieldRecord([NotNull] Type type, [CanBeNull] string fieldName)
		{
			var record = GetTypeRecord(type);

			if (fieldName == null || record == null || record.Fields.Empty() || !record.Fields.ContainsKey(fieldName))
			{
				return null;
			}

			return record.Fields[fieldName];
		}
		
		[CanBeNull]
		public static KnowledgeDatabaseField GetFieldRecord(KnowledgeDatabaseType type, [CanBeNull] string fieldName)
		{
			if (fieldName == null || type.Fields.Empty() || !type.Fields.ContainsKey(fieldName))
			{
				return null;
			}

			return type.Fields[fieldName];
		}

		[CanBeNull]
		public static string GetDescription([NotNull] Type type, [CanBeNull] string fieldName = null)
			=> fieldName != null ? GetFieldRecord(type, fieldName)?.Description : GetTypeRecord(type)?.Description;

		[CanBeNull]
		public static string GetCodeDescription([NotNull] Type type, [CanBeNull] string fieldName = null)
            => fieldName != null ? GetFieldRecord(type, fieldName)?.CodeDescription : GetTypeRecord(type)?.CodeDescription;

        [CanBeNull]
        public static string GetCodeDescription([NotNull] SerializedProperty property)
        {
            (var type, string fieldName) = property.GetTypeAndName();
            return GetCodeDescription(type, fieldName);
        }

        [CanBeNull]
		public static string GetDescription([NotNull] SerializedProperty property)
		{
			(var type, string fieldName) = property.GetTypeAndName();
			return GetDescription(type, fieldName);
		}

		public static void SetDescription(
			[NotNull] Type type, [CanBeNull] string fieldName, [NotNull] string description)
		{
			if (fieldName != null && GetFieldRecord(type, fieldName) is {} fieldRecord)
			{
				fieldRecord.Description = description;
			}
			else if (GetTypeRecord(type) is {} typeRecord)
			{
				typeRecord.Description = description;
			}
		}

		public static void SetDescription(
			[NotNull] KnowledgeDatabaseType type, [CanBeNull] string fieldName, [NotNull] string description)
		{
			var record = KnowledgeDatabase.Instance?.Records?.Get(type.Guid);
			if (record == null)
			{
				return;
			}

			if (fieldName != null && GetFieldRecord(type, fieldName) is {} fieldRecord)
			{
				fieldRecord.Description = description;
				return;
			}
			
			record.Description = description;
		}
		public static void SetFieldDescription(
			KnowledgeDatabaseType type, [CanBeNull] string fieldName, [NotNull] string description)
		{
			if (fieldName != null && !type.Fields.Empty() && type.Fields.ContainsKey(fieldName))
			{
				type.Fields[fieldName].Description = description;
			}
		}

		public static string GetKeywords([NotNull] KnowledgeDatabaseType type)
		{
			var record = KnowledgeDatabase.Instance?.Records?.Get(type.Guid);
			if (record == null)
			{
				return "";
			}

			return record.Keywords;
		}

		public static void AddKeywords([NotNull] KnowledgeDatabaseType type, [NotNull] string keywords)
		{
			var record = KnowledgeDatabase.Instance?.Records?.Get(type.Guid);
			if (record == null)
			{
				return;
			}
			record.Keywords = keywords;
		}

		public static (Type Type, string FieldName) GetTypeAndName([NotNull] this SerializedProperty property)
		{
			object target =
				TryGetClosestManagedReferenceValue(property) ?? property.serializedObject.targetObject;

			var objectType = target is BlueprintEditorWrapper wrapper 
				? wrapper.Blueprint.GetType() 
				: target is BlueprintComponentEditorWrapper componentWrapper 
					? componentWrapper.Component.GetType()
					: target.GetType();

			string propertyPath = GetPropertyPath(property);
			int lastDotIndex = propertyPath.LastIndexOf('.');
			string fieldName = property.name == "data" || property.name == "m_Script" ? null : propertyPath[(lastDotIndex + 1)..];

			return (objectType, string.IsNullOrEmpty(fieldName) ? null : fieldName);
		}

		private static object TryGetClosestManagedReferenceValue(SerializedProperty property)
		{
			SerializedProperty currentProperty = property;
			while (currentProperty != null)
			{
				if (currentProperty.propertyType == SerializedPropertyType.ManagedReference &&
				    currentProperty.managedReferenceValue is SimpleBlueprint or BlueprintComponent or Element)
					return currentProperty.managedReferenceValue;

				currentProperty = currentProperty.GetParent();
			}

			return null;
		}

		[NotNull]
		private static string GetPropertyPath(SerializedProperty property)
		{
			if (BlueprintComponentsPathRegex.Match(property.propertyPath).Success)
				return string.Empty;

			if (property.propertyPath == "Blueprint")
				return string.Empty;

			return property.propertyPath;
		}
	}
}