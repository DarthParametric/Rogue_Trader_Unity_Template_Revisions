using System;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class OwlcatPropertyLayout : OwlcatVisualElement
	{
		public enum Layout
		{
			Horizontal = 0,
			Vertical = 1,
		}

		public readonly bool Expandable;

		public readonly VisualElement HeaderContainer;
		public readonly OwlcatContentContainer ContentContainer;
		public readonly VisualElement ControlsContainer;

		public readonly OwlcatTitleLabel TitleLabel;
		
		private readonly Image m_Collapsed;
		private readonly Image m_Expanded;
		
		private bool m_IsExpanded;

		public bool IsExpanded
		{
			get => m_IsExpanded;
			set
			{
				m_IsExpanded = value;
				OnIsExpandedChangedInternal();
			}
		}
		
		public VisualElement OverridableControlContainer { get; private set; }

		public override bool canGrabFocus
			=> true;

		public OwlcatPropertyLayout(Layout layout, bool expandable)
		{
			Expandable = expandable;
			focusable = true;
			
			AddToClassList("owlcat-property");

			HeaderContainer = new VisualElement {name = "header"};
			ContentContainer = new OwlcatContentContainer {name = "content"};
			ControlsContainer = new VisualElement {name = "controls"};
			
			HeaderContainer.AddToClassList("owlcat-property-header");
			ContentContainer.AddToClassList("owlcat-property-content");
			ControlsContainer.AddToClassList("owlcat-property-controls");
			
			TitleLabel = new OwlcatTitleLabel();
			TitleLabel.style.flexGrow = 1;
			TitleLabel.style.flexShrink = 1;
			HeaderContainer.Add(TitleLabel);
			HeaderContainer.Add(new OwlcatTitleLabelSizeControl());

			if (expandable)
			{
				AddToClassList("owlcat-expandable");
				HeaderContainer.AddToClassList("owlcat-expandable");
				ContentContainer.AddToClassList("owlcat-expandable");
				ControlsContainer.AddToClassList("owlcat-expandable");
				
				Image CreateExpandImage(Texture2D texture)
					=> new Image
					{
						image = texture, 
						tintColor = Color.black,
						scaleMode = ScaleMode.ScaleToFit,
						focusable = true,
					};

				m_Collapsed = CreateExpandImage(UIElementsResources.FoldoutCollapsed);
				HeaderContainer.hierarchy.Insert(0, m_Collapsed);
			
				m_Expanded = CreateExpandImage(UIElementsResources.FoldoutExpanded);
				HeaderContainer.hierarchy.Insert(0, m_Expanded);
			
				HeaderContainer.RegisterCallback<MouseDownEvent>(SwitchExpanded);
			}

			switch (layout)
			{
				case Layout.Horizontal:
					MakeHorizontalLayout();
					break;
				case Layout.Vertical:
					MakeVerticalLayout();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
		}

		protected virtual void SwitchExpanded(MouseDownEvent evt)
		{
			if (evt.button == 0)
			{
				IsExpanded = !IsExpanded;
				if (evt.altKey)
				{
					SetExpandedToChildren(this, IsExpanded);
				}
			}
		}

		public static void SetExpandedToChildren(VisualElement visualElement, bool isExpanded)
		{
			foreach (var ve in visualElement.Children())
			{
				if (ve is OwlcatPropertyLayout owlcatPropertyLayout)
				{
					owlcatPropertyLayout.IsExpanded = isExpanded;
				}
				
				SetExpandedToChildren(ve, isExpanded);
			}
		}

		private void MakeHorizontalLayout()
		{
			Add(HeaderContainer);
			Add(ContentContainer);
			Add(ControlsContainer);
			Add(OverridableControlContainer = new VisualElement {name = "OverridableControlContainer"});
		}

		private void MakeVerticalLayout()
		{
			style.flexDirection = FlexDirection.Column;
			ContentContainer.style.flexDirection = FlexDirection.Column;
			var headerAndControls = new VisualElement
			{
				name = "HeaderAndControls",
				style = {flexDirection = FlexDirection.Row}
			};
			Add(headerAndControls);
			headerAndControls.Add(HeaderContainer);
			headerAndControls.Add(ControlsContainer);
			headerAndControls.Add(OverridableControlContainer = new VisualElement {name = "OverridableControlContainer"});
			
			Add(ContentContainer);

			ControlsContainer.style.flexGrow = 1;
			ControlsContainer.Add(new Label(" ") {name = "expander"});
			
			TitleLabel.style.width = new StyleLength(StyleKeyword.Auto);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			OnIsExpandedChangedInternal();
			OnAttachToPanelInternal(evt);
		}

		protected virtual void OnAttachToPanelInternal(AttachToPanelEvent evt) { }

		protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt) { }

		protected virtual void OnIsExpandedChanged() { }

		private void OnIsExpandedChangedInternal()
		{
			if (!Expandable)
			{
				return;
			}

			UpdateExpandableVisibility();
			OnIsExpandedChanged();
		}

		private void UpdateExpandableVisibility()
		{
			ContentContainer.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
			m_Expanded.style.display = IsExpanded ? DisplayStyle.Flex : DisplayStyle.None;
			m_Collapsed.style.display = IsExpanded ? DisplayStyle.None : DisplayStyle.Flex;
		}

		protected bool GetSavedExpandedState()
		{
			return UIElementsUtility.GetExpandedState(GetExpandedPath());
		}
		
		protected virtual string GetExpandedPath()
		{
			return string.Empty;
		}
	}
}