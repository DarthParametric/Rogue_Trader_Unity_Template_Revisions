using System.Linq;
using Kingmaker.Blueprints;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomEditor(typeof(MonoScript))]
	public class MonoScriptInspector: DecoratorEditor
	{
		private ScriptableObject[] m_References;
		[SerializeField]
		private bool m_ShowRefs = true;

		private Vector2 m_Scroll;
		private Vector2 m_Scroll2;

		public override void OnInspectorGUI()
		{
			var script = (MonoScript)target;
			if(!script || script.GetClass()==null)
				return; // this happens sometimes when still compiling
			if (script.GetClass().IsSubclassOf(typeof(PrototypeableObjectBase)))
			{
				DrawFindReferences();
				if (m_ShowRefs && m_References != null)
				{
					using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll2, GUILayout.ExpandHeight(true)))
					{
						m_Scroll2 = scope.scrollPosition;
						base.OnInspectorGUI();
						return;
					}
				}
			}
			base.OnInspectorGUI();
		}

		private void DrawFindReferences()
		{
			//throw new System.NotImplementedException();
			if (GUILayout.Button("Find referencing objects"))
			{
				FindReferences();
			}

			m_ShowRefs = EditorGUILayout.Foldout(m_ShowRefs, "Blueprints referencing this script");

			if (m_ShowRefs && m_References != null)
			{
				using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll, GUILayout.MaxHeight(300), GUILayout.ExpandHeight(true)))
				{
					m_Scroll = scope.scrollPosition;

					foreach (var reference in m_References)
					{
						EditorGUILayout.ObjectField(reference, typeof(BlueprintScriptableObject), false);
					}
				}
			}
		}

		private void FindReferences()
		{
			var script = (MonoScript)target;
			var bps = AssetDatabase.FindAssets("t:BlueprintScriptableObject", new[] { "Assets/Mechanics/Blueprints" });
			var allBlueprints =
				bps.Select(s => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(s), typeof(BlueprintScriptableObject)))
					.Cast<BlueprintScriptableObject>()
					.ToList();

			if (script.GetClass().IsSubclassOf(typeof(BlueprintScriptableObject)))
			{
				m_References = allBlueprints.Where(b => script.GetClass().IsInstanceOfType(b)).Cast<ScriptableObject>().ToArray();
			}
			else if (script.GetClass().IsSubclassOf(typeof(BlueprintComponent)))
			{
				m_References = allBlueprints.Where(b => b.ComponentsArray.Any(c=> script.GetClass().IsInstanceOfType(c))).Cast<ScriptableObject>().ToArray();
			}
		}

		public MonoScriptInspector() : base("MonoScriptInspector")
		{
		}
	}
}