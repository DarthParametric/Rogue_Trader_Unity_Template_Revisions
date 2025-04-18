using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.Blueprints.Base;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Elements;
using Kingmaker.Editor.UIElements.Custom.Array;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatArrayProperty : OwlcatProperty, IBindable
	{
		private readonly ArrayElementMenu m_Menu;
		
		[CanBeNull]
		private TextField m_Filter;

		private readonly string m_Title;

		private readonly IEnumerable<Type> m_ValidTypes;

		public IBinding binding { get; set; }

		public string bindingPath { get; set; }

		public bool ContentWasBuilt { get; private set; }

		public readonly VisualElement ArrayHeaderContainer;
		public readonly VisualElement ArrayButtonsContainer;

		public event Action ElementAdded;

		public OwlcatArrayProperty(SerializedProperty prop, string title = null) 
			: base(prop, Layout.Vertical, true, true)
		{
			if (!prop.isArray)
				throw new Exception("Not valid property type for ArrayElement");

			m_Menu = new ArrayElementMenu(prop, RebuildContent);
			m_Title = string.IsNullOrEmpty(title) ? Property.displayName : title;
			TitleLabel.text = $"{m_Title} (size: {Property.arraySize})";
						
			ArrayHeaderContainer = new VisualElement { name = "header" };
            ArrayHeaderContainer.AddToClassList("owlcat-property-header");
            ArrayHeaderContainer.style.display = DisplayStyle.None;
            ArrayButtonsContainer = new VisualElement() { style = { justifyContent = Justify.FlexStart, flexDirection = FlexDirection.Row } };
            SetupButtons();
            IsExpanded = GetSavedExpandedState();
		}
		
		protected override void LoadSavedExpandedState()
		{}

		public OwlcatArrayProperty(SerializedProperty prop, IEnumerable<Type> types, string title = null) : this(prop, title)
		{
			m_ValidTypes = types;
		}

		protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
		{
			base.OnAttachToPanelInternal(evt);
			bindingPath = PropertyPath;
			binding = new OwlcatArrayBinding(this);
		}

		protected override void OnIsExpandedChanged()
		{
			base.OnIsExpandedChanged();
			if (IsExpanded && !ContentWasBuilt)
			{
				RebuildContent();
			}
		}

		public void RebuildContent()
		{
			using (UIElementsUtility.InitializationProcessFlag.Require())
			{
				ContentWasBuilt = true;

				var focusedProperty = focusController.GetFocusedProperty();
				bool isFocusedPropertyInsideArray = ContentContainer.Contains(focusedProperty);
				string prevPropertyPath = Root?.GetPrevProperty()?.PropertyPath;
				string propertyPath = focusedProperty?.PropertyPath;

				TitleLabel.text = $"{m_Title} (size: {Property.arraySize})";

				ContentContainer.Clear();

				SetupFilterablePart();

				ContentContainer.Add(ArrayHeaderContainer);

				for (int i = 0; i < Property.arraySize; i++)
				{
					var child = Property.GetArrayElementAtIndex(i);

					var item = UIElementsUtility.CreatePropertyElement(child, true);
					var arrayElementComponent = new ArrayElementComponent(i, m_Menu);
					item.AddComponent(arrayElementComponent);
					item.AddComponent(this.TryCreateElementCustomTitleProvider(item, arrayElementComponent));

					ContentContainer.Add(item);
				}

				if (isFocusedPropertyInsideArray)
				{
					RestoreFocus(prevPropertyPath, propertyPath);
				}

				ContentContainer.Add(ArrayButtonsContainer);

				UpdateFilter();
			}
			
			//ContentContainer.Bind(Property.serializedObject);
		}

		private void SetupButtons()
        {
			var addBtn = new Button { text = "Add Element" };
            addBtn.AddToClassList("middleButton");
            addBtn.style.borderBottomLeftRadius = 10;
            addBtn.style.borderBottomRightRadius = 10;
            addBtn.style.paddingLeft = 10;
            addBtn.style.paddingRight = 10;
            addBtn.style.marginLeft = 0;
            addBtn.clicked += () =>
            {
                if (m_ValidTypes == null)
                {
                    PrototypedObjectEditorUtility.AddArrayElement(Property, Property.arraySize);
                    ElementAdded?.Invoke();
                    RebuildContent();
                }
                else
                {
	                if (m_ValidTypes.Count() == 1)
	                {
		                OnTypeSelected(m_ValidTypes.First());
		                return;
	                }

	                TypePicker.ShowPickerWindow(
                        addBtn,
                        "Add Element",
                        () => m_ValidTypes,
                        selectedType => { OnTypeSelected(selectedType); }
                    );
                }

            };

            ArrayButtonsContainer.Add(addBtn);
		}

		private void OnTypeSelected(Type selectedType)
		{
			//means its just regular scriptable
			if (Property.serializedObject.targetObject is not ScriptableWrapperBase)
			{
				PrototypedObjectEditorUtility.AddArrayElement(Property, Property.arraySize, selectedType);
			}
			else
			{
				TypeUtility.AddElementFromMenu(Property, selectedType);
			}
	                        
			ElementAdded?.Invoke();
			RebuildContent();
		}

		private void SetupFilterablePart()
		{
			if (Property.arraySize <= 0)
			{
				return;
			}

			var firstElement = Property.GetArrayElementAtIndex(0);
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
				string filterString = m_Filter?.text;
					
				var filterContainer = new OwlcatPropertyLayout(Layout.Horizontal, false);
				filterContainer.TitleLabel.text = "Filter by strings";
				ContentContainer.Add(filterContainer);
					
				m_Filter = new OwlcatTextField();
				if (filterString != null)
				{
					m_Filter.SetValueWithoutNotify(filterString);
				}
				m_Filter.RegisterValueChangedCallback(evt => UpdateFilter());
				filterContainer.ContentContainer.Add(m_Filter);
			}
		}

		private void UpdateFilter()
		{
			if (m_Filter == null)
			{
				return;
			}

			int size = Property.arraySize;
			if (size < 1)
			{
				return;
			}

			if (string.IsNullOrEmpty(m_Filter.value))
			{
				foreach (var child in ContentContainer.Children())
				{
					if (child is OwlcatProperty)
					{
						child.style.display = DisplayStyle.Flex;
					}
				}
			}
			else
			{
				var filters = m_Filter.text.ToLowerInvariant().Split(' ');
				foreach (var child in ContentContainer.Children())
				{
					var p = child as OwlcatProperty;
					if (p == null)
					{
						continue;
					}

					bool isVisible = false;
					var it = p.Property.Copy();
					it.NextVisible(true);
					do
					{
						if (!it.propertyPath.StartsWith(PropertyPath))
						{
							break;
						}

						if (it.propertyType == SerializedPropertyType.String)
						{
							string value = it.stringValue.ToLower();
							if (filters.All(f => value.Contains(f)))
							{
								isVisible = true;
								break;
							}
						}
					} while (it.NextVisible(false));

					p.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
				}
			}
		}

		private void RestoreFocus(string prevPropertyPath, string propertyPath)
		{
			foreach (var p in this.GetAllProperties())
			{
				if (p.Property.propertyPath == propertyPath)
				{
					p.Focus();
					return;
				}
			}

			foreach (var p in this.GetAllProperties())
			{
				if (p.Property.propertyPath == prevPropertyPath)
				{
					p.Focus();
					return;
				}
			}
			
			Focus();
		}
	}

	public class OwlcatArrayBinding : IBinding
	{
		private readonly OwlcatArrayProperty m_Array;
		private bool m_IsOverridden;
		private int m_lastCount;

		public OwlcatArrayBinding(OwlcatArrayProperty array)
		{
			m_Array = array;
			m_IsOverridden = array.IsOverridden;
			m_lastCount = array.Property.arraySize;
		}

		public void PreUpdate()
		{
		}

		public void Update()
		{
			if (!m_Array.ContentWasBuilt)
			{
				return;
			}

			if (m_lastCount != m_Array.Property.arraySize)
			{
				m_lastCount = m_Array.Property.arraySize;
				m_Array.RebuildContent();
			}

			foreach (var child in m_Array.ContentContainer.Children())
			{
				(child as OwlcatProperty)?.UpdateTitle();
			}

			if (m_IsOverridden != m_Array.IsOverridden)
			{
				m_IsOverridden = m_Array.IsOverridden;
				m_Array.RebuildContent();
			}
		}

		public void Release()
		{
		}
	}
}
