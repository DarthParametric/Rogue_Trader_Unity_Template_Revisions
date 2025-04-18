using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.NodeEditor.Nodes
{
	public class CueSequenceNode : EditorNode<BlueprintCueSequence>
	{
		public CueSequenceNode(Graph graph, BlueprintCueSequence asset) : base(graph, asset, new Vector2(200, 100))
		{
		}

		public override Color GetWindowColor()
		{
			return Colors.CueSequenceWindow;
		}

		protected override void DrawContent()
		{
			GUILayout.Space(100);
		}

		public override IEnumerable<string> GetMarkers(bool extended)
		{
			if (Asset.ShowOnce)
				yield return "Once";
			if (Asset.Conditions.HasConditions)
				yield return ElementsDescription.Conditions(extended, Asset.Conditions);
		}

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
		{
			return Asset.Cues.Dereference()
                .Cast<SimpleBlueprint>()
				.Concat(new[] {Asset.Exit});
		}

		protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
		{
			if (typeof(BlueprintCueBase).IsAssignableFrom(type))
				return FindProperty("Cues");
			return null;
		}

		public override bool CanAddReference(Type type, SimpleBlueprint r = null)
		{
			if (type == typeof(BlueprintSequenceExit))
				return Asset.Exit == null;

			return (base.CanAddReference(type, r));
		}

		public override void AddReferencedAsset(ScriptableObject asset)
		{
			if (asset.GetWrappedType() == typeof(BlueprintSequenceExit))
			{
				using (GuiScopes.UpdateObject(GetSerializedObject()))
                {
					BlueprintReferenceBase.SetPropertyValue(FindProperty("m_Exit"),
						BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(asset));
                }
				return;
			}

			base.AddReferencedAsset(asset);
		}

		public override void RemoveReferencedAsset(ScriptableObject asset, bool move = false)
		{
			if (Asset.Exit == BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(asset))
			{
				using (GuiScopes.UpdateObject(GetSerializedObject()))
				{
					FindProperty("m_Exit.guid").stringValue = "";
				}
				return;
			}

			base.RemoveReferencedAsset(asset);
		}
	}
}