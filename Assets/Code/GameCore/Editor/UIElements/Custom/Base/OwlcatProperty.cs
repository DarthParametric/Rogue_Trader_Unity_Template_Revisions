using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Prototypable;
using Kingmaker.Editor.Utility;
using System;
using System.Collections.Generic;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Owlcat.Runtime.Core.Utility;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatProperty : OwlcatPropertyLayout
	{
		[CanBeNull]
		public Attribute[] Attributes { get; set; }

		// hack for highlighting of custom/IMGUI properties 
		private bool m_FocusedDirectly;

		private StyleColor m_PrevBackgroundColor;

		private readonly OverridablePropertyControl m_OverridableControl;

		public RobustSerializedProperty RobustProperty { get; }

		public SerializedProperty Property
			=> RobustProperty.Property;

		public string PropertyPath
			=> RobustProperty.Path;

		public override bool canGrabFocus
			=> true;

		public bool IsOverridden
			=> m_OverridableControl?.IsOverridden ?? false;

		[CanBeNull]
		private IOwlcatPropertyTitleProvider m_TitleProvider;

		private readonly List<IOwlcatPropertyInputHandler> m_InputHandlers 
			= new List<IOwlcatPropertyInputHandler>();

		private readonly Dictionary<Type, IOwlcatPropertyComponent> m_Components 
			= new Dictionary<Type, IOwlcatPropertyComponent>();

		protected OwlcatProperty(SerializedProperty property, Layout layout, bool expandable, bool overridable) : base(layout, expandable)
		{
			name = property.propertyPath;
			focusable = true;
			RobustProperty = new RobustSerializedProperty(property.Copy());
			UpdateTitle();

			AddToClassList("owlcat-property");

			var info = property.GetFieldInfo();
			var nonOverridableField = info?.HasAttribute<NonOverridableAttribute>() ?? false;
			if (overridable && !nonOverridableField &&
				(property.serializedObject.targetObject is PrototypeableObjectBase
                 || property.serializedObject.targetObject is BlueprintEditorWrapper
                 || property.serializedObject.targetObject is BlueprintComponentEditorWrapper))
			{
                //PFLog.Default.Log($"Control for {property.propertyPath}: overridable");
				m_OverridableControl = new OverridablePropertyControl(this);
				OverridableControlContainer.Add(m_OverridableControl);
			}

			RegisterCallback<FocusEvent>(OnFocus);
			RegisterCallback<BlurEvent>(OnBlur);
			LoadSavedExpandedState();
		}

		protected virtual void LoadSavedExpandedState()
		{
			IsExpanded = GetSavedExpandedState();
		}

		protected OwlcatProperty(SerializedProperty property, Layout layout) : this(property, layout, false, true)
		{
		}

		public OwlcatProperty(SerializedProperty property) : this(property, Layout.Horizontal, false, true)
		{
		}

		public static OwlcatProperty CreateDefault(SerializedProperty property)
		{
			var result = new OwlcatProperty(property);
			// force empty label text to prevent creation of duplicate label
			var innerField = new PropertyField(property, string.Empty) { name = property.propertyPath };
			innerField.BindProperty(property);
			innerField.AddToClassList("owlcat-inner-field");
			result.ContentContainer.Add(innerField);
			
			return result;
		}

		public static OwlcatProperty CreateGeneric(SerializedProperty prop)
		{
			bool isExpandable = !prop.GetFieldInfo()?.HasAttribute<NonFoldoutAttribute>() ?? true;
			var result = new OwlcatProperty(prop, Layout.Vertical, isExpandable, false);
			result.TitleLabel.text = prop.displayName;
			if (!isExpandable)
			{
				result.TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			}
			
			foreach (var child in prop.GetChildren())
			{
				var field = UIElementsUtility.CreatePropertyElement(child, false);
				result.ContentContainer.Add(field);
			}

			return result;
		}

		public void AddComponent([CanBeNull] IOwlcatPropertyComponent component)
		{
			if (component == null)
			{
				return;
			}

			var titleProvider = component as IOwlcatPropertyTitleProvider; 
			if (titleProvider != null)
			{
				if (m_TitleProvider == null || m_TitleProvider.Order > titleProvider.Order)
				{
					m_TitleProvider = titleProvider;
				}
			}

			if (component is IOwlcatPropertyInputHandler handler)
			{
				m_InputHandlers.Add(handler);
				m_InputHandlers.Sort(OwlcatPropertyInputHandlerSorter.Instance);
			}

			m_Components[component.GetType()] = component;
			component.AttachToProperty(this);

			if (titleProvider != null)
			{
				UpdateTitle();
			}
		}

		public bool HasComponent<T>() where T : OwlcatPropertyComponent
			=> m_Components.ContainsKey(typeof(T));

		public void UpdateTitle()
		{
			TitleLabel.text = m_TitleProvider?.GetTitle() ?? Property.displayName;
			HeaderContainer.tooltip = Property.GetTooltip();
		}

		private void OnFocus(FocusEvent evt)
		{
			var focusDelegate = this.GetFirstFocusableTitle();
			if (focusDelegate != null)
			{
				focusDelegate.Focus();
			}
			else if (!m_FocusedDirectly)
			{
				m_FocusedDirectly = true;
				m_PrevBackgroundColor = style.backgroundColor;
				style.backgroundColor = new StyleColor(new Color(0, 65, 255, 80));
			}
		}

		private void OnBlur(BlurEvent evt)
		{
			if (m_FocusedDirectly)
			{
				m_FocusedDirectly = false;
				style.backgroundColor = m_PrevBackgroundColor;
			}
		}

		protected override void OnAttachToPanelInternal(AttachToPanelEvent evt)
		{
			base.OnAttachToPanelInternal(evt);
			this.OwlcatBind(Property.serializedObject);
			if (ContentContainer.IsImguiWrapper())
			{
				HeaderContainer.style.display = DisplayStyle.None;
			}
		}

		public bool TryHandle(KeyDownEvent evt)
		{
			foreach (var handler in m_InputHandlers)
			{
				handler.TryHandle(evt);
				if (evt.isPropagationStopped)
					return true;
			}

			return false;
		}

		protected override void SwitchExpanded(MouseDownEvent evt)
		{
			base.SwitchExpanded(evt);
			UIElementsUtility.SetExpandedState(GetExpandedPath(), IsExpanded);
		}

		protected override string GetExpandedPath()
		{
			var result = PropertyPath;
			if (Property.serializedObject.targetObject is BlueprintComponentEditorWrapper component)
			{
				var index = component.Component.OwnerBlueprint.ComponentsArray.FindIndex(x => x == component.Component);
				result = component.name.Split("$")[1] + $"[{index}]" + "/" + result; //component
			}

			return result;
		}
	}
}