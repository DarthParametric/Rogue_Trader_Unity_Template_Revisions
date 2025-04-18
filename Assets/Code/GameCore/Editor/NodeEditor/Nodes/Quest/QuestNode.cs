using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Experience;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Localization;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.NodeEditor.Nodes.Quest
{
	public class QuestNode : EditorNode<BlueprintQuest>
	{	
		public QuestNode(Graph graph, BlueprintQuest asset) : base(graph, asset, new Vector2(200, 50))
		{
		}

		public override EditorNode AddVirtualChild(EditorNode referencedNode)
		{
			return null;
		}

		public override string GetText()
		{
			return Asset.Description;
		}

		protected override void DrawContent()
		{
			SerializedObject.Update();

#if UNITY_EDITOR && EDITOR_FIELDS
			GUILayout.Label("Title");
			var title = FindProperty("Title");
			LocalizationEditorGUI.LocalizedStringField(title, Asset.Title, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);

			GUILayout.Label("Description");
			var description = FindProperty("Description");
			LocalizationEditorGUI.LocalizedStringField(description, Asset.Description, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);
#endif

			SerializedObject.ApplyModifiedProperties();
		}

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
		{
			var list = Asset.Objectives.ToList();
			foreach (var o in Asset.Objectives)
			{
				if (o == null)
					continue;
				foreach (var next in o.NextObjectives)
				{
					list.Remove(next);
				}
			}
			return list.Where(o => !o.IsAddendum);
		}

		public override IEnumerable<string> GetMarkers(bool extended)
		{
			if (extended)
			{
				foreach (var xp in Asset.GetComponents<Experience>())
				{
					yield return xp.GetDescription();
				}
			}
		}

		protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
        {
            if (r is BlueprintQuestObjective objective && objective.IsAddendum)
				return null;

			if (typeof(BlueprintQuestObjective).IsAssignableFrom(type))
				return FindProperty("m_Objectives");
			return null;
		}

		public override void AddReferencedAsset(ScriptableObject asset)
        {
            var objective = BlueprintEditorWrapper.Unwrap<BlueprintQuestObjective>(asset);// asset as BlueprintQuestObjective;
			if (objective == null)
				return;
			objective.SetIsAddendum(false);
			Asset.LinkObjective(objective);
		}

		public override void RemoveReferencedAsset(ScriptableObject asset, bool move = false)
		{
            var objective = BlueprintEditorWrapper.Unwrap<BlueprintQuestObjective>(asset);// asset as BlueprintQuestObjective;
			if (objective == null)
				return;
			Asset.UnlinkObjective(objective);
		}
	}
}