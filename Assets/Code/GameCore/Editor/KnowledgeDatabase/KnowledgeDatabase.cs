using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Code.Editor.KnowledgeDatabase.Data;
using Code.Editor.KnowledgeDatabase.Serialization;
using JetBrains.Annotations;
using Kingmaker;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Properties.BaseGetter;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.Utility.DotNetExtensions;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace Code.Editor.KnowledgeDatabase
{
	public class KnowledgeDatabase
	{
		public static KnowledgeDatabase Instance; //internal

		private readonly Dictionary<string, KnowledgeDatabaseType> m_Records;
		private readonly KnowledgeDatabaseColumns.Column[] m_Columns;

		public Dictionary<string, KnowledgeDatabaseType> Records => m_Records;
		
		[NotNull]
		public IEnumerable<KnowledgeDatabaseType> RecordsValues
			=> m_Records?.Values ?? Enumerable.Empty<KnowledgeDatabaseType>();

		[NotNull]
		public IEnumerable<KnowledgeDatabaseColumns.Column> Columns
			=> m_Columns ?? KnowledgeDatabaseColumns.DefaultColumns.ToArray();

		public KnowledgeDatabase(
			Dictionary<string, KnowledgeDatabaseType> records,
			KnowledgeDatabaseColumns.Column[] columns)
		{
			m_Records = records;
			m_Columns = columns;
		}

		[InitializeOnLoadMethod]
		private static async void InitialLoad()
		{
			Instance = await Load();
			if(Instance == null)
				return;
			KnowledgeDatabaseCache.UpdateTypeGuidCache();
		}
		
		[MenuItem("Design/Knowledge Database/Reload")]
		public static void ReloadDatabase()
		{
			Instance = Load().Result;
		}
		
		private static Task<KnowledgeDatabase> Load()
			=> Task.Run(() =>
			{
				var databasePath = GetDatabaseFilePath();
				if (!File.Exists(databasePath))
					return null;
				using var reader = new StreamReader(GetDatabaseFilePath(), true);
				return KnowledgeDatabaseSerializer.Deserialize(reader);
			});


		[MenuItem("Design/Knowledge Database/Update")]
		public static void UpdateDatabase()
		{
			Update();
		}

		public static Task Save()
			=> Save(Instance);
		
		private static Task Save(KnowledgeDatabase database)
			=> Task.Run(() =>
			{
				if (database == null || !File.Exists(GetDatabaseFilePath()))
				{
					PFLog.System.Error($"Failed to save KnowledgeDatabase. It's null or .csv doesn't exits.");
					return;
				}

				using var writer = new StreamWriter(GetDatabaseFilePath(), false, Encoding.UTF8);
				KnowledgeDatabaseSerializer.Serialize(writer, database);
			});

		public static string GetDatabaseFilePath()
			=> Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "KnowledgeDatabase.csv"));
		
		private static async void Update()
		{
			var loadTask = Load();
			var collectDataTask = KnowledgeDatabaseDataCollector.CollectData();

			await Task.WhenAll(loadTask, collectDataTask);

			var database = loadTask.Result;
			if(database == null)
				return;
			
			var actualData = collectDataTask.Result;

			database.Merge(actualData);
			Instance = database;
			
			await Save(database);
		}

		private void Merge(IEnumerable<KnowledgeDatabaseType> actual)
		{
			if (actual.Empty())
			{
				return;
			}
			
			if (m_Records.Empty())
			{
				actual.ForEach(i => m_Records.Add(i.Guid, i));
				return;
			}

			var newRecords = new Dictionary<string, KnowledgeDatabaseType>();
			foreach ((string guid, var recordType) in m_Records)
			{
				var actualType = actual.FirstOrDefault(i => i.Guid == guid);
				if (actualType == null)
				{
					actualType = recordType;
					
					actualType.IsRemoved = true;
				}
				else
				{
					actualType.Description = recordType.Description;
					actualType.Keywords = recordType.Keywords;

					actualType.Meta.Clear();
					recordType.Meta.ForEach(actualType.Meta.Add);

					actualType.Fields = MergeFields(recordType.Fields, actualType.Fields);
				}

				newRecords.Add(guid, actualType);
			}

			foreach (var actualType in actual)
			{
				if (newRecords.ContainsKey(actualType.Guid))
					continue;
				
				newRecords.Add(actualType.Guid, actualType);
			}

			m_Records.Clear();
			newRecords.ForEach(m_Records.Add);
		}

		private static Dictionary<string, KnowledgeDatabaseField> MergeFields(
			Dictionary<string, KnowledgeDatabaseField> recordFields,
			Dictionary<string, KnowledgeDatabaseField> actualFields)
		{
			var result = new Dictionary<string, KnowledgeDatabaseField>();
			foreach (var recordField in recordFields)
			{
				string fieldName = recordField.Key.Split(".").Last();
				actualFields.TryGetValue(fieldName, out KnowledgeDatabaseField actualField);

				if (actualField == null)
				{
					actualField = recordField.Value;
					actualField.IsRemoved = true;
				}
				else
				{
					actualField.Description = recordField.Value.Description;
					actualField.Keywords = recordField.Value.Keywords;

					actualField.Meta.Clear();
					recordField.Value.Meta.ForEach(actualField.Meta.Add);
				}
				
				result[actualField.FieldName] = actualField;
			}

			foreach (var actualField in actualFields)
			{
				if (result.ContainsKey(actualField.Value.FieldName))
					continue;
				
				result[actualField.Key] = actualField.Value;
			}

			return result;
		}
	}
}