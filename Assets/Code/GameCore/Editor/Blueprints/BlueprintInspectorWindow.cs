#if UNITY_EDITOR
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.UIElements;
using Kingmaker.Utility.EditorPreferences;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor.Blueprints
{
	public class BlueprintInspectorWindow : EditorWindow
	{
		[SerializeField]
		private SimpleBlueprint m_Blueprint;

		private UnityEditor.Editor m_Editor;
		private Vector2 m_Scroll;

		[MenuItem("CONTEXT/BlueprintScriptableObject/Copy asset path")]
		public static void CopyAssetPath()
		{
			GUIUtility.systemCopyBuffer = AssetDatabase.GetAssetPath(Selection.activeObject);
		}

		[MenuItem("CONTEXT/BlueprintScriptableObject/Copy asset name (and guid)")]
		public static void CopyAssetName()
		{
			var asset = Selection.activeObject;
			var path = AssetDatabase.GetAssetPath(asset);
			var guid = AssetDatabase.AssetPathToGUID(path);
			GUIUtility.systemCopyBuffer = $"{asset.name} ({guid})";
		}

		public static void OpenFor(SimpleBlueprint blueprint)
		{
			var allWindows = Resources.FindObjectsOfTypeAll<BlueprintInspectorWindow>();
			var window = allWindows.FirstOrDefault(w => w.m_Blueprint == blueprint);
			var move = false;

			if (window == null)
			{
				window = CreateInstance<BlueprintInspectorWindow>();
				move = true;
			}
			window.Init(blueprint);
			window.Show(true);
			window.Focus();

			if (move)
				EditorApplication.delayCall += () =>
														window.position = new Rect(window.position.min + Vector2.one * 16, window.position.size);
		}

		private void OnEnable()
		{
			if (m_Blueprint)
				Init(m_Blueprint);
		}

		private void Init(SimpleBlueprint blueprint)
		{
			m_Editor = default;
			rootVisualElement.Clear();

			m_Blueprint = blueprint;
			titleContent = new GUIContent(blueprint.name);

            var w = BlueprintEditorWrapper.Wrap(blueprint);
            
			if (EditorPreferences.Instance.UseNewEditor)
			{
				var root = UIElementsUtility.CreateInspector(new SerializedObject(w));
				root.style.paddingLeft = 15;
				root.style.paddingRight = 6;
				root.style.paddingBottom = 2;
				root.style.paddingTop = 2;
				
                //rootVisualElement.Add(new IMGUIContainer(m_Editor.DrawHeader)); todo
                
				var scroll = new ScrollView(ScrollViewMode.Vertical);
				scroll.contentContainer.Add(root);
				rootVisualElement.Add(scroll);
			}
			else
			{
				UnityEditor.Editor.CreateCachedEditor(w, null, ref m_Editor);
			}
		}

		private void OnGUI()
		{
			if (m_Editor)
			{
                m_Editor.DrawHeader();
                using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll))
                {
                    m_Editor.OnInspectorGUI();
                    m_Scroll = scope.scrollPosition;
                }
            }
            // todo: [bp] fix drag and drop onto external bp inspector
        }
	}
}
#endif