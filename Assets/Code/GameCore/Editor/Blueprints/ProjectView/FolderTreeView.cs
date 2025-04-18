using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Utility.DotNetExtensions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.ProjectView
{
    public class FolderTreeView : TreeView
    {
        private Texture2D m_FolderIcon;
        private string m_RenamingPath;
        private TreeViewItem m_RenamingItem;

        public FolderTreeView(TreeViewState state) : base(state)
        {
        }
        
        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem(0, -1, "Blueprints");
        }

        protected override IList<int> GetDescendantsThatHaveChildren(int id)
        {
            Stack<string> stack = new Stack<string>();

            string start = FindItem(id, rootItem).displayName;
            stack.Push(start);

            var parents = new List<int>();
            while (stack.Count > 0)
            {
                string current = stack.Pop();
                parents.Add(current.GetHashCode());
                string[] subs = Directory.GetDirectories(current);
                for (int i = 0; i < subs.Length; ++i)
                {
                    string sub = subs[i];
                    if (Directory.GetDirectories(sub).Length > 0)
                        stack.Push(sub);
                }
            }

            return parents;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_FolderIcon = m_FolderIcon ? m_FolderIcon : EditorGUIUtility.FindTexture("Folder Icon");

            var rows = GetRows() ?? new List<TreeViewItem>(200);

            DirectoryInfo di = new DirectoryInfo(BlueprintsDatabase.DbPathPrefix);

            rows.Clear();
            
            // the Blueprints folder itself should be a visible item
            var root2 = new TreeViewItem(di.FullName.GetHashCode(), 0, di.FullName);
            rows.Add(root2);
            root.AddChild(root2);
            
            AddChildrenRecursive(di, root2, rows);

            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }

        private void AddChildrenRecursive(DirectoryInfo dir, TreeViewItem root, IList<TreeViewItem> rows, DirectoryInfo[] subDirs = null)
        {
            //PFLog.Default.Log("Add children to "+root.displayName);
            if (subDirs != null)
            {
                root.children = new List<TreeViewItem>(subDirs.Length);
            }
            else
            {
                subDirs = dir.GetDirectories("*", SearchOption.TopDirectoryOnly);
            }

            for (int ii = 0; ii < subDirs.Length; ++ii)
            {
                var subDir = subDirs[ii];
                var item = CreateTreeViewItemForDirectory(subDir);
                root.AddChild(item);
                rows.Add(item);

                var suSubDirs = subDir.GetDirectories("*", SearchOption.TopDirectoryOnly);
                if (suSubDirs.Length > 0)
                {
                    if (IsExpanded(item.id))
                    {
                        AddChildrenRecursive(subDir, item, rows, suSubDirs);
                    }
                    else
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }
            }
        }

        public TreeViewItem CreateTreeViewItemForDirectory(DirectoryInfo subDir)
        {
            return new TreeViewItem(subDir.FullName.GetHashCode(), -1, subDir.FullName)
            {
                icon = m_FolderIcon
            };
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.isRenaming)
            {
                base.RowGUI(args);
                return;
            }

            if (args.item == m_RenamingItem)
            {
                args.item.displayName = m_RenamingPath;
            }

            args.label = args.item.depth > 0 ? Path.GetFileName(args.item.displayName) : "Blueprints";
            base.RowGUI(args);
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count != 1)
                return;

            var item = FindItem(selectedIds[0], rootItem);
            string fullPath = item.displayName;
            
//            PFLog.Default.Log($"Selected id {selectedIds[0]} item {item.displayName}");

            OnSelectionChanged?.Invoke(fullPath);
        }

        public string GetSelectedPath()
        {
            var selection = GetSelection();
            if (selection.Count != 1)
                return null;
            var item = FindItem(selection[0], rootItem);
            return item?.displayName;
        }

        public void OpenPath(string fullPath, bool alsoSelect = true)
        {
            string relPath = BlueprintsDatabase.FullToRelativePath(fullPath).Replace('/',Path.DirectorySeparatorChar);
            string[] folders = relPath.Split(Path.DirectorySeparatorChar);
            var root = rootItem.children[0];
            for (int ii = 1; ii < folders.Length; ii++) // skip first folder, it's "Blueprints"
            {
                if (!IsExpanded(root.id))
                {
                    SetExpanded(root.id, true);
                }
                if (root.children!=null && root.children.Count == 1 && root.children[0] == null)
                {
                    // load more
                    Reload(); // todo: we should actually make reload force-expand if we know in advance what we need
                    root = FindItem(root.id, rootItem);
                }

                foreach (var child in root.children.EmptyIfNull())
                {
                    if(child?.displayName==null)
                        continue;
                    if (Path.GetFileName(child.displayName) == folders[ii])
                    {
                        root = child;
                        //PFLog.Default.Log($"Move to child {child.displayName} it has {child.children?.Count} children");
                        break;
                    }
                }
            }

            if (alsoSelect)
                SetSelection(new List<int>{root.id},
                    TreeViewSelectionOptions.FireSelectionChanged | TreeViewSelectionOptions.RevealAndFrame);

            //DirectoryInfo di = new DirectoryInfo(BlueprintsDatabase.DbPathPrefix);
        }
        
        protected override bool CanRename(TreeViewItem item)
        {
            return false; // do we even need renaming in the left pane?
            // hack: we use displayName to store full path, but we only want to show the name part in rename
            //m_RenamingPath = item.displayName;
            //m_RenamingItem = item;
            //item.displayName = Path.GetFileNameWithoutExtension(item.displayName);
            //return true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = FindItem(args.itemID, rootItem);
            item.displayName = m_RenamingPath;
            m_RenamingPath = null;
            m_RenamingItem = null;

            if (args.acceptedRename)
            {
                // rename folder
                Debug.Log($"Move folder from {item.displayName} to {args.newName}");
                var newPath = Path.Combine(Path.GetDirectoryName(item.displayName), args.newName);
                if (Directory.Exists(newPath))
                {
                    args.acceptedRename = false;
                    return;
                }

                // rename folder (this will trigger invalidates in BD)
                // todo: [bp] callback on moving blueprints
                Directory.Move(item.displayName, newPath);
                item.displayName = newPath;
                Reload();
            }
        }

        //protected override bool CanStartDrag(CanStartDragArgs args)
        //{
        //    // todo: drag folders around to move them? Do we even need this?
        //    return false;//args.draggedItem.displayName.EndsWith(".jbp") && args.draggedItemIDs.Count == 1;
        //}

        public event Action<string> OnSelectionChanged;

       public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            
            HandleGUICommand();
        }

        public bool HandleGUICommand()
        {
            if(m_RenamingItem!=null)
                return false;

            if (Event.current is {type: EventType.KeyUp, keyCode: KeyCode.Delete} && HasFocus())
            {
                // delete folders
                DeleteSelection();
                return true;
            }

            if (Event.current.type == EventType.ContextClick)
            {
                BlueprintProjectViewContextMenu.ShowContextMenu(this);
                return true;
            }

            return false;
        }

        public void DeleteSelection()
        {
            string rootPath = GetSelectedPath();

            if (!BlueprintProjectViewContextMenu.IsDirectoryEmpty(rootPath) && !EditorUtility.DisplayDialog("Delete folder", "Folder is not empty. Are you sure you want to delete it?",
                    "Yes", "No"))
            {
                return;
            } 
            
            Directory.Delete(rootPath, true);
            
            SetSelection(new List<int>());
            Reload();
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var dragged = new List<UnityEngine.Object>(args.draggedItemIDs.Count);

            foreach (int id in args.draggedItemIDs)
            {
                string parentPath = FindItem(id, rootItem).parent.displayName;
                string folderPath = FindItem(id, rootItem).displayName;
                dragged.Add(ScriptableObject.CreateInstance<FolderWrapper>().Setup(parentPath, folderPath));
            }

            if (dragged.Count > 0)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = dragged.ToArray();
                string title = dragged[0].name;
                DragAndDrop.StartDrag(title);
            }
        }
        
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if(args.dragAndDropPosition==DragAndDropPosition.UponItem && (DragAndDrop.objectReferences.Contains(o=>o is BlueprintEditorWrapper) || DragAndDrop.objectReferences.Contains(o=>o is FolderWrapper)))
            {
                if (!args.performDrop)
                {
                    return Event.current.control?DragAndDropVisualMode.Copy: DragAndDropVisualMode.Move; 
                }

                var item = args.parentItem;
                string fullTargetPath = item.displayName;

                foreach (var folder in DragAndDrop.objectReferences.OfType<FolderWrapper>())
                {
                    try
                    {
                        Directory.Move(folder.Path, Path.Combine(fullTargetPath, folder.name));
                    }
                    catch
                    {
                        EditorUtility.DisplayDialog("Drag an drop", "Please, make sure that the target location and the original location are not the same and item names are different.", "Ok");
                    }
                }

                var bps = DragAndDrop.objectReferences
                    .OfType<BlueprintEditorWrapper>()
                    .Select(w => w.Blueprint)
                    .ToArray();
                
                foreach (var bp in bps)
                {
                    PFLog.Default.Log($"dropped: {bp}");
                }

                string relativeTargetPath = BlueprintsDatabase.FullToRelativePath(fullTargetPath);
                
                if (Event.current.control)
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Move here"), false,
                        () => { BlueprintsDatabase.MoveAssets(bps, relativeTargetPath); });
                    menu.AddItem(new GUIContent("Copy here"), false,
                        () => { BlueprintsDatabase.CopyAssets(bps, relativeTargetPath); });
                    menu.AddItem(new GUIContent("Smart duplicate here"), false,
                        () => { BlueprintsDatabase.SmartDuplicateAssets(bps, relativeTargetPath); });
                    menu.ShowAsContext();
                }
                else
                {
                    BlueprintsDatabase.MoveAssets(bps, relativeTargetPath);
                }

                return DragAndDropVisualMode.Move;
            }

            return DragAndDropVisualMode.Rejected;
        }
    }
}