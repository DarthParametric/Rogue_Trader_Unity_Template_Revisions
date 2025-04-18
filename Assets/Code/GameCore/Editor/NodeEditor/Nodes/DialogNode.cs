using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.NodeEditor.Nodes
{
	public class DialogNode : EditorNode<BlueprintDialog>
	{
		public DialogNode(Graph graph, BlueprintDialog asset) : base(graph, asset, new Vector2(100, 50))
		{
		}

		protected override void DrawContent()
		{
			GUILayout.Label(Asset.AssetName);
		}

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
		{
			return Asset.FirstCue.Cues.Dereference();
		}

        protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
		{
			if (typeof(BlueprintCueBase).IsAssignableFrom(type))
				return FindProperty("FirstCue.Cues");
			return null;
		}

		public override IEnumerable<string> GetMarkers(bool extended)
		{
			if (Asset.Conditions.HasConditions)
				yield return ElementsDescription.Conditions(extended, Asset.Conditions);
			if (Asset.ReplaceActions.HasActions)
				yield return ElementsDescription.Actions(extended, "Replace Actions", 0, Asset.ReplaceActions);
			if (Asset.StartActions.HasActions)
				yield return ElementsDescription.Actions(extended, "Start Actions", 0, Asset.StartActions);
			if (Asset.FinishActions.HasActions)
				yield return ElementsDescription.Actions(extended, "Finish Actions", 0, Asset.FinishActions);
		}
	}
}