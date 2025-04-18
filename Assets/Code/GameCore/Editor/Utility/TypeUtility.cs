using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Persistence.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using UnityEngine;
using Owlcat.Runtime.Core.Utility;
using Owlcat.Editor.Core.Utility;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;

namespace Kingmaker.Editor.Elements
{
	public static class TypeUtility 
	{
		public static IEnumerable<Type> GetValidTypes(RobustSerializedProperty rsp, Type elementType)
		{
            var mainAsset = rsp.targetObject as ScriptableObject;
            var blueprintComponent = (mainAsset as BlueprintComponentEditorWrapper)?.Component;
            var blueprint = blueprintComponent?.OwnerBlueprint;
            var blueprintType = blueprint?.GetType();

            var mainAssetType = mainAsset?.GetWrappedType();

            if (mainAssetType != null && mainAssetType.HasAttribute<PlayerUpgraderFilterAttribute>())
            {
	            return GetDerivedTypesRecursively(elementType)
		            .Where(et => et.HasAttribute<PlayerUpgraderAllowedAttribute>() || et.IsOrSubclassOf<PlayerUpgraderOnlyAction>())
		            .OrderBy(t => t.Name);
            }
            
            if (mainAssetType != null && mainAssetType.HasAttribute<UnitUpgraderFilterAttribute>())
            {
	            return GetDerivedTypesRecursively(elementType)
		            .Where(et => et.IsOrSubclassOf<UnitUpgraderOnlyAction>())
		            .OrderBy(t => t.Name);
            }

            var result = TypeCache.GetTypesDerivedFrom(elementType).Where(t => !t.IsAbstract);

            if (!elementType.IsAbstract)
	            result = LinqExtensions.Concat(result, elementType);

            if (blueprintType != null)
            {
	            result = result.Where(et =>
	            {
		            var attrs = et.GetCustomAttributes(typeof(AllowedOnAttribute), true);
		            return !(attrs.Length > 0) && attrs.Cast<AllowedOnAttribute>().All(attr => !(blueprintType.IsSubclassOf(attr.Type) || (blueprintType == attr.Type)));
	            });
            }
            
            if (elementType.IsOrSubclassOf<GameAction>())
            {
	            result = result.Where(t => !t.IsOrSubclassOf<PlayerUpgraderOnlyAction>() && 
	                                       !t.IsOrSubclassOf<UnitUpgraderOnlyAction>());
            }


            return result.OrderBy(t => t.Name);
		}

		public static void AddElementFromMenu(RobustSerializedProperty property, Type type)
		{
            // hmm, why is this method in TypeUtility exactly?
            var owner = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(property.serializedObject.targetObject) as SimpleBlueprint;
            if (owner == null)
            {
                owner = (property.serializedObject.targetObject as BlueprintComponentEditorWrapper)?.Component
                    ?.OwnerBlueprint;
            }
            if (owner == null)
            {
                PFLog.Default.Error($"Cannot add element: {property.serializedObject.targetObject} is not an element owner");
                return;
            }
            
            var element = (Element)Activator.CreateInstance(type);
            owner.AddNewElement(element);
            ElementWorkspaceContextualPopulationController.PrefillWithTargets(element, element.Owner);
            UpdateProperty(property, element);
        }

        private static void UpdateProperty(RobustSerializedProperty property, Element element)
        {
            using (GuiScopes.UpdateObject(property.serializedObject))
            {
                if (property.Property.isArray)
                {
                    property.Property.arraySize++;
                    property.serializedObject.ApplyModifiedProperties();
                    FieldFromProperty.SetFieldValue(property.Property.GetArrayElementAtIndex(property.Property.arraySize - 1), element);
                    property.serializedObject.Update();
                }
                else
                {
                    property.serializedObject.ApplyModifiedProperties();
                    FieldFromProperty.SetFieldValue(property.Property, element);
                    property.serializedObject.Update();
                }
            }
        }

        private static IEnumerable<Type> GetDerivedTypesRecursively(Type elementType)
        {
	        var result = new HashSet<Type>();
	        foreach (var type in TypeCache.GetTypesDerivedFrom(elementType))
	        {
		        if (type.IsAbstract)
			        GetDerivedTypesRecursively(type).ForEach(x=>result.Add(x));
		        else
			        result.Add(type);
	        }

	        return result;
        }

        private static Dictionary<string, Type> m_NameToTypeMap = new();
        public static bool TryGetTypeByName(string name, out Type type)
        {
			if (m_NameToTypeMap.Count == 0)
				FillNameToTypeMap();

			return m_NameToTypeMap.TryGetValue(name, out type);
        }

        private static void FillNameToTypeMap()
        {
	        Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
	        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		        foreach (Type type in assembly.GetTypes())
			        dictionary[type.Name] = type;
	        
	        m_NameToTypeMap = dictionary;
        }
	}
}