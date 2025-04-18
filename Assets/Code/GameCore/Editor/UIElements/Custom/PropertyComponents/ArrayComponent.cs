using Code.GameCore.Mics;
using Kingmaker.Editor.UIElements.Custom.Array;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom.Base
{
	public class ArrayElementComponent : 
		OwlcatPropertyComponent, IOwlcatPropertyInputHandler, IOwlcatPropertyTitleProvider
	{
		private readonly ArrayElementMenu m_Menu;

		int IOwlcatPropertyTitleProvider.Order { get; } = 1;

		int IOwlcatPropertyInputHandler.Order { get; } = 1;

		public int ArrayElementIndex { get; }

		public ArrayElementComponent(int index, ArrayElementMenu menu)
		{
			m_Menu = menu;
			ArrayElementIndex = index;
		}

		string IOwlcatPropertyTitleProvider.GetTitle()
			=> $"Element {ArrayElementIndex}";

		protected override void OnAttached()
		{
			Property.style.backgroundColor = UIElementsResources.GetZebra(ArrayElementIndex);
			Property.UpdateTitle();
			
			Property.ControlsContainer.Add(CreateArrayElementControl(m_Menu));
			Property.HeaderContainer.AddManipulator(
				new ContextualMenuManipulator(
					evt => evt.menu.AppendAction(
						"Remove", 
						x => m_Menu.RemoveElement(ArrayElementIndex))));
		}

		private VisualElement CreateArrayElementControl(ArrayElementMenu menu)
		{
			var root = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
			var moveUp = new OwlcatSmallButton(() => menu.MoveUp(ArrayElementIndex)) { text = "↑" };
			var moveDown = new OwlcatSmallButton(() => menu.MoveDown(ArrayElementIndex)) { text = "↓" };
			var remove = new OwlcatSmallButton(() => menu.RemoveElement(ArrayElementIndex)) { text = "X" };
			remove.AddToClassList("red-button");
			
			var menuBtn = new OwlcatSmallButton(() => m_Menu.ShowMenu(this));
			// {text = string.Empty, style = {alignSelf = Align.FlexStart}};
			var img = new Image { image = UIElementsResources.SettingsIcon, scaleMode = ScaleMode.ScaleToFit };
			menuBtn.style.backgroundColor = System.Drawing.Color.LightGray.ToUnityColor();
			menuBtn.Add(img);

			root.Add(moveUp);
			root.Add(moveDown);
			root.Add(menuBtn);
			root.Add(remove);
			return root;
		}

		void IOwlcatPropertyInputHandler.TryHandle(KeyDownEvent evt)
		{
			if (evt.keyCode == KeyCode.Delete)
			{
				m_Menu.RemoveElement(ArrayElementIndex);
				evt.StopPropagation();
			}
		}
	}
}