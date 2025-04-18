using Kingmaker.Editor.UIElements.Custom.Base;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class DragAndDropComponent : OwlcatPropertyComponent
	{
		private VisualElement m_DndTarget;

		private bool m_IsValidDrag;

		private bool m_IsEnable;

		protected Func<bool> IsValidateFunc { get; set; }

		protected Action DropFunc { get; set; }

		public bool IsEnable
		{
			get => m_IsEnable;
			set
			{
				if (m_IsEnable != value)
				{
					m_IsEnable = value;
					if (m_IsEnable)
					{
						m_DndTarget.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
						m_DndTarget.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
						m_DndTarget.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
						m_DndTarget.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
						m_DndTarget.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
					}
					else
					{
						m_DndTarget.UnregisterCallback<DragEnterEvent>(OnDragEnterEvent);
						m_DndTarget.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
						m_DndTarget.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
						m_DndTarget.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
						m_DndTarget.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
					}
				}
			}
		}

		public DragAndDropComponent(Func<bool> validateFunc, Action dropFunc)
		{
			IsValidateFunc = validateFunc;
			DropFunc = dropFunc;
		}

		protected override void OnAttached()
		{
			m_DndTarget = Property;
			IsEnable = true;
		}

		private void OnDragEnterEvent(DragEnterEvent e)
		{
			m_DndTarget.AddToClassList("drag-target");
			m_IsValidDrag = IsValidateFunc();
			if (m_IsValidDrag)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Move;
			}
			else
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
		}

		private void OnDragLeaveEvent(DragLeaveEvent e)
		{
			m_DndTarget.RemoveFromClassList("drag-target");
			DragAndDrop.visualMode = DragAndDropVisualMode.None;
		}

		private void OnDragUpdatedEvent(DragUpdatedEvent e)
		{
			m_DndTarget.AddToClassList("dragover");
			if (m_IsValidDrag)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Move;
			}
			else
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
			}
		}

		private void OnDragPerformEvent(DragPerformEvent e)
		{
			if (IsValidateFunc())
			{
				DragAndDrop.AcceptDrag();
				e.StopPropagation();
				DropFunc();
				m_DndTarget.RemoveFromClassList("drag-target");
			}
		}

		private void OnDragExitedEvent(DragExitedEvent e)
		{
			m_DndTarget.RemoveFromClassList("drag-target");
		}
	}
}