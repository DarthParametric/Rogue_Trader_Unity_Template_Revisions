using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.ElementsSystem;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.KnowledgeDatabase
{
	public static class KnowledgeDatabaseCache
	{
		private static CancellationTokenSource s_UpdateTypeGuidCacheCt = new();
		
		public static readonly ConcurrentDictionary<Type, string> TypeGuidsPair = new();

		public static async void UpdateTypeGuidCache()
		{
			s_UpdateTypeGuidCacheCt?.Cancel();
			s_UpdateTypeGuidCacheCt = new CancellationTokenSource();
			await CollectTypeGuidCache(s_UpdateTypeGuidCacheCt.Token);
		}
		private static Task<int> CollectTypeGuidCache(CancellationToken cancellationToken)
			=> Task.Run(CollectTypeGuidCache, cancellationToken);
		private static int CollectTypeGuidCache()
		{
			int recordedTypes = 0;
			var types = TypeCache.GetTypesDerivedFrom(typeof(Element))
				.Concat(TypeCache.GetTypesDerivedFrom(typeof(BlueprintComponent)))
				.Concat(TypeCache.GetTypesDerivedFrom(typeof(SimpleBlueprint)))
				.Concat(TypeCache.GetTypesDerivedFrom(typeof(MonoBehaviour)));
			
			foreach (var type in types)
			{
				if (type.IsAbstract)
				{
					continue;
				}

				string guid = type.GetAttribute<TypeIdAttribute>()?.GuidString 
				              ?? type.GetCustomAttribute<KnowledgeDatabaseIDAttribute>()?.GuidString;
				if (guid == null)
				{
					continue;
				}

				TryRecordTypeGuidCache(type, guid);
				recordedTypes++;
			}

			return recordedTypes;
		}

		private static void TryRecordTypeGuidCache(Type type, string guid)
		{
			TypeGuidsPair.TryAdd(type, guid);
		}
	}
}