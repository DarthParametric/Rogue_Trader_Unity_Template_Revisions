using System.Reflection;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
	/// <summary>
	///     Used to match SerializedProperty instances to FieldInfo's to get field attributes in inspector.
	///     Only works with blueprints and components
	/// </summary>
	internal class PropertyToFieldMatcher
	{
		public static PropertyToFieldMatcher GetMatcher(object obj)
		{
            if (obj == null)
            {
				return null;
            }
			return new PropertyToFieldMatcher();
		}

		public FieldInfo GetMatchingField(SerializedProperty prop)
		{
            return FieldFromProperty.GetFieldInfo(prop);
		}
	}
}