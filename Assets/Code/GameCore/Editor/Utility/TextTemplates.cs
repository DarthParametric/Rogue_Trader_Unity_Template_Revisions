using System;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

namespace Kingmaker.Editor.Utility
{
	public class TextTemplates
	{
		/// <summary>
		/// Replaces templates in string formatted like "this is a string with {value}s"
		/// </summary>
		/// <param name="s"></param>
		/// <param name="replacer"></param>
		/// <returns></returns>
		[NotNull]
		public static string ReplaceTemplates([NotNull]string s, [NotNull]Func<string, string> replacer)
		{
			var i = 0;
			while (i < s.Length)
			{
				i = s.IndexOf("{", i, StringComparison.InvariantCulture);
				if (i < 0)
				{
					break;
				}
				var j = s.IndexOf("}", i, StringComparison.InvariantCulture);
				if (j < 0)
				{
					// wtf?
					break;
				}
				var template = s.Substring(i + 1, j - i - 1);
				string replacement = replacer(template);
				if (replacement != null)
				{
					s = s.Substring(0, i) + replacement + s.Substring(j + 1);
				}
				else
				{
					i = j;
				}
			}
			return s;
		}

		[NotNull]
		public static string ReplacePropertyNames([NotNull]string s, [NotNull]SerializedObject so)
		{
			return ReplaceTemplates(s, PropertyNameReplacer(so));
		}


		public static Func<string, string> PropertyNameReplacer(SerializedObject so)
		{
			return template =>
			{
				var nullable = template.EndsWith("?");
				var propName = nullable ? template.Substring(0, template.Length - 1) : template;
				var prop = so.FindProperty(propName);
				if (prop != null)
				{
                    if (prop.propertyType == SerializedPropertyType.String)
                        return prop.stringValue;
                    if (prop.propertyType == SerializedPropertyType.Generic)
                    {
                        var guid = prop.FindPropertyRelative("guid");
                        if (guid != null)
                        {
                            var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(guid.stringValue);
                            return bp?.name ?? (nullable ? "" : null);
                        }
					}

                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
						return prop.objectReferenceValue
							? prop.objectReferenceValue.name
							: nullable
								? ""
								: null;
				}
				return null;
			};
		}
	}
}