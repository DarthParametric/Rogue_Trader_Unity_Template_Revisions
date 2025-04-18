using OwlcatModification.Editor.Build;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OwlcatModification.Editor.Inspector
{
	[CustomEditor(typeof(Modification))]
	public class ModificationManifestInspector : UnityEditor.Editor
	{
		private Modification Target
			=> (Modification)target;
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			bool build = GUILayout.Button("Build", GUILayout.MaxWidth(165));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal(); 
			GUILayout.FlexibleSpace();
			bool install = GUILayout.Button("Build and Install", GUILayout.MaxWidth(165));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			bool publish = GUILayout.Button("Publish to Steam Workshop", GUILayout.MaxWidth(165));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			if (install)
			{
				Builder.BuildAndInstall(Target);
			}
			
			if (build)
			{
				Builder.BuildAndOpen(Target);
			}
			
			if (publish) 
			{
				Builder.BuildAndPublish(Target);
			}
		}
	}
}