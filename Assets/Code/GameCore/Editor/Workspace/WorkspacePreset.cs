using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Kingmaker.Editor.Workspace
{
	public class WorkspacePreset: ScriptableObject
	{
		[HideInInspector]
		public string XmlData;

		[ContextMenu("Open")]
		public void OpenInWorkspace()
		{
			var w = EditorWindow.GetWindow<WorkspaceCanvasWindow>();
			w.LoadPreset(this);
		}

        [OnOpenAsset(1)]
        public static bool OpenWorkspace(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId) as WorkspacePreset;
            if (obj)
            {
                var win = EditorWindow.GetWindow<WorkspaceCanvasWindow>();
                win.LoadPreset(obj);
                return true;
            }
            return false;
        }
	}
}