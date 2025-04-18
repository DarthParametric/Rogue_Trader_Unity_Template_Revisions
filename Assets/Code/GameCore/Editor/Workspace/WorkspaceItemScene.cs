using Owlcat.Editor.Utility;
using System.Xml.Serialization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kingmaker.Editor.Workspace
{
	public class WorkspaceItemScene : WorkspaceItemBase
	{
		private SceneAsset m_SceneAsset;

		public string Guid
		{
			get { return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(SceneAsset)); }
			set { SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(value)); }
		}

		[XmlIgnore]
		public SceneAsset SceneAsset
		{
			get { return m_SceneAsset; }
			set
			{
				m_SceneAsset = value;
				UpdateGUIContent();
			}
		}

		public override Vector2 Measure()
		{
			return OwlcatEditorStyles.Instance.PrefabItem.CalcSize(GUIContent);
		}

		public override void DoubleClick()
		{
			EditorSceneManager.OpenScene(AssetDatabase.GetAssetOrScenePath(SceneAsset), OpenSceneMode.Single);
		}

		public override void Click()
		{
			Selection.activeObject = SceneAsset;
		}

		public override Object GetDraggedObject()
		{
			return SceneAsset;
		}

		protected override void UpdateGUIContent()
		{
			string text = m_SceneAsset ? m_SceneAsset.name : "??";
			string tooltip = m_SceneAsset ? AssetDatabase.GetAssetPath(m_SceneAsset) : null;

			GUIStyle = OwlcatEditorStyles.Instance.PrefabItem;
			BackgroundColor = Target != null ? TargetBackgroundColor : Color.white;
			GUIContent = new GUIContent(TargetPrefix + text, OwlcatEditorStyles.Instance.SceneItemIcon, tooltip);
		}
	}
}