using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Base;
using Owlcat.QA.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Runtime.Core.Utility;


namespace Kingmaker.Editor.UIElements.Custom
{
	public class ComponentsGroup : OwlcatVisualElement
	{
		private readonly SerializedObject m_SerializedObject;

		private readonly VisualElement m_ComponentsContainer;

        public BlueprintScriptableObject Blueprint
            => BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(m_SerializedObject.targetObject);

		public IEnumerable<SerializedProperty> ComponentsSerializedProperties
		{
			get
			{
				var componentsArrayProperty = m_SerializedObject.FindProperty("Components");
				for (int i = 0; i < componentsArrayProperty.arraySize; ++i)
				{
					yield return componentsArrayProperty.GetArrayElementAtIndex(i);
				}
			}
		}

		#region Constructor

		public ComponentsGroup(SerializedObject serializedObject)
		{
			m_SerializedObject = serializedObject;
			name = "Components";

            if (!Blueprint)
            {
                throw new Exception(
                    $"{nameof(ComponentsGroup)}(): blueprint is missing");
            }

			m_ComponentsContainer = new VisualElement 
			{
				name = "ComponentsContainer", 
				style = {flexDirection = FlexDirection.Column}
			};
			Add(m_ComponentsContainer);

            UpdateComponents();
            AddRestoreButtons();
            AddButtons();
        }

		public void UpdateComponents()
		{
			var duplicateIndices = GetDuplicateIndices(Blueprint);
			
			foreach (var component in m_ComponentsContainer.Children().OfType<ComponentElement>().ToArray())
			{
				if (component.Component!=null && Blueprint.ComponentsArray.HasItem(component.Component))
				{
					//continue;
				}
				
				m_ComponentsContainer.Remove(component);
			}

            for (var ii = 0; ii < Blueprint.ComponentsArray.Length; ii++)
            {
                var component = Blueprint.ComponentsArray[ii];
                if (m_ComponentsContainer.Children().OfType<ComponentElement>().Any(i => i.Component == component))
                {
                    //continue;
                }

                var element = new ComponentElement(component, m_SerializedObject, ii);
                if(duplicateIndices.Contains(ii))
					element.style.backgroundColor = new StyleColor(new Color(0.75f, 0.0f, 0.0f, 1.0f));
                m_ComponentsContainer.Add(element);

                element.OnMoveDownEvent += ItemMoveDown;
                element.OnMoveUpEvent += ItemMoveUp;
                element.OnRemoveEvent += ItemRemove;
                element.OnCopyEvent += ItemCopy;
            }

            int index = 0;
			foreach (var component in Blueprint.ComponentsArray)
			{
				var element = m_ComponentsContainer.Children()
					.OfType<ComponentElement>()
					.FirstOrDefault(e => e.Component == component);
				m_ComponentsContainer.Remove(element);

				if (index < m_ComponentsContainer.childCount)
				{
					m_ComponentsContainer.Insert(index, element);
				}
				else
				{
					m_ComponentsContainer.Add(element);
				}

				index++;
			}
			
			// m_ComponentsContainer.Sort(
			// 	(e1, e2) =>
			// 	{
			// 		var c1 = e1 as ComponentElement;
			// 		var c2 = e2 as ComponentElement;
			// 		if (c1 == null && c2 == null)
			// 			return 0;
			// 		if (c1 != null && c2 == null)
			// 			return 1;
			// 		if (c1 == null)
			// 			return -1;
			//
			// 		int i1 = Blueprint.ComponentsArray.FindIndex(i => i == c1.Component);
			// 		int i2 = Blueprint.ComponentsArray.FindIndex(i => i == c2.Component);
			// 		return i1.CompareTo(i2);
			// 	});
		}

		private static IReadOnlyCollection<int> GetDuplicateIndices(BlueprintScriptableObject objectToValidate)
		{
			var indexed = objectToValidate.ComponentsArray.Select((v, i) => (v, i));
			var duplicates = from component in indexed
				let compIndex = component.i
				where component.v
				let type = component.v.GetType()
				where !type.HasAttribute<AllowMultipleComponentsAttribute>()
				group compIndex by type
				into byType
				where byType.Skip(1).Any()
				select byType;

			var duplicateIndices = duplicates
				.SelectMany(v => v)
				.Distinct()
				.ToHashSet();
			return duplicateIndices;
		}
		private void AddRestoreButtons()
		{
			var proto = Blueprint.PrototypeLink as BlueprintScriptableObject;
			if (proto != null)
			{
				foreach (var component in proto.ComponentsArray)
				{
                    if (Blueprint.IsOverridden(component.name) && Blueprint.ComponentsArray.All(c => c.PrototypeLink != component))
                    {
                        AddRestoreButton(component);
                    } 
                }
			}
		}

		private void AddRestoreButton(BlueprintComponent component)
		{
			var compName = component.name;
			var root = new VisualElement();
			root.AddToClassList("owlcat-box");
			root.AddToClassList("labelPart");
			var label = new Label($"Removed {ClassNames.GetObjectNameNoPrefix(component)}") 
			{ style = { unityTextAlign = TextAnchor.MiddleLeft } };
			var button = new Button() { text = "Restore" };
			button.clicked += () =>
			{
                Blueprint.SetOverridden(compName, false);
                ((BlueprintEditorWrapper)m_SerializedObject.targetObject).SyncPropertiesWithProto();
                Remove(root);
				UpdateComponents();
			};

			root.Add(label);
			root.Add(button);
			Add(root);
		}

		private void AddButtons()
		{
			if (!Blueprint.CanAddComponents())
				return;

			var btnRoot = new VisualElement()
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					alignContent = Align.Center,
					justifyContent = Justify.Center
				}
			};

			var addBtn = TypePicker.CreatePickerButton("Add Component", Blueprint.GetValidComponentTypes,
				type =>
				{
					Blueprint.AddComponentFromMenu(type);
					m_SerializedObject.Update();
					UpdateComponents();
				});

			var pasteBtn = new Button() { text = "Paste", style = { marginTop = new StyleLength(4), marginBottom = new StyleLength(4)} };
			pasteBtn.SetEnabled(CopyPasteController.HasBlueprintComponent);
			CopyPasteController.ClipboardElementsChangedEvent += () =>
			{
				pasteBtn.SetEnabled(CopyPasteController.HasBlueprintComponent);
			};
			
			pasteBtn.clicked += () =>
			{
				if (CopyPasteController.HasBlueprintComponent)
				{
					var componentsArray = m_SerializedObject.FindProperty("Blueprint.Components");
					var cc = componentsArray.arraySize;
					CopyPasteController.PasteProperty(typeof(BlueprintComponent), componentsArray);

					componentsArray.serializedObject.ApplyModifiedProperties();
					// ugly hack: add override markers to any components that were pasted. Must do this after
					// ApplyModifiedProperties because the override manager cannot work in serialized world
					for (int ii = cc; ii < componentsArray.arraySize; ii++)
					{
                        Blueprint.SetOverridden(Blueprint.ComponentsArray[ii].name, true); 
                    }
					m_SerializedObject.Update();
					Blueprint.SetDirty();
					UpdateComponents();
				}
			};

			btnRoot.Add(addBtn);
			btnRoot.Add(pasteBtn);
			Add(btnRoot);
		}

		#endregion Constructor
			
		#region Methods

		private void ItemMoveUp(ComponentElement element)
		{
			int index = Blueprint.ComponentsArray.IndexOf(element.Component);
			if (index <= 0)
			{
				return;
			}

			var old = Blueprint.ComponentsArray[index - 1];
			Blueprint.ComponentsArray[index - 1] = element.Component;
			Blueprint.ComponentsArray[index] = old;
			m_SerializedObject.Update();
			UpdateComponents();
			
			// var oldIndex = hierarchy.IndexOf(element);
			// hierarchy.RemoveAt(oldIndex);
			// hierarchy.Insert(index - 1, element);
		}

		private void ItemMoveDown(ComponentElement element)
		{
			int index = Blueprint.ComponentsArray.IndexOf(element.Component);
			if (index < 0 || index > Blueprint.ComponentsArray.Length - 2)
			{
				return;
			}

			var old = Blueprint.ComponentsArray[index + 1];
			Blueprint.ComponentsArray[index + 1] = element.Component;
			Blueprint.ComponentsArray[index] = old;
			m_SerializedObject.Update();
			UpdateComponents();
			
			// var oldIndex = hierarchy.IndexOf(element);
			// hierarchy.RemoveAt(oldIndex);
			// hierarchy.Insert(index + 1, element);
		}

		private void ItemRemove(ComponentElement element)
		{
			element.OnMoveDownEvent -= ItemMoveDown;
			element.OnMoveUpEvent -= ItemMoveUp;
			element.OnRemoveEvent -= ItemRemove;
			element.OnCopyEvent -= ItemCopy;

			var index = Blueprint.ComponentsArray.IndexOf(element.Component);
			if (index != -1)
			{
				var component = Blueprint.ComponentsArray[index];
                if (component.PrototypeLink!=null)
                {
                    // sometimes components can be null, maybe the object creation glitched or something
                    Blueprint.SetOverridden(component.PrototypeLink.name, true);
                    AddRestoreButton(component.PrototypeLink as BlueprintComponent);
                }

                Blueprint.ComponentsArray = Blueprint.ComponentsArray.Where(c => c != component).ToArray();
                Blueprint.SetDirty();
                Blueprint.Cleanup();
				
				m_ComponentsContainer.Remove(element);
				m_SerializedObject.Update();
			}
		}

		private void ItemCopy(ComponentElement element)
		{
			var arrayProp = m_SerializedObject.FindProperty("Blueprint.Components");
			int index = Blueprint.ComponentsArray.IndexOf(element.Component);
			CopyPasteController.CopyProperty(arrayProp.GetArrayElementAtIndex(index), null);
		}

		#endregion Methods
	}
}