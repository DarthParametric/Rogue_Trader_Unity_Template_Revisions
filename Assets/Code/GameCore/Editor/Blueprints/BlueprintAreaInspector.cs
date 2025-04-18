#if UNITY_EDITOR && EDITOR_FIELDS
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Workspace;
using Kingmaker.Utility;
using Kingmaker.View;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    [BlueprintCustomEditor(typeof(BlueprintAreaPart), true)]
    public class BlueprintAreaInspector : BlueprintInspectorCustomGUI
    {
        public override void OnHeader(SimpleBlueprint bp)
        {
            using (GuiScopes.Horizontal())
            {
                #if !OWLCAT_MODS
                if (bp is BlueprintArea area && GUILayout.Button("Open workspace"))
                {
                    OpenWorkspace(area);
                }

                if (GUILayout.Button("Fix light scenes"))
                {
                    FixLightScene((BlueprintAreaPart)bp);
                }

                if (GUILayout.Button("Find audio scenes"))
                {
                    ((BlueprintAreaPart)bp).FixAudioScenes();
                }
                #endif
            }
        }
#if !OWLCAT_MODS       
        private void FixLightScene(BlueprintAreaPart part)
        {
            part.FixLightScenes();
        }


        private void OpenWorkspace(BlueprintArea area)
        {
            var workspace = area.RelatedWorkspace as WorkspacePreset;
            if (!workspace)
            {
                foreach (var ws in AssetUtility.LoadAssets<WorkspacePreset>("Assets/Mechanics/Workspaces")) 
                {
                    if (ws.XmlData.Contains(area.AssetGuid))
                    {
                        workspace = ws;
                        break;
                    }
                }
            }

            if (workspace)
            {
                if (area.RelatedWorkspace != workspace)
                {
                    area.RelatedWorkspace = workspace;
                    area.SetDirty();
                }
                var w = EditorWindow.GetWindow<WorkspaceCanvasWindow>();
                w.LoadPreset(workspace);
                w.ActiveArea = area;
            }
            else
            {
                PFLog.Default.Error("Can't find workspace for area {0}", area.name);
            }
        }
#endif

        [MenuItem("Design/Select Current Area %&r", false, 10010)]
		public static void SelectCurrentArea()
		{
			var preset = Object.FindObjectsOfType<AreaEnterPoint>()
				.Select(ep => ep.Blueprint)
				.Where(ep => ep != null)
				.Select(ep => ep.Area)
				.FirstOrDefault(p => p);

			if (preset != null)
			{
				Selection.activeObject = BlueprintEditorWrapper.Wrap(preset);
			}
		}
	}
}
#endif