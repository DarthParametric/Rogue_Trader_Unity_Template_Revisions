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
    public class EncyclopediaChapterNode : EditorNode<BlueprintEncyclopediaChapter>
    {
        public EncyclopediaChapterNode(Graph graph, BlueprintEncyclopediaChapter asset) : base(graph, asset, new Vector2(200, 50))
        {
            
        }
        public override EditorNode AddVirtualChild(EditorNode referencedNode)
        {
            return null;
        }
        protected override void DrawContent()
        {
            SerializedObject.Update();

#if UNITY_EDITOR && EDITOR_FIELDS
            GUILayout.Label("Title");
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
