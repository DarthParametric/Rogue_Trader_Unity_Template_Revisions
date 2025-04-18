using System;
using System.Collections.Generic;
using System.Globalization;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Utility
{
	public class SerializedPropertySerializer
	{
		[Serializable]
		class PropertyValue
		{
			public string StrValue;
			public UnityEngine.Object ObjectValue;
            [SerializeReference]
            public object ManagedValue;
			public string PropertyPath;
			public int ProperyDepth;
			public SerializedPropertyType PropertyType;

			public PropertyValue(SerializedProperty p)
			{
				switch (p.propertyType)
				{
					case SerializedPropertyType.Generic:
						break;
					case SerializedPropertyType.Integer:
						StrValue = p.intValue.ToString();
						break;
					case SerializedPropertyType.Boolean:
						StrValue = p.boolValue.ToString();
						break;
					case SerializedPropertyType.Float:
						StrValue = p.floatValue.ToString(CultureInfo.InvariantCulture);
						break;
					case SerializedPropertyType.String:
						StrValue = p.stringValue;
						break;
					case SerializedPropertyType.Color:
						StrValue = p.colorValue.ToString();
						break;
					case SerializedPropertyType.ObjectReference:
						ObjectValue = p.objectReferenceValue;
						break;
					case SerializedPropertyType.LayerMask:
						StrValue = p.intValue.ToString();
						break;
					case SerializedPropertyType.Enum:
						StrValue = p.intValue.ToString();
						break;
					case SerializedPropertyType.Vector2:
						StrValue = p.vector2Value.ToString();
						break;
					case SerializedPropertyType.Vector3:
						StrValue = p.vector3Value.ToString();
						break;
					case SerializedPropertyType.Vector4:
						StrValue = p.vector4Value.ToString();
						break;
					case SerializedPropertyType.Rect:
						StrValue = p.rectValue.ToString();
						break;
					case SerializedPropertyType.ArraySize:
						StrValue = p.intValue.ToString();
						break;
					case SerializedPropertyType.Character:
						StrValue = p.stringValue;
						break;
					case SerializedPropertyType.AnimationCurve:
						// no idea actually
						break;
					case SerializedPropertyType.Bounds:
						StrValue = p.boundsValue.ToString();
						break;
					case SerializedPropertyType.Gradient:
						// no idea actually
						break;
					case SerializedPropertyType.Quaternion:
						StrValue = p.quaternionValue.ToString();
						break;
					case SerializedPropertyType.ExposedReference:
						ObjectValue = p.exposedReferenceValue;
						break;
                    case SerializedPropertyType.FixedBufferSize:
                        // what even IS this thing?
                        break;
                    case SerializedPropertyType.ManagedReference:
                        ManagedValue = FieldFromProperty.GetFieldValue(p);
                        break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				PropertyType = p.propertyType;
				PropertyPath = p.propertyPath;
				ProperyDepth = p.depth;
			}

			public void Restore(SerializedProperty tgt)
			{
				if (tgt == null)
				{
					PFLog.Default.Error("no target property found for " + PropertyPath);
					return;
				}
				if (tgt.propertyType != PropertyType)
				{
					PFLog.Default.Error($"Target property for {PropertyPath} is {tgt.propertyType} but {PropertyType} expected");
					return;
				}
				switch (PropertyType)
				{
					case SerializedPropertyType.Generic:
						break;
					case SerializedPropertyType.Integer:
						tgt.intValue = int.Parse(StrValue);
						break;
					case SerializedPropertyType.Boolean:
						tgt.boolValue = bool.Parse(StrValue);
						break;
					case SerializedPropertyType.Float:
						tgt.floatValue = float.Parse(StrValue, CultureInfo.InvariantCulture);
						break;
					case SerializedPropertyType.String:
						tgt.stringValue = StrValue;
						break;
					case SerializedPropertyType.Color:
						break;
					case SerializedPropertyType.ObjectReference:
						tgt.objectReferenceValue = ObjectValue;
						break;
					case SerializedPropertyType.LayerMask:
						tgt.intValue = int.Parse(StrValue);
						break;
					case SerializedPropertyType.Enum:
						tgt.intValue = int.Parse(StrValue);
						break;
					case SerializedPropertyType.Vector2:
						break;
					case SerializedPropertyType.Vector3:
						break;
					case SerializedPropertyType.Vector4:
						break;
					case SerializedPropertyType.Rect:
						break;
					case SerializedPropertyType.ArraySize:
						break;
					case SerializedPropertyType.Character:
						break;
					case SerializedPropertyType.AnimationCurve:
						break;
					case SerializedPropertyType.Bounds:
						break;
					case SerializedPropertyType.Gradient:
						break;
					case SerializedPropertyType.Quaternion:
						break;
					case SerializedPropertyType.ExposedReference:
						tgt.exposedReferenceValue = ObjectValue;
						break;
                    case SerializedPropertyType.FixedBufferSize:
                        tgt.intValue = int.Parse(StrValue);
                        break;
                    case SerializedPropertyType.ManagedReference:
                        tgt.serializedObject.ApplyModifiedProperties();
                        FieldFromProperty.SetFieldValue(tgt, ManagedValue);
                        tgt.serializedObject.Update();
                        break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		[Serializable]
		class PropertyList
		{
			public List<PropertyValue> List = new List<PropertyValue>();
		}

		public static string Serialize(SerializedProperty p)
		{
			var values = new PropertyList();
			var d = p.depth;
			do
			{
				values.List.Add(new PropertyValue(p));
				if (!p.Next(true))
					break;
			} while (p.depth > d);
			return EditorJsonUtility.ToJson(values, true);
		}
		public static void Deserialize(SerializedProperty p, string json)
		{
			var values = JsonUtility.FromJson<PropertyList>(json);
			var basePath = values.List[0].PropertyPath;
			var tgtPath = p.propertyPath;
			for (int ii = 0; ii < values.List.Count; ii++)
			{
				var pv = values.List[ii];
				var path = pv.PropertyPath.Replace(basePath, tgtPath);
				var tgt = p.serializedObject.FindProperty(path);
				pv.Restore(tgt);
			}
		}
	}
}