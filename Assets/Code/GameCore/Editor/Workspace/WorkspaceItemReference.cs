using System.Linq;
using System.Xml.Serialization;
using Kingmaker.View;
using Owlcat.Editor.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kingmaker.Editor.Workspace
{
    public class WorkspaceItemReference : WorkspaceItemBase
    {
        private string m_ObjectName;
        private string m_UniqueId;
        private string m_ScenePath;
        private string m_ViewType;

        public WorkspaceItemReference()
        {
        }

        public WorkspaceItemReference(EntityViewBase view)
        {
            m_UniqueId = view.UniqueId;
            m_ScenePath = view.gameObject.scene.path;
            m_ObjectName = view.name;
            m_ViewType = view.GetType().Name;
            GameObject = view.gameObject;
            UpdateGUIContent();
        }

        public string UniqueId
        {
            get { return m_UniqueId; }
            set
            {
                m_UniqueId = value;
                TryGetObject();
            }
        }

        public string ScenePath
        {
            get { return m_ScenePath; }
            set
            {
                m_ScenePath = value;
                TryGetObject();
                UpdateGUIContent();
            }
        }

        public string ObjectName
        {
            get { return m_ObjectName; }
            set
            {
                m_ObjectName = value;
                UpdateGUIContent();
            }
        }

        public string ViewType
        {
            get { return m_ViewType; }
            set
            {
                m_ViewType = value;
                UpdateGUIContent();
            }
        }

        [XmlIgnore]
        public GameObject GameObject{get; set; }

        void TryGetObject()
        {
            if(string.IsNullOrEmpty(UniqueId) || string.IsNullOrEmpty(ScenePath))
                return;

            if (GameObject)
            {
                var view = GameObject.GetComponent<EntityViewBase>();
                if (!view || view.UniqueId != UniqueId)
                    GameObject = null;
            }

            if (!GameObject && SceneManager.GetSceneByPath(ScenePath).isLoaded)
            {
                var view = Object.FindObjectsOfType<EntityViewBase>().SingleOrDefault(v => v.UniqueId == UniqueId);
                GameObject = view ? view.gameObject : null;
                ViewType = view ? view.GetType().Name : "Null";
            }
        }

        public override void OnGUI(Rect rect)
        {
            TryGetObject();
            base.OnGUI(rect);
        }

        public override Vector2 Measure()
        {
            TryGetObject();
            return OwlcatEditorStyles.Instance.ReferenceItem.CalcSize(GUIContent);
        }

        public override void DoubleClick()
        {
            if (!string.IsNullOrEmpty(ScenePath))
            {
                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.isLoaded)
                {
                    EditorSceneManager.OpenScene(ScenePath); 
                }
            }
        }

        public override void Click()
        {
            if(GameObject)
                Selection.activeObject = GameObject;
        }

        public override Object GetDraggedObject()
        {
            return GameObject;
        }

        protected sealed override void UpdateGUIContent()
        {
            var icon = EditorGUIUtility.Load("ObjectIcons/" + ViewType + ".png") as Texture2D;
            icon = icon.Or(OwlcatEditorStyles.Instance.ReferenceItemIcon);

            GUIStyle = OwlcatEditorStyles.Instance.ReferenceItem;
            BackgroundColor = Target != null ? TargetBackgroundColor : Color.white;
            GUIContent = new GUIContent(TargetPrefix + m_ObjectName, icon, m_ScenePath);
        }
    }
}