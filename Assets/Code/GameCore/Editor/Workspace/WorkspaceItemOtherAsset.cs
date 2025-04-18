using Owlcat.Editor.Utility;
using System.Xml.Serialization;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Workspace
{
	public class WorkspaceItemOtherAsset : WorkspaceItemBase
	{
		private Object m_Asset;
		public string PathSaved;
		private string m_GuidSaved;

		public string AssetGuid
		{
			get { return m_GuidSaved; }
			set
			{
				var path = AssetDatabase.GUIDToAssetPath(value);
				// Load asset
				if (path != "")
				{
					Asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				}
				// Load blueprint, load by GUID
				else
				{
					SimpleBlueprint blueprint = BlueprintsDatabase.LoadById<SimpleBlueprint>(value);
					if (blueprint == null)
					{
						Asset = null;
						PFLog.Default.Error($"No blueprint for GUID {value}.");
					}
					else
					{
						Asset = BlueprintEditorWrapper.Wrap(blueprint);
					}
				}
                m_GuidSaved = value;
				PathSaved = path;
			}
		}

		[XmlIgnore]
		public Object Asset
		{
			get { return m_Asset; }
			set
			{
				m_Asset = value;
				UpdateGUIContent();
			}
		}

		public override Vector2 Measure()
		{
			var oldImage = GUIContent.image;
			if (oldImage)
				GUIContent.image = OwlcatEditorStyles.Instance.PrefabItemIcon;
			var res = OwlcatEditorStyles.Instance.PrefabItem.CalcSize(GUIContent);
			GUIContent.image = oldImage;
			return res;
		}

		public override void DoubleClick()
		{
		}

		public override void Click()
		{
			Selection.activeObject = Asset;
		}

		public override Object GetDraggedObject()
		{
			return Asset;
		}

		protected override void UpdateGUIContent()
		{
			var c = EditorGUIUtility.ObjectContent(m_Asset, m_Asset.Or(null)?.GetType());
			string text = TargetPrefix + (m_Asset ? m_Asset.name : "??");
			string tooltip = m_Asset ? AssetDatabase.GetAssetPath(m_Asset) : null;

			BackgroundColor = Target != null ? TargetBackgroundColor : Color.white;
			GUIStyle = OwlcatEditorStyles.Instance.PrefabItem;
			GUIContent = new GUIContent(text, c.image.Or(OwlcatEditorStyles.Instance.PrefabItemIcon), tooltip);
			m_GuidSaved = AssetDatabase.AssetPathToGUID(PathSaved = AssetDatabase.GetAssetPath(m_Asset));
		}
	}
}