﻿using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.Editor.NodeEditor.Window;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.NodeEditor.Nodes
{
	public class CueSequenceExitNode : EditorNode<BlueprintSequenceExit>
	{
		public CueSequenceExitNode(Graph graph, BlueprintSequenceExit asset) : base(graph, asset, new Vector2(200, 40))
		{
		}

		public override Color GetWindowColor()
		{
			return Colors.CueSequenceWindow;
		}

		protected override void DrawContent()
		{
			GUILayout.Space(40);
		}

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
		{
			return Asset.Answers.Dereference()
                .Cast<SimpleBlueprint>()
				.Concat(Asset.Continue.Cues.Dereference());
		}
        
		protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
		{
			if (typeof(BlueprintCueBase).IsAssignableFrom(type))
				return FindProperty("Continue.Cues");
			if (typeof(BlueprintAnswerBase).IsAssignableFrom(type))
				return FindProperty("Answers");
			return null;
		}
	}
}