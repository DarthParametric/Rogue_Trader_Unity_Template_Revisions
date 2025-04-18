#if UNITY_EDITOR 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.Creation;
using Kingmaker.Editor.Utility;
using Owlcat.Editor.Core.Utility;
using Owlcat.Editor.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Blueprints
{
    //[CustomPropertyDrawer(typeof(BlueprintScriptableObject), true)]
	public class BlueprintLinkDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var createPath = Enumerable.FirstOrDefault(fieldInfo?.FieldType.GetCustomAttributes(typeof(CreatePathAttribute), true)) as CreatePathAttribute;
			var type = GetElementType(fieldInfo?.FieldType) ?? fieldInfo?.FieldType;

			OnGUI(position, property, fieldInfo, label, type, createPath?.Path);
		}

		public static void OnGUI(Rect position, SerializedProperty property, [CanBeNull] FieldInfo fieldInfo,
			GUIContent label, Type blueprintType, string createPath = null)
		{
			DrawField(position, property, fieldInfo, label, objectType: blueprintType, defaultCreationPath: createPath);
		}

		public static void DrawField(Rect position,
			RobustSerializedProperty property = null,
			FieldInfo fieldInfo = null,
			GUIContent label = null,
			BlueprintScriptableObject obj = null,
			Type objectType = null,
			string defaultCreationPath = null,
			bool? showNicolayButton = null,
			Action<BlueprintScriptableObject> onChange = null,
			string defaultCreatedName = null)
		{
			if (property?.Property != null)
			{
                if (BlueprintReferenceBase.IsReference(property.Property))
				{
					obj = BlueprintReferenceBase.GetPropertyValue(property.Property);
				}
				else
				{
					PFLog.Default.Error(
						"BlueprintLinkDrawer: unsupported property " +
						property.Property.serializedObject.targetObject + "." + property.Path);
				}
			}

			if (fieldInfo != null)
			{
				var createPathAttribute =
					fieldInfo.GetCustomAttributes(typeof(CreatePathAttribute), true)
						.FirstOrDefault() as CreatePathAttribute
					?? fieldInfo.FieldType.GetCustomAttributes(typeof(CreatePathAttribute), true)
						.FirstOrDefault() as CreatePathAttribute;

				defaultCreationPath = defaultCreationPath ?? createPathAttribute?.Path;

				var createNameAttribute =
					fieldInfo.GetCustomAttributes(typeof(CreateNameAttribute), true)
						.FirstOrDefault() as CreateNameAttribute
					?? fieldInfo.FieldType.GetCustomAttributes(typeof(CreateNameAttribute), true)
						.FirstOrDefault() as CreateNameAttribute;

				defaultCreatedName = defaultCreatedName ?? createNameAttribute?.Name;

				showNicolayButton = showNicolayButton ?? (fieldInfo.FieldType.HasAttribute<ShowCreatorAttribute>() ||
				                                          fieldInfo.HasAttribute<ShowCreatorAttribute>()) && obj == null;
			}

			if (objectType == null)
			{
				objectType = GetElementType(fieldInfo?.FieldType) ?? fieldInfo?.FieldType ?? obj?.GetType() ?? typeof(BlueprintScriptableObject); // can we get type from property?
			}

			if (property != null && label == null)
			{
				label = new GUIContent(property.Property.displayName);
			}

			position.xMax -= 16;
			var showCreateButton = (defaultCreationPath != null && obj == null) || (showNicolayButton ?? false);
			if (showCreateButton)
			{
				position.xMax -= 40;
			}

			Action<object> assetPickerCallback = o =>
            {
                // todo: [bp] support SimpleBlueprint here and not just BSO
                var bp = o is ScriptableObject scr
                    ? BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(scr)
                    : o as SimpleBlueprint;
                
				if (onChange != null)
				{
					onChange.Invoke(bp as BlueprintScriptableObject);
				}
				else if (property != null)
				{
					using (GuiScopes.UpdateObject(property.Property))
					{
						if (property.Property.propertyType == SerializedPropertyType.ObjectReference)
						{
							property.Property.objectReferenceValue = o as Object; // todo: only needed with old blueprints
						}
						else if (BlueprintReferenceBase.IsReference(property.Property))
						{
							BlueprintReferenceBase.SetPropertyValue(property.Property, bp as BlueprintScriptableObject);
						}
						else
						{
							PFLog.Default.Error(
								"BlueprintLinkDrawer: unsupported property " +
								property.Property.serializedObject.targetObject + "." + property.Path);
						}
					}
				}
			};

			// var controlName = property != null
			// 	? (property.serializedObject.targetObject.GetInstanceID() + property.Path)
			// 	: fieldInfo != null
			// 		? fieldInfo.Name + "_field" + obj?.GetInstanceID()
			// 		: label?.text + "_field" + obj?.GetInstanceID();
            EditorGUI.LabelField(position, label,new GUIContent("NOT IMPLEMENTED"));

			var e = GUI.enabled;
			GUI.enabled = true; // force GUI enabled (this button should work even if property in inherited and not overridden)
			if (GUI.Button(new Rect(position.xMax, position.y, 16, 16), "", OwlcatEditorStyles.Instance.OpenButton) && obj)
			{
				BlueprintInspectorWindow.OpenFor(obj);
			}
			GUI.enabled = e;

			if (showCreateButton)
			{
				if (GUI.Button(new Rect(position.xMax + 16, position.y, 40, 16), (showNicolayButton ?? false) ? "new.." : "new", EditorStyles.miniButton))
				{
					if (property != null)
					{
						defaultCreatedName = TextTemplates.ReplacePropertyNames(defaultCreatedName ?? "", property.serializedObject);
						defaultCreationPath = TextTemplates.ReplacePropertyNames(defaultCreationPath ?? "", property.serializedObject);
					}

					if (showNicolayButton ?? false)
					{
						// find creator for type?
						var creator = CreatorPicker.GetCreatorForType(objectType);
						if (creator)
						{
							creator.SetRootObject(property.targetObject);
							NewAssetWindow.ShowWindow(creator, defaultCreatedName, assetPickerCallback);
						}
					}
					else
					{
						var name = defaultCreatedName ?? obj.name;
						var created = CreateAsset(objectType, defaultCreationPath, name);
						assetPickerCallback(created);
					}
				}
			}
		}

		public static SimpleBlueprint CreateAsset(Type type, string path, string name)
        {
            return BlueprintsDatabase.CreateAsset((SimpleBlueprint)Activator.CreateInstance(type), path, name);
        }

        public static T CreateAsset<T>(string path, string name) where T : SimpleBlueprint
		{
            return (T)CreateAsset(typeof(T), path, name);
        }

		public static Type GetElementType(Type collectionType)
		{
			if (collectionType.IsArray)
				return collectionType.GetElementType();
			if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(List<>))
				return collectionType.GetGenericArguments()[0];
			return null;
		}
	}
}
#endif