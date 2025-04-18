using System.Xml.Serialization;
using Owlcat.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Workspace
{
	public class WorkspaceItemPrefab : WorkspaceItemBase
	{
		public string PathSaved;
		private string m_GuidSaved;
		private GameObject m_GameObject;

		public string PrefabGuid
		{
			get { return m_GuidSaved; }
			set
			{
				var path = AssetDatabase.GUIDToAssetPath(value);
				GameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				m_GuidSaved = value;
				PathSaved = path;
			}
		}

		[XmlIgnore]
		public GameObject GameObject
		{
			get { return m_GameObject; }
			set
			{
				m_GameObject = value;
				UpdateGUIContent();
			}
		}

		public override Vector2 Measure()
		{
			return OwlcatEditorStyles.Instance.PrefabItem.CalcSize(GUIContent);
		}

		public override void DoubleClick()
		{
		}

		public override void Click()
		{
			Selection.activeObject = GameObject;
		}

		public override Object GetDraggedObject()
		{
			return GameObject;
		}
		
		protected override void UpdateGUIContent()
		{
			string text = TargetPrefix + (m_GameObject ? m_GameObject.name : "??");
			string tooltip = m_GameObject ? AssetDatabase.GetAssetPath(m_GameObject) : PathSaved;

			GUIStyle = OwlcatEditorStyles.Instance.PrefabItem;
			BackgroundColor = Target != null ? TargetBackgroundColor : Color.white;
			GUIContent = new GUIContent(text, OwlcatEditorStyles.Instance.PrefabItemIcon, tooltip);
			m_GuidSaved = AssetDatabase.AssetPathToGUID(PathSaved = AssetDatabase.GetAssetPath(m_GameObject));
		}
	}
}