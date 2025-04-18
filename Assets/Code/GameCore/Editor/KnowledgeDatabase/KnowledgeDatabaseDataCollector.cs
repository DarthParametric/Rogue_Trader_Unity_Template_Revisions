using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Code.Editor.KnowledgeDatabase.Data;
using JetBrains.Annotations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Interfaces;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.EntitySystem.Stats.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Kingmaker.View.Spawners;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.KnowledgeDatabase
{
	public static class KnowledgeDatabaseDataCollector
	{
		public static Task<KnowledgeDatabaseType[]> CollectData()
			=> Task.Run(() =>
				CollectInheritedTypesInfo<Element>()
					.Concat(CollectInheritedTypesInfo<BlueprintComponent>())
					.Concat(CollectInheritedTypesInfo<SimpleBlueprint>())
					.Concat(CollectMonoBehaviorsInheritedTypesInfo())
					.ToArray());

		private static KnowledgeDatabaseType[] CollectInheritedTypesInfo<TBase>()
		{
			return
				TypeCache.GetTypesDerivedFrom(typeof(TBase)).Where(x => !x.IsAbstract)
				.Select(i => (Type: i, Guid: i.GetAttribute<TypeIdAttribute>()?.GuidString))
				.Where(i => i.Guid != null)
				.Select(i =>
					new KnowledgeDatabaseType
					{
						Name = i.Type.Name,
						Guid = i.Guid,
						Type = GetTypeName(i.Type),
						AllowedOn = GetAllowedOn(i.Type),
						Fields = GetAllSerializableFields(i.Type),
						HasRuntime = typeof(IRuntimeEntityFactComponentProvider).IsAssignableFrom(i.Type),
						IsObsolete = i.Type.HasAttribute<ObsoleteAttribute>(),
						CodeDescription = i.Type.GetCustomAttribute<KDBAttribute>()?.Text
					})
				.ToArray();
		}

		public static KnowledgeDatabaseType[] CollectMonoBehaviorsInheritedTypesInfo()
		{
			var result = new List<KnowledgeDatabaseType>();
			var monoBehaviours = TypeCache.GetTypesDerivedFrom(typeof(MonoBehaviour))
				.Concat(TypeCache.GetTypesDerivedFrom(typeof(ScriptableObject)));
			foreach (var monoBehaviour in monoBehaviours)
			{
				if (monoBehaviour.IsAbstract)
				{
					continue;
				}

				string guid = monoBehaviour.GetCustomAttribute<KnowledgeDatabaseIDAttribute>()?.GuidString;
				if (guid == null)
				{
					continue;
				}
				
				var kdbType = new KnowledgeDatabaseType
				{
					Name = monoBehaviour.Name,
					Guid = guid,
					Type = GetTypeName(monoBehaviour),
					AllowedOn = GetRequiredComponents(monoBehaviour),
					Fields = GetAllSerializableFields(monoBehaviour, true),
					HasRuntime = false,
					IsObsolete = false,
				};
				result.Add(kdbType);
			}

			return result.ToArray();
		}

		private static string GetAllowedOn(Type type)
		{
			var attributes = type.GetAttributes<AllowedOnAttribute>();
			return string.Join(", ", attributes.Select(i => i.Type.Name));
		}
		
		private static string GetRequiredComponents(Type type)
		{
			var attributes = type.GetAttributes<RequireComponent>();
			return string.Join(", ", attributes.Select(i => i.m_Type0.Name));
		}

		private static Dictionary<string,KnowledgeDatabaseField> GetAllSerializableFields(Type type, bool isMonoBehaviour = false)
		{
			var validFields = new Dictionary<string,KnowledgeDatabaseField>();
			GetAllSerializableFieldsInternal(type, validFields, null, isMonoBehaviour);
			return validFields;
		}

		private static void GetAllSerializableFieldsInternal(
			Type type, Dictionary<string,KnowledgeDatabaseField> validFields, [CanBeNull] Type prevType, 
			bool isMonoBehaviour)
		{
			var fields = (isMonoBehaviour ? type.GetUnitySerializedFields() : type.GetRuntimeFields()).Where(IsFieldValid).ToArray();
			if (fields.Empty())
			{
				return;
			}

			foreach (var field in fields)
			{
				string fieldPath = (prevType == null ? type.FullName : prevType.FullName + "." + type.Name) + "." +
				                   field.Name;
				if (validFields.ContainsKey(field.Name))
				{
					continue;
				}

				validFields[field.Name] = new KnowledgeDatabaseField
				{
					FieldPath = fieldPath,
					FieldName = field.Name,
					CodeDescription = field.GetCustomAttribute<KDBAttribute>()?.Text,
					Type = field.FieldType.Name,
					AllowedEntity = field.GetCustomAttributes(true).OfType<AllowedEntityTypeAttribute>()
						.FirstOrDefault()?.Type.Name
				};

				if (ShouldNotGoDeeper(type, prevType, field))
				{
					continue;
				}

				GetAllSerializableFieldsInternal(field.FieldType, validFields, type, isMonoBehaviour);
			}
		}
		
		private static bool IsFieldValid(FieldInfo field)
		{
			object[] attributes = field.GetCustomAttributes(true);
			bool isSerializable =
				(field.IsPrivate || field.IsFamily) && attributes.HasItem(i => i is SerializeField or SerializeReference) || field.IsPublic;
			bool isIgnored = attributes.HasItem(i => i is HideInInspector or JsonIgnoreAttribute);
			if (!isSerializable || isIgnored)
				return false;
			return true;
		}
		
		private static bool ShouldNotGoDeeper(Type type, Type prevType, FieldInfo field)
		{
			return field.FieldType.GetAttribute<TypeIdAttribute>() != null 
			       || field.FieldType.IsEnum
			       || field.FieldType == type
			       || prevType != null && field.FieldType == prevType
			       || field.FieldType.Namespace != null 
			       && (field.FieldType.Namespace.StartsWith("Kingmaker.Localization") 
			           || !field.FieldType.Namespace.StartsWith("Kingmaker"));
		}
		
		private static string GetTypeName(Type t)
		{
			if (typeof(Element).IsAssignableFrom(t))
			{
				if (typeof(Evaluator).IsAssignableFrom(t))
					return "Evaluator";
				if (typeof(ContextAction).IsAssignableFrom(t))
					return "ContextAction";
				if (typeof(ContextCondition).IsAssignableFrom(t))
					return "ContextCondition";
				if (typeof(GameAction).IsAssignableFrom(t))
					return "Action";
				if (typeof(Condition).IsAssignableFrom(t))
					return "Condition";
				if (typeof(PropertyGetter).IsAssignableFrom(t))
					return "Getter";
				return "Element";
			}

			if (typeof(BlueprintComponent).IsAssignableFrom(t))
			{
				if (typeof(IRuntimeEntityFactComponentProvider).IsAssignableFrom(t))
					return "FactLogic";
				return "Component";
			}

			if (typeof(SimpleBlueprint).IsAssignableFrom(t))
			{
				if (typeof(CommandBase).IsAssignableFrom(t))
					return "CutsceneCommand";
				return "Blueprint";
			}
			
			if(t.IsSubclassOf(typeof(EntityViewBase)))
				return "EntityView";

			if(t.IsSubclassOf(typeof(AbstractEntityPartComponent)))
				return "EntityPartComponent";

			return t.Name;
		}
	}
}