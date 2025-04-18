using System;
using System.Linq;
using System.Reflection;
using Kingmaker.Blueprints;
using Kingmaker.Editor.Utility;
using UnityEditor;
using UnityEngine;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.Elements;
using Owlcat.QA.Validation;
using Kingmaker.ElementsSystem;
using UnityEngine.Profiling;
using Owlcat.Editor.Utility;
using System.Collections.Generic;
using Code.Editor.KnowledgeDatabase;
using Code.Editor.KnowledgeDatabase.Inspector;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.ElementsSystem.Interfaces;
using Kingmaker.Utility.Attributes;
using Owlcat.Runtime.Core.Utility;
using Kingmaker.Utility.DotNetExtensions;
using Kingmaker.Utility.UnityExtensions;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Blueprints
{
	public static class PrototypedObjectEditorUtility
	{
		private static string s_ArrayFilter;

		private static (SerializedProperty property, string description, Rect controlRect)? OvertipData;

		private abstract class Decorator
		{
			private class EmptyDecorator : Decorator
			{
				public override void DrawDecorated(Action drawAction)
				{
					drawAction.Invoke();
				}
			}

			public static readonly Decorator Empty = new EmptyDecorator();

			public abstract void DrawDecorated(Action drawAction);
		}

		private class ArrayElementDecorator : Decorator
		{
			private readonly SerializedProperty m_Array;
			private readonly int m_Index;

			public ArrayElementDecorator(SerializedProperty array, int index)
			{
				m_Array = array;
				m_Index = index;
			}

			public override void DrawDecorated(Action drawAction)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					using (new EditorGUILayout.VerticalScope())
						drawAction.Invoke();

					var blueprint = m_Array.serializedObject.targetObject as PrototypeableObjectBase;

					if (!blueprint || blueprint.PrototypeLink == null || blueprint.IsOverridden(m_Array))
					{
						if (GUILayout.Button("*", EditorStyles.miniButtonLeft, GUILayout.Width(16f)))
						{
							var rsp = new RobustSerializedProperty(m_Array); 
							
							GenericMenu menu = new GenericMenu();
							var canResize = CanResizeArrayProperty(m_Array);
							if (canResize)
							{
								menu.AddItem(new GUIContent("Remove"),false,() => { RemoveArrayElement(rsp, m_Index); });
								menu.AddItem(new GUIContent("Add before"),false,() => { AddArrayElement(rsp, m_Index); });
								menu.AddItem(new GUIContent("Add after"),false,() => { AddArrayElement(rsp, m_Index + 1); });
							}

							if (m_Array.propertyType == SerializedPropertyType.Generic)
							{
								if (canResize)
									menu.AddSeparator("");
								menu.AddItem(new GUIContent("Copy"), false, () => { CopyArrayElement(rsp, m_Index); });
								menu.AddItem(new GUIContent("Paste"), false, () => { PasteArrayElement(rsp, m_Index); });

							}
							menu.ShowAsContext();
						}

						GUILayout.Space(4f);
					}
				}
			}
		}

		static PrototypedObjectEditorUtility()
		{
			EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
		}

		public static bool IncludeChildren { get; set; } = true;

		public static BlueprintInspector RootInspector { get; set; }

		public static void DisplayProperties(SerializedObject serializedObject)
		{
			var p = serializedObject.GetIterator();
			
			if (!p.NextVisible(true))
				return;
			do
			{
				// nothing?
			} while (ShowPropertyRecursive(p, null, false));

			if (OvertipData!= null)
			{
				ShowDescriptionOvertip(OvertipData.Value.property, OvertipData.Value.description,
				                       OvertipData.Value.controlRect);
				OvertipData = null;
			}
			
			if (GUI.changed)
				serializedObject.ApplyModifiedProperties();
		}

		public static bool ShowPropertyRecursive(SerializedProperty p, bool asArrayElement = false)
		{
			if (!asArrayElement)
			{
				return ShowPropertyRecursive(p, null, false);
			}
			return ShowPropertyRecursive(p, new ArrayElementDecorator(p.GetParent(), p.GetIndexInParentArray()), true);
		}
		
		public static bool CanResizeArrayProperty(SerializedProperty property)
		{
			var propertyToFieldMatcher = PropertyToFieldMatcher
				.GetMatcher(property.serializedObject.targetObject);
			if (propertyToFieldMatcher == null)
				return true;

			var field = propertyToFieldMatcher
				.GetMatchingField(property);
			
			return field == null || !field.HasAttribute<DisableArrayResizingInEditorAttribute>();
		}

		private static bool ShowPropertyRecursive(SerializedProperty p, Decorator decorator, bool isArrayElement)
		{
			// skip rest of the tree for performance
			if (Event.current.type == EventType.Used
				&& !ElementsBaseDrawer.RecursiveFoldout)
				return p.NextVisible(false);

			//Profiler.BeginSample(p.propertyPath);

			if (!isArrayElement && !ConditionalAttributeExtension.IsVisible(p))
			{
				// do not draw this property
			}
			else if (p.isArray && (p.propertyType != SerializedPropertyType.String))
			{
				ShowArrayProperty(p, decorator);
			}
			else if (p.hasVisibleChildren && (p.propertyType == SerializedPropertyType.Generic || p.propertyType==SerializedPropertyType.ManagedReference))
			{
				bool expand = ShowPropertySingle(p, decorator);

				if (expand)
                {
                    return ShowPropertyChildren(p);
                }
			}
			else
			{
				ShowPropertySingle(p, decorator);
			}
			//Profiler.EndSample();

			return p.NextVisible(false);
		}

		public static bool ShowPropertyChildren(SerializedProperty p)
		{
			int d = p.depth;
			bool cont = p.NextVisible(true);
			{
				EditorGUI.indentLevel += 1;
				while (cont && p.depth > d)
				{
					cont = ShowPropertyRecursive(p, null, false);
				}

				EditorGUI.indentLevel -= 1;
			}

			return cont; // true if we found a prop after current, false if not
		}

		private static bool ShowPropertySingle(SerializedProperty p, Decorator decorator)
		{
			Profiler.BeginSample("ShowPropertySingle");
			bool result = false;
			IncludeChildren = false;
			try
			{
				result = ShowPropertySingleInternal(p, decorator);
			}
			catch (Exception e)
			{
				PFLog.Default.Exception(e);
			}
			IncludeChildren = true;
			Profiler.EndSample();
			return result;
		}

        private static bool ShowPropertySingleInternal(SerializedProperty p, Decorator decorator)
        {
	        decorator ??= Decorator.Empty;
	        bool isArray = p.isArray && p.hasVisibleChildren;
	        bool drawChildren = false;

			// todo: also detect blueprint references here??
			bool container = 
				p.type == "LocalizedString" ||
				p.type.StartsWith("Ak") && p.type.EndsWith("Reference");
			string controlId = p.serializedObject.targetObject.GetHashCode() + "_" + p.propertyPath;

			using (new EditorGUILayout.HorizontalScope())
			{
				bool overridden = false;
				bool hasOverrideOption = false;
				
				if (isArray || container || p.propertyType != SerializedPropertyType.Generic || !p.hasVisibleChildren)
				{
					overridden = IsOverridden(p);
					hasOverrideOption = HasOverrideOption(p);
					using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
					{
						decorator.DrawDecorated(
							() =>
							{
								using (new EditorGUI.DisabledGroupScope(!overridden && hasOverrideOption))
								{
									if (isArray)
									{
										string title = string.Format("{0} (size: {1})",
											p.displayName,
											p.hasMultipleDifferentValues ? "<multiple>" : p.arraySize.ToString());
										GUI.SetNextControlName(controlId);
										drawChildren = p.isExpanded = EditorGUILayout.Foldout(p.isExpanded,
											new GUIContent(title, p.GetTooltip()), false);
									}
									else
									{
										GUI.SetNextControlName(controlId);
										drawChildren = EditorGUILayout.PropertyField(p, false);
									}
								}
							});
					}
				}
				else
				{
					using (new EditorGUILayout.VerticalScope())
					{
						decorator.DrawDecorated(
							() =>
							{
								GUI.SetNextControlName(controlId);
								EditorGUILayout.PropertyField(p, false);
								drawChildren = p.isExpanded;
							});
					}
				}

				if (hasOverrideOption)
				{
					DrawOverrideButton(p, overridden);
				}

				if (p.type != "ActionList")
				{
					DrawDescriptionButton(p);
				}
			}

			if (GUI.GetNameOfFocusedControl() == controlId)
			{
				var type = SerializableTypesCollection.GetType(p);
				if (type != null)
				{
					CopyPasteController.Process(type, p);
				}
			}

	        return drawChildren;
        }

        private static GUIStyle s_TooltipStyle;
        private static GUIStyle s_MissingInfoButton;
		private static GUIStyle s_HasDescriptionInfoButton;
		private static GUIStyle s_HasLinkButton;
		private static GUIStyle s_HasAllInfoButton;
		
		public static void DrawPropertyWithDescriptionButton(SerializedProperty property)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(property);
				DrawDescriptionButton(property);
			}
		}
		
		public static void DrawDescriptionButton(SerializedProperty property)
		{
			// setup styles
			if (s_TooltipStyle == null)
			{
				if (GUI.depth <= 0)
					return;
				
				s_TooltipStyle = new GUIStyle(GUI.skin.box)
				{
					wordWrap = true,
					normal = {background = Texture2D.whiteTexture}, 
					richText = true,
					alignment = TextAnchor.UpperLeft,
					clipping = TextClipping.Overflow,
				};
				s_TooltipStyle.normal.textColor = Color.black;
                s_MissingInfoButton = new GUIStyle(GUI.skin.button) {normal = {textColor = Color.grey}};
				s_HasDescriptionInfoButton = new GUIStyle(GUI.skin.button) {normal = {textColor = new Color(0f,0.8f,0f)}};
				s_HasLinkButton = new GUIStyle(GUI.skin.button) {normal = {textColor = Color.blue}};
				s_HasAllInfoButton = new GUIStyle(GUI.skin.button) {normal = {textColor = new Color(0f,0.8f,0.8f)}};
			}

			(var type, string fieldName) = property.GetTypeAndName();
			string description = KnowledgeDatabaseSearch.GetDescription(type, fieldName);
			string codeDescription = KnowledgeDatabaseSearch.GetCodeDescription(type, fieldName);
			bool hasLink = KnowledgeDatabaseSearch.HasLink(type, fieldName);
           
			if (description == null && codeDescription == null && !hasLink)
				return;
			bool clicked;

			var buttonStyle = GetButtonStyle(description, codeDescription, hasLink, out string buttonText);
			float buttonWidth = 20f;
			using (new EditorGUILayout.VerticalScope(GUILayout.Width(buttonWidth)))
			{
				GUILayout.FlexibleSpace();
				clicked = GUILayout.Button(buttonText, buttonStyle, GUILayout.Width(buttonWidth));
			}

			if ((!description.IsNullOrEmpty() || !codeDescription.IsNullOrEmpty()) && Event.current.type == EventType.Repaint)
			{
				var controlRect = GUILayoutUtility.GetLastRect(); //new Rect(property.rectValue);
				bool isMouseOverProperty = controlRect.Contains(Event.current.mousePosition);
				if (isMouseOverProperty)
				{
					var fullDescription = "";
					if (!codeDescription.IsNullOrEmpty())
						fullDescription += "Programmer's description:\n\n" + codeDescription;
					if (!codeDescription.IsNullOrEmpty() && !description.IsNullOrEmpty())
						fullDescription += "\n\n";
					if (!description.IsNullOrEmpty())
                        fullDescription  += "Designer's description:\n\n" + description;

                    OvertipData = new (new RobustSerializedProperty(property), fullDescription, controlRect);
				}
			}

			// context menu
			if (clicked)
			{
				var contextMenu = new GenericMenu();
				OnPropertyContextMenu(contextMenu, property);
				contextMenu.ShowAsContext();
					
				Event.current.Use();
			}
		}

		private static GUIStyle GetButtonStyle(string description, string codeDescription, bool hasLink, out string buttonText)
		{
			buttonText = "?";
			if (description.IsNullOrEmpty() && codeDescription.IsNullOrEmpty() && !hasLink)
			{
				return s_MissingInfoButton;
			}
			
			if ((!description.IsNullOrEmpty() || !codeDescription.IsNullOrEmpty()) && !hasLink)
			{
				return s_HasDescriptionInfoButton;
			}
			
			if (description.IsNullOrEmpty() && codeDescription.IsNullOrEmpty() && hasLink)
			{
				buttonText = "L";
				return s_HasLinkButton;
			}
			
			return s_HasAllInfoButton;
		}

		private static void ShowDescriptionOvertip(SerializedProperty property, string description, Rect controlRect)
		{
			if (!description.IsNullOrEmpty() || !property.GetTooltip().IsNullOrEmpty())
			{
                string hardcodedTooltip = property.GetTooltip();

                string fullDescription = "";
				if (!description.IsNullOrEmpty())
					fullDescription = description;
				if (!description.IsNullOrEmpty() && !hardcodedTooltip.IsNullOrEmpty())
					fullDescription += "\n---\n";
				if (!hardcodedTooltip.IsNullOrEmpty())
					fullDescription += hardcodedTooltip;

				var tooltipContent = new GUIContent(fullDescription);
				float width = controlRect.y < 10f
					? EditorGUIUtility.labelWidth * 3f
					: EditorGUIUtility.labelWidth * 2f; //_____________
				float height = s_TooltipStyle.CalcHeight(tooltipContent, width);

				var tooltipRect = controlRect;
				float maybeCenterX = EditorGUIUtility.currentViewWidth * 0.5f - width * 0.5f;
				tooltipRect.x = maybeCenterX < 0 ? EditorGUIUtility.currentViewWidth * 0.2f : maybeCenterX;
				float maybeCenterY = controlRect.center.y - height * 0.5f;
				tooltipRect.y = EditorWindow.focusedWindow != null &&
				                maybeCenterY > EditorWindow.focusedWindow.position.yMax
					? controlRect.center.y - height
					: maybeCenterY < 0
						? controlRect.center.y
						: maybeCenterY;
				tooltipRect.height = height;
				tooltipRect.width = width;

				// .....yes
				GUI.Box(tooltipRect, GUIContent.none, s_TooltipStyle);
				GUI.Box(tooltipRect, GUIContent.none, s_TooltipStyle);
				GUI.Box(tooltipRect, tooltipContent, s_TooltipStyle);
			}
		}

		private static void ShowArrayProperty(SerializedProperty array, Decorator decorator)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				ShowPropertySingle(array, decorator);
			}

			if (!array.isExpanded)
				return;

			if (array.arraySize > 0)
			{
				var firstElement = array.GetArrayElementAtIndex(0);
				bool filterable = false;
				foreach (SerializedProperty item in firstElement)
				{
					if (item.propertyType == SerializedPropertyType.String)
					{
						filterable = true;
						break;
					}
				}

				if (filterable)
				{
					s_ArrayFilter = EditorGUILayout.TextField("Filter by strings", s_ArrayFilter);
				}
				else
				{
					s_ArrayFilter = string.Empty;
				}
			}

			using (new EditorGUILayout.VerticalScope())
			{
				EditorGUI.indentLevel += 1;
				bool itemsDisabled = !IsOverridden(array);
				using (new EditorGUI.DisabledScope(itemsDisabled))
				{
					//var size = array.arraySize; // The manual says this is the SMALLEST size in case of multiple arrays. The manual lies.
					//for (var index = 0; index < size; index++)
					foreach (var index in GetFilteredArrayIndices(array, s_ArrayFilter))
					{
						var p = array.GetArrayElementAtIndex(index);
						if (!p.propertyPath.StartsWith(array.propertyPath)) // if we get a property with a wrong path, we've reached the end of at least one array
						{
							break;
						}

						using (var scope = new EditorGUILayout.VerticalScope())
						{
							EditorGUI.DrawRect(
								scope.rect,
								index % 2 == 0 ? OwlcatEditorStyles.Instance.ArrayZebraColor1 : OwlcatEditorStyles.Instance.ArrayZebraColor2);

							ShowPropertyRecursive(p, new ArrayElementDecorator(array, index), true);
						}
					}

					if (IsOverridden(array))
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							const float step = 15;
							GUILayout.Space(EditorGUI.indentLevel * step);
							var text = PropertyToFieldMatcher.GetMatcher(array.serializedObject.targetObject).GetMatchingField(array)?.GetAttribute<AddElementButton>()?.Text;

							if ((CanResizeArrayProperty(array) || array.arraySize == 0)
							    && GUILayout.Button(text ?? "Add element", EditorStyles.miniButton))
							{
								AddArrayElement(array, array.arraySize);
							}
							GUILayout.FlexibleSpace();
						}
					}
				}
				EditorGUI.indentLevel -= 1;
			}
		}

		private static IEnumerable<int> GetFilteredArrayIndices(SerializedProperty array, string filterByName)
		{
			var size = array.arraySize;

			if (string.IsNullOrEmpty(filterByName))
			{
				for (int i = 0; i < size; i++)
				{
					yield return i;
				}
			}
			else
			{
				var filters = filterByName.ToLowerInvariant().Split(' ');

				for (int i = 0; i < size; i++)
				{
					var p = array.GetArrayElementAtIndex(i);
					foreach (var item in p)
					{
						if (p.propertyType == SerializedPropertyType.String)
						{
							var stringValue = p.stringValue.ToLowerInvariant();
							if (filters.All(f => stringValue.Contains(f)))
							{
								yield return i;
							}
						}
					}
				}	
			}
		}
		
		private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
		{
			var target = property.serializedObject.targetObject;
			if (target is not (BlueprintEditorWrapper or BlueprintComponentEditorWrapper or MonoBehaviour or ScriptableObject))
				return;
			
			bool hasOverrideOption = HasOverrideOption(property);
			var robustProperty = new RobustSerializedProperty(property);

			menu.AddSeparator(property.displayName);

			if (hasOverrideOption && !SerializedPropertyHelper.IsArrayElement(property))
			{
				menu.AddSeparator("");

				bool overridden = IsOverridden(property);
				menu.AddItem(
					new GUIContent(overridden ? "Revert" : "Override"),
					false,
					() => SetOverridden(!overridden, robustProperty.Property));
			}

			var type = SerializableTypesCollection.GetType(property) ?? FieldFromProperty.GetActualValueType(property);
			if (type != null)
			{
				menu.AddSeparator("");

				menu.AddItem(new GUIContent("Copy"),
					false,
					() => { CopyPasteController.CopyProperty(robustProperty.Property, null); });

				string pasteType = CopyPasteController.ClipboardElements.Count > 1
					? CopyPasteController.ClipboardElements.Count + " items"
					: CopyPasteController.ClipboardElements.FirstItem()?.Type.Name;
				if (pasteType != null)
				{
					pasteType = $" ({pasteType})";
				}

				if (CopyPasteController.IsSuitableForPaste(type))
				{
					menu.AddItem(new GUIContent("Paste" + pasteType),
						false,
						() => { CopyPasteController.Paste(type, robustProperty.Property); });
				}
				else
				{
					menu.AddDisabledItem(new GUIContent("Paste" + pasteType));
				}

				if (Application.isPlaying)
				{
					if (type.IsSubclassOf(typeof(Condition)))
					{
						menu.AddItem(new GUIContent("Evaluate"),
							false,
							() =>
							{
								DebugCondition(FieldFromProperty.GetFieldValue(robustProperty.Property) as Condition);
							});
					}

					if (type.GetInterfaces().FirstOrDefault(v
						    => v.IsGenericType && v.GetGenericTypeDefinition() == typeof(IEvaluator<>)) is {} iFace)
					{
						menu.AddItem(new GUIContent("Evaluate"),
							false,
							() =>
							{
								DebugEvaluator(iFace,
									FieldFromProperty.GetFieldValue(robustProperty.Property) as Element);
							});
					}
				}

			}

			(var kdbType, string kdbFieldName) = property.GetTypeAndName();
			if (KnowledgeDatabaseSearch.GetDescription(kdbType, kdbFieldName) is {} || KnowledgeDatabaseSearch.GetCodeDescription(kdbType, kdbFieldName) is { })
			{
				menu.AddItem(new GUIContent("Description"),
					false,
					() => KnowledgeDatabaseEditWindow.Show(robustProperty.Property));
			}

			if (KnowledgeDatabaseSearch.HasLink(kdbType, kdbFieldName))
			{
				menu.AddItem(new GUIContent("Link"),
					false,
					() => KnowledgeDatabaseSearch.GoTo(KnowledgeDatabaseSearch.GetLink(kdbType, kdbFieldName)));
			}
		}

		public static void DebugCondition(Condition c)
		{
			try
			{
				var result = c?.Check();
				EditorUtility.DisplayDialog(c?.GetCaption(), "Result: " + result, "OK");
			}
			catch (Exception x)
			{
				PFLog.Default.Exception(x);
				EditorUtility.DisplayDialog(c?.GetCaption(), "Error: " + x, "OK");
			}
		}
		public static void DebugEvaluator(Type iFace, Element e)
		{
			var method = MethodBase.GetCurrentMethod().DeclaringType?.GetMethod(nameof(DebugEvaluatorImpl), BindingFlags.NonPublic | BindingFlags.Static);
			if (method == null)
				throw new InvalidOperationException("Debug evaluator internal error: cant find method");
			var arg = iFace.GetGenericArguments()[0];
			var substituted = method.MakeGenericMethod(arg);
			var del = (Action<Element>)substituted.CreateDelegate(typeof(Action<Element>));
			del(e);
		}
		
		private static void DebugEvaluatorImpl<T>(Element e)
		{
			try
			{
				var iFace = (IEvaluator<T>)e;

				iFace.TryGetValue(out var res);

				EditorUtility.DisplayDialog(e?.GetCaption(), "Result: " + (res?.ToString() ?? "NULL"), "OK");
			}
			catch (Exception x)
			{
				PFLog.Default.Exception(x);
				EditorUtility.DisplayDialog(e?.GetCaption(), "Error: " + x, "OK");
			}
		}



		public static Type GetFieldType(RobustSerializedProperty property)
		{
			var field = PropertyToFieldMatcher.GetMatcher(property.serializedObject.targetObject).GetMatchingField(property);
			var type = GetElementType(field);
			return type;
		}

		public static void AddArrayElement(RobustSerializedProperty array, int index, Type newElementType = null)
		{
			if (array?.Property == null || !array.Property.isArray)
			{
				Debug.LogErrorFormat(array?.targetObject, "'{0}' is not an array", array?.Path);
				return;
			}
			
			array.serializedObject.Update();
			array.Property.InsertArrayElementAtIndex(index);
			PrepareAddedArrayElement(array.Property.GetArrayElementAtIndex(index), newElementType);
			array.serializedObject.ApplyModifiedProperties();
		}

		public static void CopyArrayElement(RobustSerializedProperty array, int index)
		{
			if (array?.Property == null || !array.Property.isArray)
			{
				Debug.LogErrorFormat(array?.targetObject, "'{0}' is not an array", array?.Path);
				return;
			}
			
			array.serializedObject.Update();
			var p = array.Property.GetArrayElementAtIndex(index);
			var s = SerializedPropertySerializer.Serialize(p);
			//			UberDebug.Log(s);
			GUIUtility.systemCopyBuffer = s;
		}
		public static void PasteArrayElement(RobustSerializedProperty array, int index)
		{
			if (array?.Property == null || !array.Property.isArray)
			{
				Debug.LogErrorFormat(array?.targetObject, "'{0}' is not an array", array?.Path);
				return;
			}
			
			array.serializedObject.Update();
			var p = array.Property.GetArrayElementAtIndex(index);
			//			UberDebug.Log(GUIUtility.systemCopyBuffer);
			SerializedPropertySerializer.Deserialize(p, GUIUtility.systemCopyBuffer);
			if (p.boxedValue is Element && 
			    p.propertyType == SerializedPropertyType.ManagedReference)
				p.FindPropertyRelative("name").stringValue = Element.GenerateName(p.managedReferenceValue.GetType().Name);
			
			//SanitizeElementsLinks(p);
			array.serializedObject.ApplyModifiedProperties();
		}

		public static void MoveArrayElement(RobustSerializedProperty array, int indexOld, int indexNew)
		{
			if (array?.Property == null || !array.Property.isArray)
			{
				Debug.LogErrorFormat(array?.targetObject, "'{0}' is not an array", array?.Path);
				return;
			}

			array.serializedObject.Update();
			array.Property.MoveArrayElement(indexOld, indexNew);
			array.serializedObject.ApplyModifiedProperties();
		}

		public static Type GetElementType(this FieldInfo f)
		{
			if (f == null)
				return typeof(Object);
			var ft = f.FieldType;
			if (ft.IsArray)
				return ft.GetElementType();
			if (ft.IsGenericType)
				return ft.GetGenericArguments()[0];
			return ft;
		}

		private static void PrepareAddedArrayElement(SerializedProperty p, Type newElementType = null)
		{
			p = p.Copy();
			// search for array initializer method in property's type and call it
			if (p.propertyType == SerializedPropertyType.Generic)
			{
				var field = PropertyToFieldMatcher.GetMatcher(p.serializedObject.targetObject).GetMatchingField(p);
				var type = GetElementType(field);
				var initializer = type?.GetMethod("InitializeArrayElement", BindingFlags.Static | BindingFlags.Public);
				if (initializer != null)
				{
					initializer.Invoke(null, new object[] { p });
				}
			}
			else if (p.propertyType == SerializedPropertyType.ObjectReference)
			{
				var field = PropertyToFieldMatcher.GetMatcher(p.serializedObject.targetObject).GetMatchingField(p);
				var type = GetElementType(field);
				if (type != null && !type.IsSubclassOf(typeof(Element)) && !field.HasAttribute<NoAutoPickerAttribute>())
				{
					var p1 = new RobustSerializedProperty(p);
                    AssetPicker.ShowAssetPicker(
	                    
					type,
					field,
					o =>
					{
						AssetValidator.CancelIgnoreValidation();
						p1.serializedObject.Update();
						p1.Property.objectReferenceValue = o;
						p1.serializedObject.ApplyModifiedProperties();
						
					},
					p.objectReferenceValue);
					AssetValidator.StartIgnoreValidation();
				}
			} else if (p.propertyType == SerializedPropertyType.ManagedReference)
			{
				if (newElementType != null)
				{
					p.managedReferenceValue = Activator.CreateInstance(newElementType);
				}
				else
				{
					var field = PropertyToFieldMatcher.GetMatcher(p.serializedObject.targetObject).GetMatchingField(p);
					var type = GetElementType(field);
					p.managedReferenceValue = Activator.CreateInstance(type);
				}
			}

			SanitizeElementsLinks(p);
		}

		private static void SanitizeElementsLinks(SerializedProperty p)
		{
			// remove links to all Element objects from new array element.
			// this is needed b/c when adding array elements they may be cloned and retain references to elements from 
			// another array element, which is always a _bug.
            // also, when adding an array element containing managed references, all the references magically become the containing object itself, 
            // even if the type does not match, creating an infinite reference loops and breaking everything
            var d = p.depth;
            do
            {
                if (p.propertyType == SerializedPropertyType.ManagedReference)
                {
                    var declaredType = FieldFromProperty.GetDeclaredType(p);
                    if (declaredType.IsOrSubclassOf<Element>() || !declaredType.IsAssignableFrom(FieldFromProperty.GetActualValueType(p)))
                    {
                        p.managedReferenceValue = null;
                    }
                }
                else if(p.isArray && p.arraySize > 0 && p.propertyType==SerializedPropertyType.Generic && p.arrayElementType == "managedReference<>")
                {
                    // also clear arrays of elements too
					var declaredType = FieldFromProperty.GetDeclaredType(p.GetArrayElementAtIndex(0));
                    if (declaredType?.IsOrSubclassOf<Element>() ?? false)
                    {
                        p.arraySize = 0;
                    }
				}
                    
            } while (p.Next(true) && p.depth > d);

			// probably NOT needed with new elements, but may still be useful
		}

		public static void RemoveArrayElement(RobustSerializedProperty array, int index)
		{
			if (array?.Property == null || !array.Property.isArray)
			{
				Debug.LogErrorFormat(array?.targetObject, "'{0}' is not an array", array?.Path);
				return;
			}
			
			array.serializedObject.Update();

			int prevSize = array.Property.arraySize;
			array.Property.DeleteArrayElementAtIndex(index);
			if (prevSize == array.Property.arraySize)
			{
				array.Property.DeleteArrayElementAtIndex(index);
			}

			array.serializedObject.ApplyModifiedProperties();
		}

		private static void DrawOverrideButton(SerializedProperty property, bool overridden)
		{
			var gc = new GUIContent("");
			//if (!overridden)
			//{
			//	var blueprint = property.serializedObject.targetObject as PrototypeableObjectBase;
			//	var vs = blueprint?.GetValueSource(property);
			//	if (vs is BlueprintComponent)
			//		vs = (PrototypeableObjectBase)AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(vs));
			//	gc.tooltip = "Defined in: " + vs.NameSafe();
			//}

			bool pressed;
			using (var s = new EditorGUILayout.VerticalScope(GUILayout.Width(16f)))
			{
				GUILayout.FlexibleSpace();
				var position = s.rect;
				position.y += position.height - 14f;
				position.height = 14f;
				using (new EditorGUI.DisabledScope(SerializedPropertyHelper.IsArrayElement(property)))
				{
					pressed = GUI.Button(
						position,
						gc,
						overridden ? OwlcatEditorStyles.Instance.RevertButton : OwlcatEditorStyles.Instance.OverrideButton);
				}
			}

			if (pressed)
			{
				var p = new RobustSerializedProperty(property);
				var menu = new GenericMenu();
				menu.AddItem(
					new GUIContent(overridden ? "Revert" : "Override"),
					false,
					() => SetOverridden(!overridden, p));
				menu.ShowAsContext();
			}
		}

		private static bool IsOverridden(SerializedProperty p)
		{
            if(p.serializedObject.targetObject is ScriptableWrapperBase sb)
            {
                return sb.PrototypeableInstance?.IsOverridden(p) ?? true;
            }
            
			if (!(p.serializedObject.targetObject is PrototypeableObjectBase))
				return true;
			// property is NOT overridden if at least one blueprint does not override
			for (int ii = 0; ii < p.serializedObject.targetObjects.Length; ii++)
			{
				var targetObject = (PrototypeableObjectBase)p.serializedObject.targetObjects[ii];
				if (targetObject.PrototypeLink && !targetObject.IsOverridden(p))
					return false;
			}
			return true;
		}

		private static bool HasOverrideOption(SerializedProperty p)
		{
            if (p.serializedObject.targetObject is ScriptableWrapperBase sb)
            {
                return sb.PrototypeableInstance?.PrototypeLink != null && sb.PrototypeableInstance.IsOverridable(p);
            }

			if (!(p.serializedObject.targetObject is PrototypeableObjectBase))
				return false;
			// property is overridable if all blueprints allow override
			for (int ii = 0; ii < p.serializedObject.targetObjects.Length; ii++)
			{
				var targetObject = (PrototypeableObjectBase)p.serializedObject.targetObjects[ii];
				if (!targetObject.PrototypeLink || !targetObject.IsOverridable(p))
					return false;
			}
			return true;
		}

		private static void SetOverridden(bool overridden, SerializedProperty p)
		{
			if (p.serializedObject.targetObject is ScriptableWrapperBase sb)
            {
                if (sb.PrototypeableInstance != null)
                {
                    sb.PrototypeableInstance.SetOverridden(p, overridden);
                    sb.SyncPropertiesWithProto();
                    return;
                }
            }


			for (int ii = 0; ii < p.serializedObject.targetObjects.Length; ii++)
			{
				var blueprint = (PrototypeableObjectBase)p.serializedObject.targetObjects[ii];
				if (blueprint.IsOverridden(p) == overridden)
					continue;

				blueprint.SetOverridden(p, overridden);
				if (overridden)
				{
					blueprint.CopyOverridesFromProto();
                    // todo: [bp] fix this once prototypes are a thing again
                }

			}

			EditorApplication.delayCall += () => p.serializedObject.Update();
		}

		
		
	}

	internal static class SerializedPropertyHelper
	{
		public static bool IsArrayElement(this SerializedProperty property) 
			=> property.propertyPath.Contains("Array");
	}
}
