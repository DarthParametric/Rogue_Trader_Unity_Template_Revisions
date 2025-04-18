#if UNITY_EDITOR && EDITOR_FIELDS
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Quests
{
	[BlueprintCustomEditor(typeof(BlueprintQuestObjective), true)]
    public class BlueprintQuestObjectiveInspector : BlueprintInspectorCustomGUI
    {
        public bool ShowAddendums = true;
        public bool ShowNextObjectives = true;

        public override void OnBeforeComponents(SimpleBlueprint bp)
        {
            AdditionalGui((BlueprintQuestObjective)bp);
            //bp.SetDirty(); // todo: don't do this just EVERY frame duh
        }

        private void AdditionalGui(BlueprintQuestObjective bp)
        {
            bool lastObjective = bp.NextObjectives.Empty();

            if (bp.IsAddendum)
            {
                GUI.enabled = false;
                EditorGUILayout.Toggle("Is Addendum", bp.IsAddendum);
                GUI.enabled = true;

                bool autoStart = EditorGUILayout.Toggle("Is Starting Automatically", bp.IsAutomaticallyStartingAddendum);
                bp.SetStartAddendumAutomatically(autoStart);

                var caption = lastObjective ? "Finish Parent" : "Fail Parent";
                var newValue = EditorGUILayout.Toggle(caption, bp.IsFinishParent);
                bp.SetIsFinishParent(newValue);

                //				var newValue = EditorGUILayout.Toggle("Is Starting Automatically", Blueprint.IsAutomaticallyStartingAddendum);
                //				Blueprint.SetStartAddendumAutomatically(newValue);
            }
            else
            {
                var caption = lastObjective ? "Finish Quest" : "Fail Quest";
                var newValue = EditorGUILayout.Toggle(caption, bp.IsFinishParent);
                bp.SetIsFinishParent(newValue);
            }

            GUILayout.Space(5);

            ShowNextObjectives = EditorGUILayout.Foldout(ShowNextObjectives, "Next AllObjectives");
            if (ShowNextObjectives)
            {
                foreach (var objective in bp.NextObjectives.ToList())
                    using (new GUILayout.HorizontalScope())
                    {
                        GUI.enabled = false;
                        BlueprintEditorUtility.ObjectField(objective, typeof(BlueprintQuestObjective), false);
                        GUI.enabled = true;

                        if (GUILayout.Button("Unlink", EditorStyles.miniButtonRight))
                            bp.RemoveNextObjective(objective);
                    }

                object newObjective = BlueprintEditorUtility.ObjectField(null, typeof(BlueprintQuestObjective), false);
                if (newObjective != null)
                    bp.AddNextObjectiveFromMenu(newObjective);
            }

            GUILayout.Space(10);

            ShowAddendums = EditorGUILayout.Foldout(ShowAddendums, "Addendums");
            if (ShowNextObjectives)
            {
                foreach (var addendum in bp.Addendums.ToList())
                {
                    if (addendum == null)
                        continue;
                    using (new GUILayout.HorizontalScope())
                    {
                        using (GuiScopes.LabelWidth(25))
                        {
                            var startingAutomatically = EditorGUILayout.ToggleLeft(
                                "Auto start",
                                addendum.IsAutomaticallyStartingAddendum,
                                GUILayout.ExpandWidth(false));
                            addendum.SetStartAddendumAutomatically(startingAutomatically);
                        }

                        GUI.enabled = false;
                        BlueprintEditorUtility.ObjectField(addendum, typeof(BlueprintQuestObjective), false);
                        GUI.enabled = true;

                        if (GUILayout.Button("Unlink", EditorStyles.miniButtonRight))
                        {
                            bp.RemoveAddendum(addendum);
                            addendum.SetIsAddendum(false);
                        }
                    }
                }

                object newAddendum = BlueprintEditorUtility.ObjectField(null, typeof(BlueprintQuestObjective), false);
                if (newAddendum != null)
                    bp.AddAddendumFromMenu(newAddendum);
            }
        }
    }
}
#endif