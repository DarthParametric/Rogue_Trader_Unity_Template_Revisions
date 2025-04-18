using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Editor.Scene
{
    public class SceneViewerTreeView : TreeView
    {
        public delegate void OnDoubleClickSceneDelegate(string sceneName, bool additive);
        public event OnDoubleClickSceneDelegate OnDoubleClickScene;

        public SceneViewerTreeView(TreeViewState state) : base(state) =>
            SetSceneList(new[] { "Loading scene list..." });

        public void SetSceneList(IEnumerable<string> scenes)
        {
            if (!_scenes.SequenceEqual(scenes))
            {
                _root = null; // reset cache
            }

            _scenes = scenes;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            if (_root != null)
            {
                return _root; // cached
            }

            _nextId = 0;
            _root = new() { id = _nextId++, depth = -1, displayName = "Root" };

            foreach (string scene in _scenes.OrderBy(x => x))
            {
                AddSceneToTree(_root, scene);
            }

            SetupDepthsFromParentsAndChildren(_root);
            return _root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            IList<TreeViewItem> rows = base.BuildRows(root);

            foreach (TreeViewItem item in rows)
            {
                item.displayName = hasSearch ? _nodeFullNames[item.id] : _nodePartialNames[item.id];
            }

            return rows;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search) =>
            _nodeFullNames[item.id].IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        protected override void DoubleClickedItem(int id) =>
            OnDoubleClickScene?.Invoke(_nodeFullNames[id], false);

        protected override void ContextClickedItem(int id)
        {
            var gm = new GenericMenu();
            
            //TODO: may be add item to show path to bundle containing scene?
            
            gm.AddItem(new GUIContent("Load Additive"), false, () =>
            {
                OnDoubleClickScene?.Invoke(_nodeFullNames[id], true);
            });
            gm.ShowAsContext();
        }

        private TreeViewItem _root;
        private readonly Dictionary<int, string> _nodePartialNames = new();
        private readonly Dictionary<int, string> _nodeFullNames = new();
        private IEnumerable<string> _scenes = Enumerable.Empty<string>();
        private int _nextId;

        private void AddSceneToTree(TreeViewItem parent, string scene)
        {
            string[] segments = scene.Split('_');
             _ = segments.Aggregate(parent, FindOrCreateChild);
             SetIconForLeaf(parent);
        }

        private void SetIconForLeaf(TreeViewItem item)
        {
            if (!item.hasChildren)
            {
                item.icon = (Texture2D)EditorGUIUtility.IconContent("d_SceneAsset Icon").image;
            }
            else
            {
                foreach (TreeViewItem child in item.children)
                {
                    SetIconForLeaf(child);
                }
            }
        }

        private TreeViewItem FindOrCreateChild(TreeViewItem parent, string segment)
        {
            TreeViewItem child = parent
                .children?
                .FirstOrDefault(c => c.displayName == segment);

            if (child == null)
            {
                child = new()
                {
                    id = _nextId++,
                    depth = parent.depth + 1,
                    displayName = segment
                };

                _nodePartialNames[child.id] = segment;
                _nodeFullNames[child.id] = _nodeFullNames.TryGetValue(parent.id, out string parentName) ? $"{parentName}_{segment}" : segment;

                parent.AddChild(child);
            }

            return child;
        }
    }
}
