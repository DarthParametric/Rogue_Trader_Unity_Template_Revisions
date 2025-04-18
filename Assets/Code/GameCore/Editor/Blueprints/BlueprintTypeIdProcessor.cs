using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Code.Editor.KnowledgeDatabase;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.ElementsSystem;
using Kingmaker.View;
using Kingmaker.View.MapObjects;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	class BlueprintTypeIdProcessor : AssetPostprocessor
	{
		static void OnPostprocessAllAssets(
			string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			foreach (string str in importedAssets)
			{
				if (str.EndsWith(".cs"))
				{
					var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(str);
					if (!monoScript)
					{
						Debug.LogWarning("Failed to import script from " + str);
						continue;
					}

					var guid = AssetDatabase.AssetPathToGUID(str);
					//Debug.Log(monoScript?.name + " imported, guid = " + guid);
					var type = monoScript.GetClass();
					if (type == null)
						continue; // not yet compiled maybe?

					if (type.IsSubclassOf(typeof(SimpleBlueprint))
					    || type.IsSubclassOf(typeof(BlueprintComponent))
					    || type.IsSubclassOf(typeof(Element)))
					{
						if (TryCorrectId(type, guid, str, "TypeId"))
						{
							continue;
						}

						TryCreateId(type, str, guid, "TypeId");
						continue;
					}

					if (type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject)))
					{
						if (TryCorrectId(type, guid, str, "KnowledgeDatabaseID"))
						{
							continue;
						}

						if (type.IsSubclassOf(typeof(EntityViewBase)) 
						    || type.IsSubclassOf(typeof(AbstractEntityPartComponent)))
						{
							TryCreateId(type, str, guid, "KnowledgeDatabaseID");
						}
					}
				}
			}
		}

		private static void TryCreateId(Type type, string str, string guid, string attributeName)
		{
			Debug.LogError($"{type.Name} has no {attributeName} attribute. Patching.");
			var sourceLines = File.ReadAllLines(str);
			using (var sw = new StreamWriter(str))
			{
				foreach (string sourceLine in sourceLines)
				{
					// the class has an Id, but it does not match its actual guid
					if (sourceLine.Contains($"{attributeName}(\""))
						continue;

					if (Regex.IsMatch(sourceLine, $"class {type.Name}\\b"))
					{
						sw.WriteLine($"\t[Kingmaker.Blueprints.JsonSystem.Helpers.{attributeName}(\"{guid}\")]");
					}

					sw.WriteLine(sourceLine);
				}
			}
		}

		private static bool TryCorrectId(Type type, string guid, string str, string attributeName)
		{
			var typeId = type.GetAttribute<TypeIdAttribute>();
			var kdbId = type.GetAttribute<KnowledgeDatabaseIDAttribute>();
			IIdAttribute idAttribute = typeId != null ? typeId : kdbId;
			if (idAttribute != null)
			{
				if (idAttribute.GuidString != guid)
				{
					Debug.LogError(
						$"In {type.Name} {attributeName} is {idAttribute.GuidString}, does not match guid. Patching.");
					var file = File.ReadAllText(str);
					file = file.Replace($"{attributeName}(\"{idAttribute.GuidString}\"",
						$"{attributeName}(\"{guid}\"");
					File.WriteAllText(str, file);
				}

				return true;
			}

			return false;
		}
	}
}
