using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Blueprints
{
	public class ClassNames
	{
		private static readonly Dictionary<Type, NamesData> s_Names = new Dictionary<Type, NamesData>();

		static ClassNames()
		{
			var types = 
					TypeCache.GetTypesDerivedFrom(typeof(BlueprintScriptableObject)).Where(t => !t.IsAbstract)
					.Concat(TypeCache.GetTypesDerivedFrom(typeof(BlueprintComponent)).Where( t=> !t.IsAbstract))
					.Where(t => t.HasAttribute<ComponentNameAttribute>());

			foreach (var type in types)
			{
				var attr = type.GetCustomAttributes(typeof(ComponentNameAttribute), false).FirstOrDefault() as ComponentNameAttribute;
				if (attr != null)
				{
					var slash = attr.Name.LastIndexOf("/", StringComparison.Ordinal);
					s_Names[type] = new NamesData
					{
						NameWithPrefix = attr.Name,
						NameWithoutPrefix = slash < 0 ? attr.Name : attr.Name.Substring(slash + 1)
					};
				}
			}
		}

		public static string GetClassName(Type type)
		{
			NamesData data;
			if (s_Names.TryGetValue(type, out data))
				return data.NameWithPrefix;
			return type.Name;
		}

		public static string GetClassNameNoPrefix(Type type)
		{
			NamesData data;
			if (s_Names.TryGetValue(type, out data))
				return data.NameWithoutPrefix;
			return type.Name;
		}

		public static string GetObjectNameNoPrefix(object obj)
		{
			NamesData data;
			if (s_Names.TryGetValue(obj.GetType(), out data))
				return data.NameWithoutPrefix;
            if(obj is Object unityObject)
			    return ObjectNames.GetClassName(unityObject);
            return obj.GetType().Name;
        }

		private class NamesData
		{
			public string NameWithoutPrefix;
			public string NameWithPrefix;
		}
	}
}