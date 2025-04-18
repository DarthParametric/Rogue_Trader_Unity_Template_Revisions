using System.IO;
using System.Linq;
using Assets.Editor;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;
using UnityEngine;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Blueprints.ProjectView
{
	public static class BlueprintProjectViewContextMenu
	{
        public static bool IsDirectoryEmpty(string path)
        {
            return Directory.EnumerateFileSystemEntries(path).Empty();
        }
        
        public static void ShowContextMenu(FolderTreeView folderView)
        {
            var gm = new GenericMenu();

            string rootPath = folderView.GetSelectedPath();

            AddItemShowInExplorerByRootPath(gm, rootPath);

            gm.AddSeparator("");

            gm.AddItem(new GUIContent("Create folder"), false, () =>
            {
                var path = BlueprintsDatabase.FullToRelativePath(rootPath);
                string newfolder = "NewFolder"; // todo: add numbers if new folder already exists
                Directory.CreateDirectory(Path.Combine(path, newfolder));
                folderView.Reload();
            });

            gm.AddItem(new GUIContent("Delete"), false, folderView.DeleteSelection);

            gm.AddSeparator("");

            gm.AddItem(new GUIContent("Refresh window"), false, () => BlueprintProjectView.ReloadAll());

            gm.ShowAsContext();
        }

        private static void AddItemShowInExplorerByRootPath(GenericMenu gm, string rootPath) 
        {
            gm.AddItem(new GUIContent("Show in explorer"), false,
                () => System.Diagnostics.Process.Start("explorer.exe", "/open," + rootPath));
        }

        public static void ShowContextMenu(FileListView fileView, FileListItem fileListItem)
        {
            var gm = new GenericMenu();

            if (fileListItem != null)
            {
                gm.AddItem(new GUIContent("Show in explorer"), false,
                    () => System.Diagnostics.Process.Start("explorer.exe", "/select," + fileListItem.FullPath));
                gm.AddItem(new GUIContent("Open as file"), false, () => Application.OpenURL(fileListItem.FullPath));
            }
            else
            {
                AddItemShowInExplorerByRootPath(gm, fileView.RootPath);
            }
            
            gm.AddSeparator("");

            if (fileView.SearchPattern == "")
            {
                var mp = Event.current.mousePosition;
                gm.AddItem(new GUIContent("Create blueprint..."), false,
                    () =>
                    {
                        fileView.NextGui = () => { fileView.Owner.ShowBlueprintCreation(new Rect(mp, Vector2.one)); }; 
                        fileView.Owner.Repaint();
                    });
                
                gm.AddItem(new GUIContent("Create folder"), false, () =>
                {
                    var path = BlueprintsDatabase.FullToRelativePath(fileView.RootPath);
                    string newfolder = "NewFolder"; // todo: add numbers if new folder already exists
                    Directory.CreateDirectory(Path.Combine(path, newfolder));
                    fileView.Reload();
                    fileView.SelectAndRenameFolder(newfolder);
                });
            }

            gm.AddItem(new GUIContent("Rename"), false, fileView.RenameFromContextMenu);
            gm.AddItem(new GUIContent("Delete"), false, fileView.DeleteSelection);
            gm.AddItem(new GUIContent("Shadow Delete"), false, fileView.ShadowDeleteSelection);
            gm.AddItem(new GUIContent("Duplicate %D"), false, () => fileView.DuplicateSelection(false));
            gm.AddItem(new GUIContent("Smart Duplicate %#D"), false, () => fileView.DuplicateSelection(true));
            gm.AddItem(new GUIContent("Update localization comments"), false, () => LocalizationUtility.AddCommentsToJsons());

            gm.AddSeparator("");

            var selection = fileView.GetSelection().Select(fileView.GetItemBySelectionId).ToList();
            bool hasDirty = selection.Any(i => i!=null && BlueprintsDatabase.IsDirty(i.Id));

            if (hasDirty)
            {
                gm.AddItem(new GUIContent("Save"), false, () => selection.ForEach(i => BlueprintsDatabase.Save(i.Id)));
            }
            else
            {
                gm.AddItem(new GUIContent("Force Resave"), false, () => selection.ForEach(i => BlueprintsDatabase.Save(i.Id)));
            }

            if (hasDirty || selection.Any(i => i!=null && BlueprintsDatabase.IsChangedOnDisk(i.Id)))
            {
                gm.AddItem(new GUIContent("Discard"), false, () => selection.ForEach(i => BlueprintsDatabase.Discard(i.Id)));
            }

            gm.AddSeparator("");
            BlueprintContextMenu.AddItemsToMenu(gm, selection, fileListItem);

            gm.AddSeparator("");

            gm.AddItem(new GUIContent("Save all"), false, () => BlueprintsDatabase.SaveAllDirty());
            gm.AddItem(new GUIContent("Refresh window"), false, () => BlueprintProjectView.ReloadAll());

            gm.ShowAsContext();
        }
	}
}