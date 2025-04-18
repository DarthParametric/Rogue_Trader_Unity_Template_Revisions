using System.Linq;
using Kingmaker.Blueprints;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomEditor(typeof(LibraryScriptableObject))]
	internal class LibraryScriptableObjectInspector : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			if (GUILayout.Button("Build library"))
			{
				((LibraryScriptableObject)target).BuildLibrary(false);
				EditorUtility.SetDirty(target);
			}
			if (GUILayout.Button("Check for deleted comps"))
			{
				var deps = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(target), true);
				foreach (var dep in deps)
				{
					if(!dep.EndsWith(".prefab"))
						continue;

					var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dep);
					var comps = prefab.GetComponentsInChildren<Component>(true);
					if (comps.Any(c => !c))
					{
						Debug.Log("Found missing componen in "+dep);
					}
				}
			}
		}
	}
}