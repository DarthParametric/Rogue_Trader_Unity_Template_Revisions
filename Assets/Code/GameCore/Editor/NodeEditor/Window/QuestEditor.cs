using System;
using JetBrains.Annotations;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Blueprints.Quests;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.NodeEditor.Window
{
    public class QuestEditor : NodeEditorBase
    {
        public QuestEditor()
        {
            titleContent = new GUIContent("Quest Editor");
        }

        [BlueprintContextMenu("Open in Quest Editor", BlueprintType = typeof(BlueprintQuest))]
        public static void OpenAssetInQuestEditor(BlueprintQuest bp)
        {
            Focus(bp, null);
        }

        public static void Focus([CanBeNull] BlueprintQuest quest, Object asset)
        {
            if (quest == null)
            {
                quest = (BlueprintEditorWrapper.Unwrap<BlueprintQuestObjective>(asset))?.Quest;
            }

            if (quest == null)
            {
                PFLog.Default.Error("Quest is missing");
                return;
            }

            var window = GetWindow<QuestEditor>("Quest Editor", false);
            window.OpenAsset(BlueprintEditorWrapper.Wrap(quest), asset);
        }

        [MenuItem("Design/Quest Editor", false, 2004)]
        public static void ShowWindow()
        {
            GetWindow<QuestEditor>().Show();
        }

        protected override Type GetOpenType()
        {
            return typeof(BlueprintQuest);
        }

        protected override void ExtraHUDButtons()
        {
            base.ExtraHUDButtons();

            if (Graph != null)
            {
                if (Graph.ShowTagButtons)
                {
                    if (GUILayout.Button("Hide tag buttons"))
                    {
                        Graph.ShowTagButtons = false;
                    }
                }
                else
                {
                    if (GUILayout.Button("Show tag buttons"))
                    {
                        Graph.ShowTagButtons = true;
                    }
                }
            }
        }
    }
}