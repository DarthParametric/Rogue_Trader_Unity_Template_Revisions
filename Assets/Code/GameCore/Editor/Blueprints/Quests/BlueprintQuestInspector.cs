#if UNITY_EDITOR && EDITOR_FIELDS
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Quests
{
	[BlueprintCustomEditor(typeof(BlueprintQuest), true)]
    public class BlueprintQuestInspector : BlueprintInspectorCustomGUI
    {
        public bool ShowAllObjectives;
        public bool ShowTopLevelObjectives = true;

        public override void OnBeforeComponents(SimpleBlueprint bp)
        {
            AdditionalGui((BlueprintQuest)bp);
            bp.SetDirty(); // todo: don't do this just EVERY frame duh
        }

        private void AdditionalGui(BlueprintQuest bp)
        {
            ShowTopLevelObjectives = EditorGUILayout.Foldout(ShowTopLevelObjectives, "Top Level Objectives");
            if (ShowTopLevelObjectives)
                foreach (var objective in bp.Objectives.ToList())
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.enabled = false;
                        BlueprintEditorUtility.ObjectField(objective, typeof(BlueprintQuestObjective), false);
                        GUI.enabled = true;

                        if (GUILayout.Button("Unlink", EditorStyles.miniButtonRight))
                            bp.UnlinkObjective(objective);
                    }

            ShowAllObjectives = EditorGUILayout.Foldout(ShowAllObjectives, "All Objectives");
            if (ShowAllObjectives)
                foreach (var objective in bp.AllObjectives.ToList())
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.enabled = false;
                        BlueprintEditorUtility.ObjectField(objective, typeof(BlueprintQuestObjective), false);
                        GUI.enabled = true;

                        if (GUILayout.Button("Unlink", EditorStyles.miniButtonRight))
                            bp.UnlinkObjective(objective);
                    }

            object newObjective = BlueprintEditorUtility.ObjectField("Link Objective", null, typeof(BlueprintQuestObjective), false);
            if (newObjective != null)
                bp.AddObjectiveFromMenu(newObjective);

            if (GUILayout.Button("Update And Validate AllObjectives"))
                bp.UpdateAndValidateObjectives();
        }
	}
}
#endif