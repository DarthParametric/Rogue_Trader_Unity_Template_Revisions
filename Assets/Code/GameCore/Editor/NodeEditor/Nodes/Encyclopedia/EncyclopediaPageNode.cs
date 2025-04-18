using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Encyclopedia;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.Localization;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.NodeEditor.Nodes.Encyclopedia
{
    public class EncyclopediaPageNode : EditorNode<BlueprintEncyclopediaPage>
    {
        public EncyclopediaPageNode(Graph graph, BlueprintEncyclopediaPage asset) : base(graph, asset, new Vector2(200, 50))
        {            
        }

		public override bool CanBeShared()
		{
			return false;
		}

		public override EditorNode AddVirtualChild(EditorNode referencedNode)
        {
            return null;
        }

        protected override void DrawContent()
        {
            SerializedObject.Update();

            GUILayout.Label("Title");
#if UNITY_EDITOR && EDITOR_FIELDS
            var title = FindProperty("Title");
            LocalizationEditorGUI.LocalizedStringField(title, Asset.Title, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);
#endif

            SerializedObject.ApplyModifiedProperties();
        }

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
        {
            return Asset.ChildPages.Select(p => p.Get()).ToList();
		}
        
        protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
        {
			if (type == typeof(BlueprintEncyclopediaPage))
				return FindProperty("ChildPages");
			return null;
        }
	}
}
