using System.Xml.Serialization;
using Owlcat.Editor.Utility;
using UnityEngine;

namespace Kingmaker.Editor.Workspace
{
	[XmlInclude(typeof(WorkspaceItemBlueprint))]
    [XmlInclude(typeof(WorkspaceItemPrefab))]
	[XmlInclude(typeof(WorkspaceItemReference))]
	[XmlInclude(typeof(WorkspaceItemScene))]
	[XmlInclude(typeof(WorkspaceItemOtherAsset))]
	public abstract class WorkspaceItemBase
	{
		protected GUIStyle GUIStyle;
		protected GUIContent GUIContent;
		protected Color BackgroundColor;
		protected int? Target { get; private set; }
		protected string TargetPrefix { get; private set; }

		protected Color TargetBackgroundColor
			=> OwlcatEditorStyles.Instance.WorkspaceTargetBackgroundColor;
		
		public bool Selected { get; set; }

		public Rect Rect
			=> new Rect(Position, Measure());
		private Vector2 m_Position;

		public Vector2 Position
		{
			get
			{
				if (m_Position.x < 0)
					m_Position.x = 0;
				if (m_Position.y < 0)
					m_Position.y = 0;
				return m_Position;
			}
			set { m_Position = value; }
		}

		public void MarkItemAsTarget(int? targetNumber)
		{
			Target = targetNumber;
			TargetPrefix = Target != null ? $"[Target#{Target.Value}]" : "";
			UpdateGUIContent();
		}
		
		public virtual void OnGUI(Rect rect)
		{
			GUI.backgroundColor = BackgroundColor;
			GUI.Box(rect, GUIContent, GUIStyle);
			GUI.backgroundColor = Color.white;
		}

		public Texture GetDragIcon()
		{
			return GUIContent.image;
		}

		public virtual void ShowContextMenu()
		{
		}

		public abstract void Click();
		
		public abstract void DoubleClick();

		public abstract Vector2 Measure();
		
		protected abstract void UpdateGUIContent();

		public virtual Object GetDraggedObject()
		{
			return null;
		}
	}
}