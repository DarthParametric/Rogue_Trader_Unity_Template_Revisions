using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Owlcat.Editor.Core.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Elements
{
	public static class CopyPasteController
	{
		public class ClipboardElement
		{
			public readonly Type Type;
			public readonly object Object;

			public ClipboardElement(Type type, object o)
			{
				Type = type;
				Object = o;
			}

			public ClipboardElement(object o)
			{
				Type = o.GetType();
				Object = o;
			}
		}

		public static event Action ClipboardElementsChangedEvent;

		public static readonly List<ClipboardElement> ClipboardElements = new List<ClipboardElement>();
		private static readonly string ClipboardElementId = "-#@(element-copy)@#-";
		private static readonly string ClipboardBlueprintId = "-#@(blueprint-copy)@#-";
		private static readonly string ClipboardComponentId = "-#@(component-copy)@#-";

        private static RobustSerializedProperty s_CopiedProperty;
        public static bool IsThisCopied(SerializedProperty p)
        {
            return s_CopiedProperty?.Property != null &&
                   p!=null &&
                   s_CopiedProperty.serializedObject.targetObject == p.serializedObject.targetObject &&
                   s_CopiedProperty.Path == p.propertyPath;
        }

		public static bool HasBlueprintComponent
		{
			get { return ClipboardElements.Any(c => c.Object is BlueprintComponent); }
		}

	    public static void Process(Type type, SerializedProperty property, Object value = null,
	        Action<Object> pasteCallback = null)
	    {
	        TryCopy(property, value);
	        TryPaste(type, property, pasteCallback);
	    }

	    private static void TryCopy(SerializedProperty property, Object value)
		{
			var e = Event.current;
			if (e.commandName != "Copy")
				return;

            if (value == null)
            {
                if(property==null)
                    return;
                if(property.isArray)
                {
                    if (property.arraySize==0) 
                        return;
                    if(property.propertyType!=SerializedPropertyType.Generic)
                        return;
                    if (!property.arrayElementType.StartsWith("PPtr<") &&
                        !property.arrayElementType.StartsWith("managedReference<"))
                        return;
                }
                else
                {
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.ObjectReference:
                            if (property.objectReferenceValue == null)
                                return;
                            break;
                        case SerializedPropertyType.ManagedReference:
                            if (!property.HasManagedReference())
                                return;
                            break;
                        default:
                            return;
                    }
                }
            }

			if (e.type == EventType.ValidateCommand)
			{
				e.Use();
				return;
			}

			if (e.type != EventType.ExecuteCommand)
				return;
			e.Use();

			CopyProperty(property, value);

		}

		public static void CopyProperty(SerializedProperty property, object value)
		{
			var sourceType = SerializableTypesCollection.GetType(property);
			if (sourceType == typeof(ActionList))
			{
				var list = property.FindPropertyRelative("Actions");
				CopyProperty(list, null);
				return;
			}

			if (sourceType == typeof(ConditionsChecker))
			{
				var list = property.FindPropertyRelative("Conditions");
				CopyProperty(list, null);
				return;
			}

			ClipboardElements.Clear();
			ClipboardElementsChangedEvent?.Invoke();

			if(property != null && property.isArray)
			{
				for (int i = 0; i < property.arraySize; ++i)
				{
					object element = null;
					var p = property.GetArrayElementAtIndex(i);
					element = GetCopyableValueFromProperty(p);
					
					if (element != null)
					{
						ClipboardElements.Add(new ClipboardElement(element));
						ClipboardElementsChangedEvent?.Invoke();
					}
				}

				if (ClipboardElements.All(e => e.Object is SimpleBlueprint))
				{
					GUIUtility.systemCopyBuffer = ClipboardBlueprintId;
				}
				else if (ClipboardElements.All(e => e.Object is BlueprintComponent))
				{
					GUIUtility.systemCopyBuffer = ClipboardComponentId;
				}
				else
				{
					GUIUtility.systemCopyBuffer = ClipboardElementId;
				}
			}
			else
			{
				if (value == null && property != null)
                {
                    value = GetCopyableValueFromProperty(property);
                }
				
				if (value != null)
				{
					ClipboardElements.Add(new ClipboardElement(value));
					ClipboardElementsChangedEvent?.Invoke();
                    PFLog.Default.Log($"Copied obj: {value.GetType()}: {value}");
				}

				if (value is SimpleBlueprint)
				{
					GUIUtility.systemCopyBuffer = ClipboardBlueprintId;
				}
				else if (value is BlueprintComponent)
				{
					GUIUtility.systemCopyBuffer = ClipboardComponentId;
				}
				else
				{
					GUIUtility.systemCopyBuffer = ClipboardElementId;
				}
			}

            s_CopiedProperty = property;
		}

		private static object GetCopyableValueFromProperty(SerializedProperty p)
        {
            if (p.propertyType == SerializedPropertyType.ObjectReference)
            {
                return p.objectReferenceValue;
            }

            if (p.propertyType == SerializedPropertyType.ManagedReference)
            {
                return FieldFromProperty.GetFieldValue(p);
            }

            var type = SerializableTypesCollection.GetType(p);
            if (type.IsSubclassOf(typeof(BlueprintReferenceBase)))
            {
                return BlueprintReferenceBase.GetPropertyValue(p);
            }
            if (type.IsSubclassOf(typeof(ElementsReferenceBase)))
            {
                return ElementsReferenceBase.GetPropertyValue(p);
            }

            return null;
        }

        public static void TryPaste(Type type, SerializedProperty property, Action<Object> pasteCallback)
		{
			var e = Event.current;
			if (e.commandName != "Paste")
				return;

			if (GUIUtility.systemCopyBuffer == ClipboardElementId)
				TryPasteElements(type, property);
			if (GUIUtility.systemCopyBuffer == ClipboardBlueprintId)
				TryPasteBlueprint(type, property, pasteCallback);
		}

		public static void Paste(Type type, SerializedProperty property)
		{
			if (PasteProperty(type, property))
			{
				property.serializedObject.ApplyModifiedProperties();
				property.serializedObject.Update();
			}
		}

		private static void TryPasteBlueprint(Type type, SerializedProperty property, Action<Object> pasteCallback)
		{
			var e = Event.current;
			if (ClipboardElements == null)
				return;
			if (ClipboardElements.Count > 1)
				return;
			if (property!=null && property.isArray)
				return;

			var pasted = ClipboardElements[0];
			if (!type.IsInstanceOfType(pasted))
				return;

			if (e.type == EventType.ValidateCommand)
			{
				e.Use();
				return;
			}

			if (e.type != EventType.ExecuteCommand)
				return;
			e.Use();

		    if (pasteCallback != null)
		    {
		        pasteCallback((Object)pasted.Object);
		    }
		    else if(property!=null)
		    {
		        property.objectReferenceValue = (Object)pasted.Object;
		    }
		}

		private static void TryPasteElements(Type type, SerializedProperty property)
		{
			var e = Event.current;
			if (ClipboardElements == null)
				return;
			if (!property.isArray && ClipboardElements.Count != 1)
				return;

			if (e.type == EventType.ValidateCommand)
			{
				e.Use();
				return;
			}

			if (e.type != EventType.ExecuteCommand)
				return;
			e.Use();

			PasteProperty(type, property);
			
			property.serializedObject.ApplyModifiedProperties();
			property.serializedObject.Update();
		}

		public static bool IsSuitableForPaste(Type targetType)
		{
			if (ClipboardElements.Empty())
			{
				return false;
			}

			foreach (var e in ClipboardElements)
			{
				if (!IsSuitableForPaste(targetType, e))
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsSuitableForPaste(Type targetType, ClipboardElement e)
		{
			var obj = e.Object;
			if (e.Type == targetType || e.Type.IsSubclassOf(targetType))
			{
				return true;
			}

			var elementType = targetType.GetElementType();
			if (elementType != null && ( e.Type == elementType || e.Type.IsSubclassOf(elementType)))
			{
				return true;
			}

			if (targetType.IsSubclassOf(typeof(BlueprintReferenceBase)))
			{
				if (targetType.BaseType?.IsGenericType ?? false)
				{
					var typeArg = targetType.BaseType?.GenericTypeArguments.FirstItem();
					if (typeArg != null)
					{
						if (typeArg.IsInstanceOfType(obj))
						{
							return true;
						}
					}
				}
				else if (obj is BlueprintScriptableObject)
				{
					return true;
				}
			}

			if (targetType == typeof(ActionList) && obj is GameAction)
			{
				return true;
			}

			if (targetType == typeof(ConditionsChecker) && obj is Condition)
			{
				return true;
			}

			return false;
		}

		public static bool PasteProperty(Type type, SerializedProperty property)
		{
			if (property == null)
				return false;

			if (property.propertyType == SerializedPropertyType.ObjectReference &&
				ClipboardElements.Contains(x => x.Object.Equals(property.objectReferenceValue)))
				return false;

			if (!IsSuitableForPaste(type))
				return false;

			if (property.GetIndexInParentArray() != -1)
			{
				//Пока не понятно, как вытащить из Property ссылку на массив
				EditorUtility.DisplayDialog("Не доступно",
					"Для вставки элемента массива поверх существующего элемента массива используйте меню в шестерёнке Copy/Paste",
					"Хорошо");
				return false;
			}

			var targetType = SerializableTypesCollection.GetType(property);
			if (targetType == typeof(ActionList))
			{
				var list = property.FindPropertyRelative("Actions");
				return PasteProperty(type, list);
			}

			if (targetType == typeof(ConditionsChecker))
			{
				var list = property.FindPropertyRelative("Conditions");
				return PasteProperty(type, list);
			}

			if (property.isArray)
			{
				using (GuiScopes.UpdateObject(property))
				{
					foreach (var element in ClipboardElements)
					{
						property.arraySize++;
						var newItem = property.GetArrayElementAtIndex(property.arraySize - 1);
						PasteProperty(type, newItem, element);
					}
				}
			}
			else
			{
				PasteProperty(type, property, ClipboardElements[0]);
			}

			property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
			
			return true;
		}

		public static void PasteProperty(Type type, SerializedProperty property, ClipboardElement e)
		{
			if (!IsSuitableForPaste(type, e))
			{
				return;
			}

            if(property.propertyType == SerializedPropertyType.ManagedReference)
            {
                if (property.serializedObject.targetObjects.Length > 1)
                {
                    // after paste, we use the property's targetObject to set owner id values
                    // to support multiple targets, we'd have to divide the property here into multiple properties, because they'd have to have different owners 
                    // and copy each one individually (this would also break subsequent calls to ApplyModifiedProperties though)
                    PFLog.Default.Error("Pasting into multi-selection is not supported yet");
					return;
                }

                Undo.RecordObject(property.serializedObject.targetObject,"Paste");
                
                // before we set the value, we need to create a clone of the copied object, so that we do not actually _reference_ the object we copy
                var json = JsonUtility.ToJson(e.Object);
                var copy = JsonUtility.FromJson(json, e.Object.GetType());
                
                // set value (using property.managedReferenceValue would be way easier, but it's bugged as fuck)
				property.serializedObject.ApplyModifiedPropertiesWithoutUndo(); 
                FieldFromProperty.SetFieldValue(property, copy);
                property.serializedObject.Update();
                ((ScriptableWrapperBase)property.serializedObject.targetObject).SetBlueprintDirty();
                
				// Any components or elements in the object tree would have wrong OwnerId property,
				// so we need to fix that
				FixNestedPropertiesAfterPaste(property);
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

			var targetObject = property.serializedObject.targetObject;
			string assetPath = AssetDatabase.GetAssetPath(targetObject);
			bool isBlueprintReference = type.IsSubclassOf(typeof(BlueprintReferenceBase));

			if (e.Object is BlueprintScriptableObject blueprint)
			{
				if (isBlueprintReference)
				{
					BlueprintReferenceBase.SetPropertyValue(property, blueprint);
				}
            }
            else if (property.IsObjectRef())
			{
				var copy = CopyObject(targetObject, assetPath, (Object)e.Object);
				if (copy != null)
				{
					property.objectReferenceValue = copy;
				}
			}
			else if (e.Object is string str)
			{
				SerializedPropertySerializer.Deserialize(property, str);
			}
		}

        public static void FixNestedPropertiesAfterPaste(SerializedProperty property)
        {
	        var clonedProperty = property.Copy();
            var ownerId = (clonedProperty.serializedObject.targetObject as ScriptableWrapperBase)?.GetOwnerBlueprintId();
            if (ownerId != null)
            {
                var d = clonedProperty.depth;
                do
                {
                    if (clonedProperty.propertyType == SerializedPropertyType.ManagedReference)
                    {
						// check type
						var valType = FieldFromProperty.GetActualValueType(clonedProperty);
                        var fieldType = FieldFromProperty.GetDeclaredType(clonedProperty);
                        if (!fieldType.IsAssignableFrom(valType))
                        {
                            // apparently this happens all the time that a managed property that should be null suddenly references the containing object itself,
                            // even if the types don't match at all
                            //PFLog.Default.Error($"Property {property.propertyPath} has value of type {valType.Name} which is not {fieldType.Name}");
                            clonedProperty.managedReferenceValue = null;
                            continue;
                        }

						// make owner reference the actual owner
						var ownerProp = clonedProperty.FindPropertyRelative("m_OwnerId__");
                        if (ownerProp!=null)
                        {
                            ownerProp.stringValue = ownerId;
                        }

                        // make component/element name unique
                        var nameProp = clonedProperty.FindPropertyRelative("name");
                        if (nameProp != null && clonedProperty.propertyType==SerializedPropertyType.ManagedReference)
                        {
                            if (valType.IsOrSubclassOf<BlueprintComponent>() || valType.IsOrSubclassOf<Element>())
                            {
                                nameProp.stringValue = "$" + valType.Name + "$" + Guid.NewGuid();
                            }
                        }
                    }
                } while (clonedProperty.Next(clonedProperty.propertyType == SerializedPropertyType.Generic ||
                                       clonedProperty.propertyType == SerializedPropertyType.ManagedReference) &&
                         clonedProperty.depth > d);

            }
        }

        [CanBeNull]
		private static Object CopyObject(Object targetObject, string assetPath, Object objectForCopy)
		{
			// todo: [bp] write deep copy code for blueprints

			var result = DeepCopy(assetPath, objectForCopy);
			return result;
		}

		public static Object DeepCopy(string targetAssetPath, Object source)
		{
			if (source == null)
				return null;

			Object copy = Object.Instantiate(source);
			copy.hideFlags = HideFlags.HideInHierarchy;
			copy.name = "$" + copy.GetType().Name + "$" + Guid.NewGuid();
			EditorUtility.SetDirty(copy);
			AssetDatabase.AddObjectToAsset(copy, targetAssetPath);

            // todo: [bp] write deep copy code for blueprints

            DeepCopyLinks(targetAssetPath, source, copy);

			return copy;
		}

		public static void DeepCopyLinks(string targetAssetPath, Object source, Object target)
		{
			var sourceSo = new SerializedObject(source);

			var property = sourceSo.GetIterator();
			while (property.Next(true))
			{
				if (property.propertyType != SerializedPropertyType.ObjectReference)
					continue;
				Object child = property.objectReferenceValue;
				if (child == null)
					continue;
				
                var childCopy = DeepCopy(targetAssetPath, child);
				ReflectionUtils.SetFieldValueByPath(target, property.propertyPath, childCopy);
			}
		}
	}
}