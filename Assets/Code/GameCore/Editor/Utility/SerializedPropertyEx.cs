using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Code.Editor.Utility
{
	public static class SerializedPropertyEx
	{
		private static readonly Regex IndexedRx = new Regex("(\\w+)[[](\\d+)[]]");

		public static SerializedProperty GetParent(this SerializedProperty property)
		{
			var lastDot = property.propertyPath.LastIndexOf(".", StringComparison.Ordinal);
			return lastDot < 0
				? null
				: property.serializedObject.FindProperty(property.propertyPath.Substring(0, lastDot));
		}
		public static bool IsArrayElement(this SerializedProperty property)
		{
			return property.GetParent()?.isArray ?? false;
		}
		public static bool IsArrayOfReferences(this SerializedProperty property)
		{
			return property.isArray && property.arrayElementType.StartsWith("PPtr<");
		}
		public static bool ValueIsReference(this SerializedProperty property)
		{
			return property.propertyType == SerializedPropertyType.ObjectReference || property.IsArrayOfReferences();
		}
		public static int GetIndexInParentArray(this SerializedProperty property)
		{
			var lastDot = property.propertyPath.LastIndexOf('.');
			var left = property.propertyPath.LastIndexOf("[", StringComparison.Ordinal) + 1;
			var right = property.propertyPath.LastIndexOf("]", StringComparison.Ordinal);
			if (left > 0 && right > left && left > lastDot)
			{
				int res;
				if (int.TryParse(property.propertyPath.Substring(left, right - left), out res))
					return res;
			}
			return -1;
		}

		public static bool IsObjectRef(this SerializedProperty property)
		{
			return property.propertyType == SerializedPropertyType.ObjectReference;
		}

		[CanBeNull]
		public static Object GetObjectRef(this SerializedProperty property)
		{
			return property.IsObjectRef() ? property.objectReferenceValue : null;
		}

		public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
		{
			property = property.Copy();
			var nextElement = property.Copy();
			bool hasNextElement = nextElement.NextVisible(false);
			if (!hasNextElement)
			{
				nextElement = null;
			}

			property.NextVisible(true);
			while (true)
			{
				if ((SerializedProperty.EqualContents(property, nextElement)))
				{
					yield break;
				}

				yield return property;

				bool hasNext = property.NextVisible(false);
				if (!hasNext)
				{
					break;
				}
			}
		}

		public static IEnumerable<Attribute> GetAttributes(this SerializedProperty prop)
		{
			var propInfo = prop.GetFieldInfo();
			return propInfo != null ? propInfo.GetCustomAttributes() : new Attribute[0];
		}
        
		public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            return FieldFromProperty.GetFieldInfo(property);
		}

        public static bool HasManagedReference(this SerializedProperty p)
        {
	        if (p.propertyType != SerializedPropertyType.ManagedReference)
		        return false;
	        
            return !string.IsNullOrEmpty(p.managedReferenceFullTypename);
        }

		public static string GetTooltip(this SerializedProperty prop)
		{
			var tooltipAttr = (TooltipAttribute)prop.GetAttributes().Where(attr => attr.GetType() == typeof(TooltipAttribute)).FirstOrDefault();
			return tooltipAttr != null ? tooltipAttr.tooltip : string.Empty;
		}

        /// <summary>
        /// Gets the object the property represents.
        /// </summary>
        public static object GetTargetObjectOfProperty(this SerializedProperty p)
        {
            string path = p.propertyPath.Replace(".Array.data[", "[");
            object obj = p.serializedObject.targetObject;
            string[] parts = path.Split('.');
            foreach (string part in parts)
            {
	            var indexedMatch = IndexedRx.Match(part);
                if (indexedMatch.Success)
                {
                    string elementName = indexedMatch.Groups[1].Value;
                    int index = Convert.ToInt32(indexedMatch.Groups[2].Value);
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, part);
                }
            }
            return obj;
        }

        private static object GetValue(object source, string name)
        {
	        if (source == null)
	        {
		        return null;
	        }
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                {
	                return f.GetValue(source);
                }

                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
	                return p.GetValue(source, null);
                }

                type = type.BaseType;
            }
            return null;
        }

        private static object GetValue(object source, string name, int index)
        {
	        if (GetValue(source, name) is not IEnumerable enumerable)
	        {
		        return null;
	        }

	        var enm = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
	            if (!enm.MoveNext())
	            {
		            return null;
	            }
            }
            return enm.Current;
        }
	}
}