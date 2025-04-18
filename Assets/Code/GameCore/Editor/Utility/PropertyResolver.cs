using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using UnityEditor;

namespace Kingmaker.Editor.Utility
{
	public static class PropertyResolver
	{
		public static T GetPropertyObject<T>(SerializedProperty prop)
		{
			var v = GetPropertyObject(prop);
			try
			{
				return (T)v;
			}
			catch
			{
				PFLog.Default.Error($"Resolving exception: {prop.propertyPath}({nameof(T)}): {v}");
				throw;
			}
		}

		[CanBeNull]
		public static object GetPropertyObject(SerializedProperty prop)
		{
		    if (prop.serializedObject.targetObjects.Length > 1)
		        return null; // multiple objects: cannot select just one

            return FieldFromProperty.GetFieldValue(prop);
		}
	}
}
