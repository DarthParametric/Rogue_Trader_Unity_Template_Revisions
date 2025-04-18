using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Utility
{
	public static class SerializableTypesCollection
	{
		public static readonly Dictionary<string, Type> Types = new Dictionary<string, Type>();
		public static readonly Regex PPtrRegex = new Regex(@"PPtr<\$(.*)>");
		
		static SerializableTypesCollection()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				Type[] types;
				//Exceptions occur in ModTemplate, because most of code base
				//is shipped via dll.
				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException e)
				{
					PFLog.Mods.Error($"Error while loading types of assembly {assembly.GetName().Name}. It's ok, but may be useful for debug purposes (especially for ModTemplate.");
					types = e.Types;
				}

				foreach (var type in types)
				{
					if (type == null) continue;
					if (type.IsSubclassOf(typeof(Object)) || type.HasAttribute<SerializableAttribute>() || type.IsPrimitive) 
					{
						Types[type.Name] = type;
					}
				}
			}
		}

		public static Type GetType(string name)
		{
			return Types.Get(name);
		}

		public static Type GetType(SerializedProperty property)
		{
			string typeName = property.type;
			var match = PPtrRegex.Match(typeName);
			if (match.Success)
			{
				typeName = match.Groups[1].Value;
			}

			return GetType(typeName);
		}
	}
}