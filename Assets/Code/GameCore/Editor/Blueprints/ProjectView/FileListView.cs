using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Editor;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Utility.CodeTimer;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Blueprints.ProjectView
{
    public class FileListView : TreeView
    {
        private string m_SearchPattern;
        private string m_SearchByName;
        private Type m_SearchByType;
        private readonly List<string> m_SearchByNameSplit = new();
        private TreeViewItem m_RenamingItem;

        private readonly List<FileListItem> m_ItemList = new();
        private readonly List<string> m_FolderNames = new();
        private string m_RootPath;

        private Texture2D m_FolderIcon;
        private bool m_HasSearch;

        private delegate void FrameType(int id, bool frame, bool ping);

        private readonly FrameType m_Frame;
        
        static FileListView()
        {
        }
        
        public FileListView(TreeViewState state) : base(state)
        {
            var ctrType = typeof(TreeView).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
            var ctrField = typeof(TreeView).GetField("m_TreeView", BindingFlags.Instance | BindingFlags.NonPublic);
            var pingMethod = ctrType.GetMethod("Frame", new[] { typeof(int), typeof(bool), typeof(bool) });
            m_Frame = (FrameType)pingMethod.CreateDelegate(typeof(FrameType), ctrField.GetValue(this));
        }

        public string SearchPattern
        {
            get => m_SearchPattern;
            set
            {
                if (m_SearchPattern == value) 
                    return;
                
                m_SearchPattern = value;
                state.searchString = SearchPattern;

                OnSearchUpdated();
            }
        }

        public string RootPath
        {
            get => m_RootPath;
            set
            {
                if (m_RootPath == value) 
                    return;
                
                //PFLog.Default.Log("Set root to "+value);
                EndRename();
                SetSelection(new List<int>());
                m_RootPath = value;
                Reload();
            }
        }

        private int m_SelectOnReloadId;
        private string m_SelectOnReloadName;

        public bool HasSearch
            => m_SearchByType != null || m_SearchByNameSplit.Count > 0;

        public event Action<string> OnFolderSelected;

        public BlueprintProjectView Owner { get; set; }
        public Action NextGui { get; set; }

        public bool IsSelected(FileListItem item)
        {
            return GetSelection().Any(i => i >= 0 && i < m_ItemList.Count && m_ItemList[i] == item);
        }

        private void OnSearchUpdated()
        {
            EndRename();
            ParseSearchPattern();
            if (m_SearchPattern == "")
            {
                var sel = GetSelection();
                if (sel.Count == 1 && sel[0] >= 0 && sel[0] < m_ItemList.Count)
                {
                    // if we had a blueprint selected when searching and then cleared search, make the whole window
                    // show that blueprint after clearing
                    var selectedItem = m_ItemList[sel[0]];
                    string rp = RootPath;
                    // open that blueprint's folder (this should reload the file view too
                    OnFolderSelected?.Invoke(Path.GetDirectoryName(selectedItem.FullPath));

                    if (RootPath == rp)
                    {
                        Reload(); // if tree was already at needed folder, reload files
                    }
                    
                    // select the blueprint
                    SetSelection(new List<int> { m_ItemList.FindIndex(i => i.Id == selectedItem.Id) });
                    return; // we've already reloaded all we wanted
                }
            }
            Reload();
        }

        private void ParseSearchPattern()
        {
            m_SearchByName = null;
            m_SearchByType = null;
            m_SearchByNameSplit.Clear();

            if (m_SearchPattern == null || m_SearchPattern.Length < 3)
            {
                return;
            }

            string[] split = m_SearchPattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in split)
            {
                if (s.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
                {
                    string typeName = s.Substring(2);
                    var type = typeName == nameof(SimpleBlueprint)
                        ? typeof(SimpleBlueprint)
                        : TypeCache.GetTypesDerivedFrom<SimpleBlueprint>().FirstOrDefault(t => t.Name == typeName || t.Name.ToLower() == typeName.ToLower());

                    if (type != null)
                    {
                        if (m_SearchByType != null)
                        {
                            PFLog.Default.Warning("Searching for multiple types not supported");
                        }
                        m_SearchByType = type;
                    }
                }
                else
                {
                    m_SearchByNameSplit.Add(s);
                }

                if (m_SearchByType != null || m_SearchByNameSplit.Count <= 0) continue;
                m_SearchByName = string.Join(" ", m_SearchByNameSplit);
                if (m_SearchByName.Length < 3)
                    m_SearchByName = null; // do not search for short patterns, too slow and not useful
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem(0, -1, "Blueprints");
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            m_FolderIcon = m_FolderIcon ? m_FolderIcon : EditorGUIUtility.FindTexture("Folder Icon");

            var rows = GetRows() ?? new List<TreeViewItem>(200);
            rows.Clear();
            m_ItemList.Clear();
            m_FolderNames.Clear();

            if (m_SearchByName != null || m_SearchByType != null)
            {
                BuildRowsForSearch(rows);
                return rows;
            }

            if (m_RootPath == null)
                return rows; // just in case

            string[] folders = Directory.GetDirectories(m_RootPath, "*", SearchOption.TopDirectoryOnly);
            for (int ii = 0; ii < folders.Length; ii++)
            {
                string folder = Path.GetFileName(folders[ii]);
                var folderItem = new TreeViewItem(-ii - 1, 0, folder) { icon = m_FolderIcon };
                rows.Add(folderItem);
                root.AddChild(folderItem);
                m_FolderNames.Add(folder);
            }

            string[] files = Directory.GetFiles(m_RootPath, "*.jbp", SearchOption.TopDirectoryOnly);
            for (int ii = 0; ii < files.Length; ii++)
            {
                string file = files[ii];
                var item = new FileListItem(file, this);
                m_ItemList.Add(item);

                var treeViewItem = new TreeViewItem(ii, 0, item.Name);
                rows.Add(treeViewItem);
                root.AddChild(treeViewItem);
            }

            if (m_SelectOnReloadName == null) 
                return rows;
            
            // unselect old
            var selectedIDs = GetSelection().ToList(); // GetSelection returns IList that's not an IList. Real classy, unity.
            selectedIDs.Remove(m_SelectOnReloadId);

            // select new
            int newId = m_SelectOnReloadId < 0
                ? -1 - m_FolderNames.IndexOf(m_SelectOnReloadName)
                : m_ItemList.FindIndex(i => i.Name == m_SelectOnReloadName);
            selectedIDs.Add(newId);
                    
            SetSelection(selectedIDs);

            m_SelectOnReloadName = null;

            return rows;
        }

        private void BuildRowsForSearch(IList<TreeViewItem> rows)
        {
            using (ProfileScope.New("BuildRowsForSearch"))
            {
                List<(string, string, bool, bool)> searchResult;
                if (m_SearchByType != null)
                {
                    // search by type and filter the result here
                    using (ProfileScope.New("SearchByType"))
                    {
                        searchResult = BlueprintsDatabase.SearchByType(m_SearchByType);
                    }
                    if (m_SearchByNameSplit.Count > 0)
                    {
                        string splits = "";
                        foreach (string split in m_SearchByNameSplit)
                        {
                            splits += " " + split;
                        }
                        List<(string, string, bool, bool)> searchSplits;
                        using (ProfileScope.New("SearchByType"))
                        {
                            searchSplits = BlueprintsDatabase.SearchByName(splits);
                        }

                        searchResult.RemoveAll(p => !searchSplits.Contains(p));
                    }
                }
                else if (m_SearchPattern.StartsWith("g:"))
                {
                    string id = m_SearchPattern.Substring(2, 32);
                    searchResult = new List<(string, string, bool, bool)> 
                        {(
                            id, 
                            BlueprintsDatabase.IdToPath(id), 
                            BlueprintsDatabase.GetMetaById(id).ShadowDeleted,
                            BlueprintsDatabase.IdToContainsShadowDeletedBlueprints(id)
                        )};
                }
                else
                {
                    // search by name only on the index side
                    using (ProfileScope.New("SearchByName"))
                        searchResult = BlueprintsDatabase.SearchByName(m_SearchByName);
                }

                m_ItemList.Capacity = searchResult.Count;

                for (int ii = 0; ii < searchResult.Count; ii++)
                {
                    using (ProfileScope.New("Single Item 1"))
                    {
                        var item = new FileListItem(searchResult[ii], this);
                        m_ItemList.Add(item);
                    }
                }

                using (ProfileScope.New("Sort"))
                    m_ItemList.Sort((a, b)
                        => string.Compare(a.Name, b.Name, StringComparison.Ordinal));


                for (int ii = 0; ii < m_ItemList.Count; ii++)
                {
                    rows.Add(new TreeViewItem(ii, 0, m_ItemList[ii].Name));
                }
            }
        }

        public override void OnGUI(Rect rect)
        {
            baseIndent = 2 - foldoutWidth; // hack to make foldouts "take no space"
            base.OnGUI(rect);
            
            NextGui?.Invoke();
            NextGui = null;
        }

        public bool HandleGUICommand()
        {
            if(m_RenamingItem!=null)
                return false;
            
            if (Event.current.type == EventType.KeyUp && HasFocus())
            {
                if (Event.current.keyCode == KeyCode.Delete)
                {
                    // delete blueprints
                    DeleteSelection();
                }
                if (Event.current.keyCode == KeyCode.D && Event.current.control)
                {
                    // duplicate blueprints
                    DuplicateSelection(Event.current.shift);
                }
                return true;
            }

            if (Event.current.type == EventType.ContextClick)
            {
                var s = GetSelection();
                int id = s?.Count == 1 ? s[0] : -1;
                BlueprintProjectViewContextMenu.ShowContextMenu(this,GetItemBySelectionId(id));
                return true;
            }
            
            return false;
        }

        public void DuplicateSelection(bool smart)
        {
            var selection = GetSelection();
            if (selection.Count == 0 || selection.All(i => i < 0))
                return;
            if (smart)
            {
                var blueprints = selection
                    .Where(i => i >= 0)
                    .Select(i => m_ItemList[i])
                    .Select(i => BlueprintsDatabase.LoadById<SimpleBlueprint>(i.Id))
                    .ToArray();
                BlueprintsDatabase.SmartDuplicateAssets(blueprints);
            }
            else
            {
                foreach (int idx in selection)
                {
                    if (idx >= 0)
                    {
                        var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(m_ItemList[idx].Id);
                        var dup = BlueprintsDatabase.DuplicateAsset(bp);
                        if (selection.Count == 1)
                        {
                            // select the duplicate on ctrl-d
                            Selection.activeObject = BlueprintEditorWrapper.Wrap(dup);
                        }
                    }
                }
            }

            EditorApplication.delayCall += Reload;
        }

        public void DeleteSelection()
        {
            var selection = GetSelection();
            if (selection.Count == 0)
                return;

            string name = selection.Count > 1
                ? "multiple blueprints"
                : selection[0] < 0
                    ? m_FolderNames[-selection[0] - 1]
                    : m_ItemList[selection[0]].Name;
            
            if (!EditorUtility.DisplayDialog("Delete blueprint", $"Are you sure you want to delete {name}?",
                "Yes", "No"))
                return;

            foreach (int idx in selection)
            {
                if (idx >= 0)
                {
                    BlueprintsDatabase.DeleteAsset(BlueprintsDatabase.LoadById<SimpleBlueprint>(m_ItemList[idx].Id));
                }
                else
                {
                    string folder = m_FolderNames[-idx - 1];
                    try
                    {
                        Directory.Delete(Path.Combine(RootPath, folder));
                    }
                    catch (IOException x)
                    {
                        EditorUtility.DisplayDialog("Cannot delete " + folder, x.Message, "OK");
                    }
                }
            }

            SetSelection(new List<int>());
            Reload();
        }

        public void ShadowDeleteSelection()
        {
            var selection = GetSelection();
            if (selection.Count == 0)
                return;

            var name = selection.Count > 1
                ? "multiple blueprints"
                : selection[0] < 0
                    ? m_FolderNames[-selection[0] - 1]
                    : m_ItemList[selection[0]].Name;
            if (!EditorUtility.DisplayDialog("Delete blueprint", "Are you sure you want to shadow-delete (mark as unavailable) " + name,
                "Yes", "No"))
                return;

            foreach (var idx in selection)
            {
                if (idx >= 0)
                {
                    var id = m_ItemList[idx].Id;
                    BlueprintsDatabase.LoadById<SimpleBlueprint>(id);
                    BlueprintsDatabase.GetMetaById(id).ShadowDeleted = true;
                    BlueprintsDatabase.Save(id);
                }
                else
                {
                    EditorUtility.DisplayDialog("Oops","Cannot shadow-delete folders", "OK");
                }
            }

            SetSelection(new List<int>());
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.isRenaming)
            {
                base.RowGUI(args);
                return;
            }

            var fileItem = args.item.id < 0 ? null : m_ItemList[args.item.id];
            args.label = (fileItem == null) ? args.label : "";
            base.RowGUI(args);

            // args.rowRect.x -= baseIndent;
            fileItem?.OnGUI(args.rowRect);

            if (fileItem != null)
            {
                BlueprintProjectView.RaiseOnItemGUI(args.rowRect, fileItem);
            }
        }

        //protected override void ContextClickedItem(int id)
        //{
        //    var fileItem = id < 0 ? null : m_ItemList[id];
        //    ShowContextMenu(fileItem);
        //    Event.current.Use();
        //}
        protected override void DoubleClickedItem(int id)
        {
            if (id < 0)
            {
                OnFolderSelected?.Invoke(RootPath + "\\" + m_FolderNames[-id - 1]);
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.Count == 0 || selectedIds.Count > 1)
            {
                // todo: we maybe CAN select multiple blueprints for multiedit.
                Selection.objects = null;
                return;
            }

            var fileItem = m_ItemList.Get(selectedIds[0]);
            if (fileItem != null)
            {
                var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(fileItem.Id);
                Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
            }

            // todo: follow selections in Unity?
        }

        protected override void SingleClickedItem(int id)
        {
            var fileItem = id < 0 ? null : m_ItemList[id];
            if (fileItem != null)
            {
                var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(fileItem.Id);
                Selection.activeObject = BlueprintEditorWrapper.Wrap(bp);
            }
        }

        public void RefreshSelection()
        {
            SelectionChanged(GetSelection());
        }
        
        protected override bool CanRename(TreeViewItem item)
        {
            m_RenamingItem = item;
            return true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            m_RenamingItem = null;
            
            if (!args.acceptedRename)
                return;

            if (args.itemID >= 0)
            {
                // rename blueprint
                string oldPath = m_ItemList[args.itemID].FullPath;
                string newPath = Path.Combine(Path.GetDirectoryName(oldPath), args.newName + ".jbp");
                if (args.originalName == args.newName && File.Exists(newPath))
                {
                    args.acceptedRename = false;
                    return;
                }
                // rename blueprint (this will trigger invalidates in BD)
                File.Move(oldPath, newPath);

                if (IsSelected(args.itemID))
                {
                    m_SelectOnReloadId = args.itemID;
                    m_SelectOnReloadName = args.newName;
                }
            }
            else
            {
                // rename folder
                //Debug.Log($"Move folder from {args.originalName} to {args.newName}");
                string newPath = Path.Combine(RootPath, args.newName);
                if (args.originalName == args.newName && Directory.Exists(newPath))
                {
                    args.acceptedRename = false;
                    return;
                }
                // rename folder (this will trigger invalidates in BD)
                string oldPath = Path.Combine(RootPath, args.originalName);
                Directory.Move(oldPath, newPath);

                if (IsSelected(args.itemID))
                {
                    m_SelectOnReloadId = args.itemID;
                    m_SelectOnReloadName = args.newName;
                }
            }
            // TODO: track blueprint renames for localization
            EditorApplication.delayCall += Reload;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var dragged = new List<Object>(args.draggedItemIDs.Count);

            foreach (int id in args.draggedItemIDs)
            {
                if (id < 0)
                {
                    string title = FindItem(id, rootItem).displayName;
                    string folderPath = Path.Combine(RootPath, title);
                    dragged.Add(ScriptableObject.CreateInstance<FolderWrapper>().Setup(folderPath, title));
                    continue;
                }

                var item = m_ItemList[id];
                string fullPath = item.Id;
                var bp = BlueprintsDatabase.LoadById<SimpleBlueprint>(fullPath);
                if (bp)
                {
                    dragged.Add(BlueprintEditorWrapper.Wrap(bp));
                }
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
            if(args.dragAndDropPosition==DragAndDropPosition.UponItem && DragAndDrop.objectReferences.All(o=>o is FolderWrapper))
            {
                if (!args.performDrop)
                {
                    return Event.current.control?DragAndDropVisualMode.Copy: DragAndDropVisualMode.Move; 
                }

                var item = args.parentItem;
                

                foreach (var folder in DragAndDrop.objectReferences.OfType<FolderWrapper>())
                {
                    string targetPath = Path.Combine(RootPath, item.displayName);
                    string targetFolderName = folder.name.Replace(folder.Path, "");
                    targetFolderName = targetFolderName.Substring(1, targetFolderName.Length-1);

                    try
                    {
                        Directory.Move(folder.name, Path.Combine(targetPath, targetFolderName));
                        
                    }
                    catch
                    {
                        EditorUtility.DisplayDialog("Drag an drop", "Please, make sure that the target location and the original location are not the same and item names are different.", "Ok");
                    }
                }
                BlueprintProjectView.ReloadAll();
                return DragAndDropVisualMode.Move;
            }

            return DragAndDropVisualMode.Rejected;
        }
        
        public void RenameFromContextMenu()
        {
            int selectedItem = GetSelection().First();
            CanRename(FindItem(selectedItem, rootItem));
            BeginRename(m_RenamingItem, 0.05f);
        }

        public void SelectAndRename(string guid)
        {
            int idx = m_ItemList.FindIndex(i => i.Id == guid);
            if (idx >= 0)
            {
                SetSelection(new List<int> { idx });
                BeginRename(FindItem(idx, rootItem), 0.05f);
            }
        }
        public void SelectAndRenameFolder(string f)
        {
            int folderIndex = m_FolderNames.IndexOf(f);
            if (folderIndex>=0)
            {
                int id = -folderIndex - 1;
                SetSelection(new List<int> { id });
                var fi = FindItem(id, rootItem);
                BeginRename(fi, 0.05f);
            }
        }

        public void Select(string guid, bool ping)
        {
            int idx = m_ItemList.FindIndex(i => i.Id == guid);
            if (idx >= 0)
            {
                if (!GetSelection().Contains(idx))
                {
                    SetSelection(new List<int> {idx});
                }

                if (ping)
                {
                    m_Frame(idx, true, true);
                }
                else
                {
                    FrameItem(idx);
                }
            }
        }

        public bool IsShowing(string id)
        {
            return m_ItemList.HasItem(i=>i.Id==id);
        }

        public FileListItem GetItemBySelectionId(int arg)
        {
            return arg < 0 ? null : m_ItemList[arg];
        }
    }
}