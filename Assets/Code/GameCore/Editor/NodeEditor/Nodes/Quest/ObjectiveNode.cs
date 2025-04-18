using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Blueprints.Quests.Logic;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Localization;
using UnityEditor;
using UnityEngine;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.NodeEditor.Nodes.Quest
{
    public class ObjectiveNode : EditorNode<BlueprintQuestObjective>
    {
        public int AddendumsGroup = 0;

        public ObjectiveNode(Graph graph, BlueprintQuestObjective asset) : base(graph, asset, new Vector2(200, 50))
        {

        }

		public override string GetText()
		{
			return Asset.Description;
		}

        public override EditorNode AddVirtualChild(EditorNode referencedNode)
        {
            return null;
        }

        public override Color GetWindowColor()
        {
            return Asset.IsAddendum ? Color.yellow : Color.green;
        }

        protected override void DrawContent()
        {
            SerializedObject.Update();

#if UNITY_EDITOR && EDITOR_FIELDS
            if (!Asset.IsErrandObjective)
            {
                GUILayout.Label("Title");
                var title = FindProperty("Title");
                LocalizationEditorGUI.LocalizedStringField(title, Asset.Title, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);

                GUILayout.Label("Description");
                var description = FindProperty("Description");
                LocalizationEditorGUI.LocalizedStringField(description, Asset.Description, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);
            }
#endif

            SerializedObject.ApplyModifiedProperties();
        }

		public override IEnumerable<string> GetMarkers(bool extended)
        {
            if (Asset.IsHidden)
                yield return "Hidden";
            if (Asset.IsAutomaticallyStartingAddendum)
                yield return "Auto-Start";
            if (Asset.IsFinishParent)
            {
                if (Asset.NextObjectives.Count == 0)
                    yield return "Complete";
				else
					yield return "Fail";
            }

			if (extended)
			{
				foreach (var xp in Asset.GetComponents<Experience>())
				{
					yield return xp.GetDescription();
				}

				foreach (var logic in Asset.GetComponents<INodeEditorDescriptionProvider>())
				{
					yield return logic.GetDescription();
				}
			}
        }

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
        {
        var firstAddendums = Asset.Addendums.ToList();
            foreach (var o in Asset.Addendums)
            {
                if (o == null)
                    continue;
                foreach (var next in o.NextObjectives)
                {
                    firstAddendums.Remove(next);
                }
            }

            return Asset.NextObjectives.Concat(firstAddendums);
        }

        protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
        {
            var objective =  r as BlueprintQuestObjective;
            if (objective == null)
                return null;

            if (!Asset.IsAddendum)
            {
                if (objective.IsAddendum)
                    return FindProperty("m_Addendums");
                else
                    return FindProperty("m_NextObjectives");
            }
            else
            {
                if (objective.IsAddendum)
                    return FindProperty("m_NextObjectives");
            }

            return null;
        }

        public override void RemoveReferencedAsset(ScriptableObject asset, bool move = false)
        {
            var objective = BlueprintEditorWrapper.Unwrap<BlueprintQuestObjective>(asset);//asset as BlueprintQuestObjective;
            if (objective == null)
                return;

            if (Asset.Addendums.Contains(objective))
                GetAddendumsTree(objective).ForEach(Asset.RemoveAddendum);
            if (Asset.NextObjectives.Contains(objective))
                Asset.RemoveNextObjective(objective);
        }

        public override void AddReferencedAsset(ScriptableObject asset)
        {
            var objective = BlueprintEditorWrapper.Unwrap<BlueprintQuestObjective>(asset);//asset as BlueprintQuestObjective;
            if (objective == null)
                return;

            if (Asset.IsAddendum)
            {
                objective.SetIsAddendum(true);
                Asset.AddNextObjective(objective);

                ObjectiveNode parentObjective = this;
                while (parentObjective != null && parentObjective.Asset.IsAddendum)
                    parentObjective = parentObjective.Parent as ObjectiveNode;

                if (parentObjective != null)
                    GetAddendumsTree(objective).ForEach(parentObjective.Asset.AddAddendum);
            }
            else
            {
                if (objective.IsAddendum)
                    GetAddendumsTree(objective).ForEach(Asset.AddAddendum);
                else
                    Asset.AddNextObjective(objective);
            }
        }

        private static IEnumerable<BlueprintQuestObjective> GetAddendumsTree(BlueprintQuestObjective firstAddendum)
        {
            var queue = new Queue<BlueprintQuestObjective>();
            var visited = new HashSet<BlueprintQuestObjective>();
            queue.Enqueue(firstAddendum);
            while (queue.Count > 0)
            {
                var addendum = queue.Dequeue();
                visited.Add(addendum);

                addendum.NextObjectives
                    .Where(n => !visited.Contains(n))
                    .ForEach(queue.Enqueue);
            }

            return visited;
        }
    }
}