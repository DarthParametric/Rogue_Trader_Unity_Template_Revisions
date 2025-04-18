using Code.GameCore.Blueprints.BlueprintPatcher;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.Converters;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.Helpers;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Persistence.JsonUtility;
using Kingmaker.EntitySystem.Persistence.JsonUtility.Core;
using Kingmaker.Localization;
using Kingmaker.Networking.Serialization.Converters;
using Kingmaker.Utility.UnityExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Owlcat.Runtime.Core.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	public static class BlueprintPatchEditorUtility
	{
		
		private class ValueHolder
		{
			public object TargetValue;
			public object ProtoValue;

			public ValueHolder(object targetValue, object protoValue)
			{
				TargetValue = targetValue;
				ProtoValue = protoValue;
			}
		}
		
		private class ArrayOverrideInfo
		{
			public readonly string FieldPath;
			public readonly Type ItemType;
			private readonly ValueHolder m_Holder;

			private IList m_ProtoList;
			private IList m_TargetList;

			public IList ProtoList
				=> m_ProtoList ??= m_Holder.ProtoValue as IList;
			public IList TargetList
				=> m_TargetList ??= m_Holder.TargetValue as IList;

			public ArrayOverrideInfo(string fieldPath, ValueHolder holder)
			{
				FieldPath = fieldPath;
				m_Holder = holder;
				ItemType = GetListElementType(holder.TargetValue.GetType());
				Logger.Log($"Array element type is {ItemType}");
			}

			public static Type GetListElementType(Type type)
			{
				var elementType = type.GetInterfaces()
					.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<object>).GetGenericTypeDefinition())
					?.GetGenericArguments()[0];

				return elementType;
			}
		}
		
		private static BlueprintJsonWrapper s_ProtoBlueprint;
		private static List<string> s_Overrides = new();
		private static string s_ProtoId;
		private static BlueprintScriptableObject s_Target;
		private static List<BlueprintFieldOverrideOperation> s_FieldOverrides = new();
		private static List<BlueprintSimpleArrayPatchOperation> s_ArrayPatches = new();
		private static List<BlueprintComponentsPatchOperation> s_ComponentsPatches = new();
		private static List<ArrayOverrideInfo> s_OverridenArrays = new();
		
		private static readonly LogChannel Logger = LogChannelFactory.
			GetOrCreate(nameof(BlueprintPatchEditorUtility));
		// private static readonly JsonSerializer Serializer = JsonSerializer.Create
		// 	(new JsonSerializerSettings
		// 		{Formatting = Formatting.Indented}
		// 	);
		private static readonly JsonSerializer Serializer = Json.Serializer;

		private static void SetTarget(BlueprintScriptableObject blueprint)
		{
			if (blueprint == null) return;

			Logger.Log($"Setting target: {blueprint.name} || {blueprint.AssetGuid}");
			
			Reset();
			
			s_Target = blueprint;
			
			SetupProto(blueprint);
			
			if(!GetOverrides(blueprint)) return;
			
			ParseOverrides(blueprint);
			
			ConvertArrayOverridesIntoOperations();
		}

		private static void SetupProto(BlueprintScriptableObject blueprint)
		{
			if (!GetProtoId(blueprint)) return;
			
			//Get proto bp itself
			var protoBlueprint = BlueprintsDatabase.LoadWrapperById(s_ProtoId);

			if (protoBlueprint == null)
			{
				Logger.Error($"Prototype object for protoId {s_ProtoId} not found. Something went completely wrong");
				return;
			}

			s_ProtoBlueprint = protoBlueprint;
		}

		private static void ConvertArrayOverridesIntoOperations()
		{
			foreach (var overridenArray in s_OverridenArrays)
			{
				ConvertSimpleArray(overridenArray);
			}
		}
		
		private static bool IsSimple(Type type)
		{
			return type.IsPrimitive 
			       || type.Equals(typeof(string));
		}
		
		/// <summary>
		/// Both collections (proto and target) shouldn't contain null elements.
		/// </summary>
		/// <param name="overrideInfo"></param>
		private static void ConvertSimpleArray(ArrayOverrideInfo overrideInfo)
		{
			Logger.Log($"Analyzing simple array: {overrideInfo.FieldPath}");

			//Get only in proto items
			foreach (var protoItem in overrideInfo.ProtoList)
			{
				if (protoItem == null)
				{
					Logger.Error($"Null element found while parsing array changes for field {overrideInfo.FieldPath}. It's ok. This item will be skipped, but checkout bp overrides are valid");
					continue;
				}
				
				bool match = false;
				foreach (var targetItem in overrideInfo.TargetList)
				{
					if (targetItem == null)
					{
						Logger.Error($"Null element found while parsing array changes for field {overrideInfo.FieldPath}. It's ok. This item will be skipped, but checkout bp overrides are valid");
						continue;
					}
					
					match = BlueprintPatchObjectComparator.ObjectsAreEqual(protoItem, targetItem, overrideInfo.FieldPath);
					//Items are the same, we'll skip it
					if(match)
						break;
				}
				
				//Only in proto items are treated as RemoveElement operations
				if (match)
					continue;
				
				var isItemBlueprintReference = typeof(BlueprintReferenceBase).IsAssignableFrom(overrideInfo.ItemType);
				// var valueForOperation = isItemBlueprintReference ?
				// 	((BlueprintReferenceBase)protoItem).Guid :
				// 	protoItem;

				Logger.Log($"Element: {protoItem} found only in proto, not in target.");
				var removeOperation = new BlueprintSimpleArrayPatchOperation()
				{
					FieldName = overrideInfo.FieldPath,
					OperationType = BlueprintPatchOperationType.RemoveElement,
					Value = protoItem
				};
				s_ArrayPatches.Add(removeOperation);
				Logger.Log($"Adding RemoveElement operation: {protoItem}");
			}
			
			//Get only in target items
			for (var i = 0; i < overrideInfo.TargetList.Count; i++)
			{
				var targetItem = overrideInfo.TargetList[i];
				if (targetItem == null)
				{
					Logger.Error($"Null element found while parsing array changes at index {i} for field {overrideInfo.FieldPath}. It's ok. This item will be skipped, but checkout bp overrides are valid");
					continue;
				}
				
				bool match = false;
				foreach (var protoItem in overrideInfo.ProtoList)
				{
					if (protoItem == null)
					{
						Logger.Error($"Null element found while parsing array changes for field {overrideInfo.FieldPath}. It's ok. This item will be skipped, but checkout bp overrides are valid");
						continue;
					}
					
					match = BlueprintPatchObjectComparator.ObjectsAreEqual(protoItem, targetItem, overrideInfo.FieldPath);
					//Items are the same, we'll skip it
					if(match)
						break;
				}
				if(match)
					continue;
				
				//Insert at the beginning operation
				if (i == 0)
				{
					var atBeggingOperation = new BlueprintSimpleArrayPatchOperation()
					{
						FieldName = overrideInfo.FieldPath,
						OperationType = BlueprintPatchOperationType.InsertAtBeginning,
						Value = targetItem,
					};
					s_ArrayPatches.Add(atBeggingOperation);
					Logger.Log($"Adding InsertAtBeginning operation with {targetItem}");
					continue;
				}
				
				//InsertAfter operation
				var operation = new BlueprintSimpleArrayPatchOperation()
				{
					FieldName = overrideInfo.FieldPath,
					OperationType = BlueprintPatchOperationType.InsertAfterElement,
					Value = targetItem,
					TargetValue = overrideInfo.TargetList[i - 1]
				};
				s_ArrayPatches.Add(operation);
				Logger.Log($"Adding InsetAfter operation with {targetItem} after guid {overrideInfo.TargetList[i - 1]}");
			}
		}

		private static void ParseOverrides(BlueprintScriptableObject blueprint)
		{
			var failedOverrides = new List<string>();
			s_OverridenArrays = new List<ArrayOverrideInfo>();
			s_FieldOverrides = new List<BlueprintFieldOverrideOperation>();
			s_ComponentsPatches = new List<BlueprintComponentsPatchOperation>();
			
			foreach (string fieldName in s_Overrides)
			{
				string[] split = fieldName.Split('.');
				object target = blueprint;
				object protoTarget = s_ProtoBlueprint.Data;

				var holder = new ValueHolder(target, protoTarget);
				
				//Assume this field is from Components array
				//All components should have name pattern like "$name$guid"
				if (fieldName.StartsWith('$') && fieldName.Count(x=> x == '$') == 2)
				{
					var componentPatch = GetComponentElementValue(holder, fieldName);
					if (componentPatch != null)
					{
						Logger.Log($"Adding Component patch operation :{componentPatch}");
						s_ComponentsPatches.Add(componentPatch);
					}
					else
					{
						Logger.Log($"Component patch on {fieldName} skipped.");
					}
					continue;
				}
				
				foreach (string part in split)
				{
					holder = GetFieldValue(holder, part);
					if (holder == null)
					{
						Logger.Error($"Failed to resolve override {fieldName}" +
						             $" in bp {blueprint.name} | {blueprint.AssetGuid}." +
						             $" Maybe this field is private in parent class. Ignoring change.");
						break;
					}
				}

				if (holder == null)
				{
					failedOverrides.Add(fieldName);
					continue;
				}

				bool isList = CheckTypeIsGenericList(holder.TargetValue);
				//Will parse arrays later
				if (holder.TargetValue.GetType().IsArray || isList)
				{
					var item = new ArrayOverrideInfo(fieldName, holder);
					s_OverridenArrays.Add(item);
					IList protoVals =  holder.ProtoValue as IList;
					IList targetVals = holder.TargetValue as IList;
					Logger.Log($"Adding Array operation {fieldName} proto: {protoVals.Count} target: {targetVals.Count}");
					continue;
				}
				
				//Regular fields are parsed jit
				ParseFieldOverride(holder, fieldName);
			}

			if(failedOverrides.Count == 0)
				return;
			
			Logger.Error($"{failedOverrides.Count} fails occured while creating patch:");
			foreach (string failedOverride in failedOverrides)
			{
				Logger.Error($"Failed with field: {failedOverride}");
			}
		}

		private static void ParseFieldOverride(ValueHolder holder, string fieldName)
		{
			if (BlueprintPatchObjectComparator.ObjectsAreEqual(holder.ProtoValue, holder.TargetValue, fieldName))
			{
				Logger.Log($"Equal objects are detected while parsing blueprint's override field {fieldName}. Skipping override...");
				return;
			}
			
			var operation = new BlueprintFieldOverrideOperation()
			{
				FieldName = fieldName,
				FieldValue = holder.TargetValue
			};
			Logger.Log($"Adding override operation: {operation.FieldName} {operation.FieldValue}");
			s_FieldOverrides.Add(operation);
		}

		private static bool CheckTypeIsGenericList(object target)
			=> target.GetType().IsGenericType && (target.GetType().GetGenericTypeDefinition() == typeof(List<>));


		private const BindingFlags RegularFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PrivateParentFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        
        private static ValueHolder GetFieldValue(ValueHolder holder, string fieldName)
        {
        	//Target blueprint
        	var targetType = holder.TargetValue.GetType();
        	var fieldInfo = GetFieldInfoInType(targetType, fieldName);
        	if (fieldInfo == null)
        	{				
        		Logger.Error($"Failed to get field {fieldName} in target type {targetType}");
        		return null;
        	}
        
        	//Prototype blueprint
        	var protoType = holder.ProtoValue.GetType();
        	var protoFieldInfo = GetFieldInfoInType(protoType, fieldName);
        	if (protoFieldInfo == null)
        	{
        		Logger.Error($"Failed to get field {fieldName} in proto type {protoType}");
        		return null;
        	}
        
        	return new ValueHolder(fieldInfo.GetValue(holder.TargetValue), protoFieldInfo.GetValue(holder.ProtoValue));
        }
        
        private static FieldInfo GetFieldInfoInType(Type type, string fieldName)
        {
	        var fieldInfo = type.GetField(fieldName, RegularFlags);
	        if (fieldInfo != null) 
		        return fieldInfo;
			
	        //Search among private fields of base classes
	        while ((type = type.BaseType) != null)
	        {
		        fieldInfo = type.GetField(fieldName, PrivateParentFlags);
		        if(fieldInfo != null)
			        break;
	        }

	        return fieldInfo == null ? null : fieldInfo;
        }
		
		private static BlueprintComponentsPatchOperation GetComponentElementValue(ValueHolder holder, string fieldName)
		{
			Type targetType = holder.TargetValue.GetType();
			var targetComponents = targetType.GetProperty("ComponentsArray",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (targetComponents == null)
			{
				Logger.Error($"Failed to get Components of {targetType}");
				return null;
			}

			var targetComponentsValue = targetComponents.GetValue(holder.TargetValue);
			if (targetComponentsValue == null)
			{
				Logger.Error($"Couldn't get Components value on {targetType}");
				return null;
			}

			BlueprintComponent component = null;
			var concreteTargetComponents = (BlueprintComponent[])targetComponentsValue;
			var targetComponentValue = concreteTargetComponents.Where(x => x.name == fieldName).ToList();
			if (targetComponentValue == null || !targetComponentValue.Any())
			{
				Logger.Log($"Component with name : {fieldName} removed in target.");
			}
			else
			{
				component = targetComponentValue[0];
				Logger.Log(component.ToString());
			}

			Type protoType = holder.ProtoValue.GetType();
			var protoComponents = protoType.GetProperty("ComponentsArray",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (protoComponents == null)
			{
				Logger.Error($"Failed to get Components of proto {protoType}");
				return null;
			}
			var protoComponentsValue = protoComponents.GetValue(holder.ProtoValue);
			
			//This means proto has no Components - it's a valid case
			if (protoComponentsValue == null && component != null)
			{
				Logger.Log($"Components value for proto is null on {protoType}");

				return CreateBlueprintComponentsPatchOperation(component);
			}

			var concreteProtoComponents = (BlueprintComponent[])protoComponentsValue;
			if (concreteProtoComponents == null )
			{
				Logger.Error($"Cast components to concrete type failed");
				return null;
			}
			
			//Target Component already exists in proto - skipping it
			if(concreteProtoComponents.Length != 0 
			   && concreteProtoComponents.
				   Any(x => x.name == fieldName))
			{
				if (component == null)
				{
					var protoComponent = concreteProtoComponents.FirstOrDefault(x => x.name == fieldName);
					return CreateBlueprintComponentsPatchOperation(protoComponent, true);
				}
				
				Logger.Log($"Component {fieldName} already exists in proto.");
				return null;
			}
			
			return CreateBlueprintComponentsPatchOperation(component);
		}

		private static BlueprintComponentsPatchOperation CreateBlueprintComponentsPatchOperation(BlueprintComponent component, bool remove = false)
		{
			if (component == null)
			{
				Logger.Error("Got null instead of BlueprintComponent");
				return null;
			}

			//It's a workaround to force Newtonsoft Json
			//serialize actual blueprintComponent's $type
			var patchData = new BlueprintComponentPatchData()
			{
				ComponentValue = component
			};
			string patchDataJson =
				JsonConvert.SerializeObject(patchData, Formatting.Indented, BlueprintPatcher.Settings);

			var operation = new BlueprintComponentsPatchOperation()
			{
				FieldName = "Components",
				FieldValue = patchDataJson,
				OperationType = remove == false ?
					BlueprintPatchOperationType.InsertLast :
					BlueprintPatchOperationType.RemoveElement
			};
			Logger.Error($"{patchDataJson}");
			return operation;	
		}

		private static bool GetProtoId(BlueprintScriptableObject blueprint)
		{
			var protoIdField = typeof(BlueprintScriptableObject).GetField("m_PrototypeId",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (protoIdField == null)
			{
				Logger.Error($"Couldn't get protoId field for {blueprint.name} | {blueprint.AssetGuid}");
				return false;
			}

			string protoId = protoIdField.GetValue(blueprint) as string;
			if (protoId.IsNullOrEmpty())
			{
				Logger.Error($"ProtoId for {blueprint.name} | {blueprint.AssetGuid} is empty." +
				             $" Can't create patch for bp without proto");
				return false;
			}
			
			s_ProtoId = protoId;
			return true;
		}

		private static bool GetOverrides(BlueprintScriptableObject blueprint)
		{
			var overridesField = typeof(BlueprintScriptableObject).GetField("m_Overrides",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (overridesField == null)
			{
				Logger.Error($"Couldn't get m_Overrides field for bp" +
				             $" {blueprint.name} | {blueprint.AssetGuid}");
				return false;
			}

			var overrides = overridesField.GetValue(blueprint) as List<string>;
			if (overrides == null || overrides.Count == 0)
			{
				Logger.Error($"Can't create patch for bp {blueprint.name} " +
				             $"| {blueprint.AssetGuid}. There are no overrides.");
				return false;
			}

			foreach (string @override in overrides)
			{
				Logger.Log($"Override detected: {@override}");
			}

			s_Overrides = overrides;
			return true;
		}
		
		private static void Reset()
		{
			s_ProtoBlueprint = null;
			s_Overrides = new List<string>();
			s_ProtoId = null;
			s_Target = null;
			s_FieldOverrides = new List<BlueprintFieldOverrideOperation>();
			s_ArrayPatches = new List<BlueprintSimpleArrayPatchOperation>();
			s_OverridenArrays = new List<ArrayOverrideInfo>();
		}
		
		public static void SavePatch(BlueprintScriptableObject blueprint)
		{
			Reset();
			
			if (blueprint == null || blueprint.AssetGuid.IsNullOrEmpty())
			{
				Logger.Error("Null given instead of blueprint");
				return;
			}

			s_Target = blueprint;
			SetTarget(blueprint);
			SavePatchInternal();
			GC.Collect();
		}

		private static void SavePatchInternal()
		{
			if (s_Target == null)
			{
				Logger.Error("Patch has no target");
				return;
			}

			if (s_ArrayPatches.Count == 0 
			    && s_FieldOverrides.Count == 0
			    && s_ComponentsPatches.Count == 0)
			{
				Logger.Error("Patch found no changes.");
				return;
			}

			if (s_ProtoId.IsNullOrEmpty())
			{
				if (s_ProtoId.IsNullOrEmpty())
				{
					Logger.Log("Blueprint Patch creating cancelled. Target has no proto.");
					return;
				}
			}

			var patch = new BlueprintPatch()
			{
				TargetGuid = s_ProtoId,
				FieldOverrides = s_FieldOverrides.ToArray(),
				ArrayPatches = s_ArrayPatches.ToArray(),
				ComponentsPatches = s_ComponentsPatches.ToArray()
			};

			var defaultPath = BlueprintsDatabase.GetAssetPath(s_ProtoBlueprint.Data);
			var suggestedFileName = Path.GetFileNameWithoutExtension(defaultPath);
			defaultPath = defaultPath.Replace($"{suggestedFileName}.jbp", "");
			var selectedPath = EditorUtility.
				OpenFolderPanel("Select Directory To Save",
					defaultPath,"");
			Logger.Log($"Selected path to save patch: {selectedPath}");
			var finalFilePath = $"{Path.Combine(selectedPath, suggestedFileName)}.jbp_patch";
			using (var fs = File.Open(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var sw = new StreamWriter(fs))
			{
				Serializer.Serialize(sw, patch);
				sw.Flush();
			}
			EditorUtility.RevealInFinder(finalFilePath);
		}
	}
}