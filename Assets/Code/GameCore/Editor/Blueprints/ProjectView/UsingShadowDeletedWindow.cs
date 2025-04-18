using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;
using UnityEngine;

namespace RogueTrader.Editor.Blueprints.ProjectView
{
	public class UsingShadowDeletedWindow : EditorWindow
	{
		public class HierarchyItem
		{
			public HierarchyItem Parent;
			public string AssetGuid;
			public string Path;
			public string DisplayName;
			public List<string> ShadowDeletedGuids;
			public List<HierarchyItem> Children;
			public bool Expanded = true;
			public bool Hidden;
			
			public SimpleBlueprint Asset
				=> BlueprintsDatabase.LoadById<SimpleBlueprint>(AssetGuid);
			
			public int CompareTo(HierarchyItem other)
			{
				if (Children == null && other.Children != null)
				{
					return 1;
				}
				if (Children != null && other.Children == null)
				{
					return -1;
				}
				return string.Compare(Path, other.Path, StringComparison.Ordinal);
			}
		}
		
		private static GUIStyle s_SelectedAssetStyle;
		private static GUIStyle s_AssetStyle;
		private static GUIStyle s_FolderStyle;
		
		private readonly HashSet<string> m_FoldedPaths = new();
		
		private Vector2 m_Scroll;
		private float m_CurrentLine;
		private float m_CurrentIndent;
		private string m_SelectedGuid;
		private List<HierarchyItem> m_LoadedAssets;
		
		private static void InitGUIStyles()
		{
			try
			{
				if (s_SelectedAssetStyle == null)
				{
					s_SelectedAssetStyle = new GUIStyle(EditorStyles.label);
					s_SelectedAssetStyle.normal = s_SelectedAssetStyle.focused;
					s_SelectedAssetStyle.active = s_SelectedAssetStyle.focused;
					s_SelectedAssetStyle.onNormal = s_SelectedAssetStyle.focused;
					s_SelectedAssetStyle.onActive = s_SelectedAssetStyle.focused;
				}

				if (s_AssetStyle == null)
				{
					s_AssetStyle = new GUIStyle(EditorStyles.label);
					s_AssetStyle.active = s_AssetStyle.normal;
					s_AssetStyle.focused = s_AssetStyle.normal;
					s_AssetStyle.onActive = s_AssetStyle.normal;
					s_AssetStyle.onFocused = s_AssetStyle.normal;
				}

				if (s_FolderStyle == null)
				{
					s_FolderStyle = new GUIStyle(EditorStyles.foldout);
					s_FolderStyle.focused = s_FolderStyle.normal;
					s_FolderStyle.active = s_FolderStyle.normal;
					s_FolderStyle.hover = s_FolderStyle.normal;
					s_FolderStyle.onFocused = s_FolderStyle.normal;
					s_FolderStyle.onActive = s_FolderStyle.normal;
					s_FolderStyle.onHover = s_FolderStyle.normal;
				}
			}catch(NullReferenceException)
			{
				// this just happens sometimes because apparently EditorStyles does not get initialized immediately
			}
		}

		private void UpdateAssetList()
		{
			if (DatabaseServerConnector.ClientInstance == null)
			{
				return;
			}
			
			m_LoadedAssets = new List<HierarchyItem>();

			var ids = BlueprintsDatabase.ListOfContainsShadowDeletedBlueprintWithoutIsShadowDeleted;
			foreach (var pair in ids)
			{
				AddAssetInfo(pair.Item1, pair.Item2);
			}
		}

		private void AddAssetInfo(string guid, string path)
		{
			var folders = path.Split('/','\\');
			var list = m_LoadedAssets;
			HierarchyItem parent = null;
			for (int ii = 1; ii < folders.Length - 1; ii++) // 1 to skip Blueprints folder, -1 to skip asset name
			{
				var folder = folders[ii];
				var p = parent;
				parent = list.SingleOrDefault(e => e.DisplayName == folder && e.Children != null);
				if (parent == null)
				{
					parent = new HierarchyItem
					{
						Parent = p,
						DisplayName = folder,
						Path = string.Join("/", folders, 0, ii + 1),
					};
					list.Add(parent);
					parent.Expanded = !m_FoldedPaths.Contains(parent.Path);
				}
				list = parent.Children ??= new List<HierarchyItem>();
			}
			var hierarchyEntry = new HierarchyItem
			{
				Parent = parent,
				DisplayName = Path.GetFileNameWithoutExtension(path),
				Path = path,
				AssetGuid = guid,
				Children = null
			};
			list.Add(hierarchyEntry);
		}

		private void OnEnable()
		{
			string folded = EditorPrefs.GetString("US_Folded_Paths", "");
			m_FoldedPaths.UnionWith(folded.Split('\n'));
			
			UpdateAssetList();

			InitGUIStyles();
		}

		private void OnDisable()
		{
			EditorPrefs.SetString("US_Folded_Paths", string.Join("\n", m_FoldedPaths.ToArray()));
		}

		private void DisplayList(List<HierarchyItem> list)
		{
			if (list == null)
			{
				return;
			}
			
			foreach (var entry in list)
			{
				if (entry.Hidden)
				{
					continue;
				}

				if (entry.Children == null)
				{
					DisplayAsset(entry);
				}
				else
				{
					// this is a folder
					Rect rect = new Rect(m_CurrentIndent, m_CurrentLine, position.width - m_CurrentIndent, EditorGUIUtility.singleLineHeight);
					m_CurrentLine += rect.height;
                        
					bool ex = EditorGUI.Foldout(rect, entry.Expanded, entry.DisplayName, true, s_FolderStyle);
					if (ex != entry.Expanded)
					{
						if (ex)
						{
							m_FoldedPaths.Remove(entry.Path);
						}
						else
						{
							m_FoldedPaths.Add(entry.Path);
						}
						entry.Expanded = ex;
					}
					
					if (entry.Expanded)
					{
						{
							m_CurrentIndent += 10;
							DisplayList(entry.Children);
							m_CurrentIndent -= 10;
						}
					}
				}
			}
		}
		
		private void DisplayAsset(HierarchyItem entry)
        {
            Rect rect = new Rect(m_CurrentIndent, m_CurrentLine, position.width - m_CurrentIndent, EditorGUIUtility.singleLineHeight);
            m_CurrentLine += rect.height;
            rect.xMin += 16;

            if (rect.yMax < m_Scroll.y)
                return;
            if (rect.yMin - m_Scroll.y > position.height)
                return;

            bool selected = m_SelectedGuid == entry.AssetGuid;
            var style = selected ? s_SelectedAssetStyle : s_AssetStyle;
            if (GUI.Button(rect, entry.DisplayName, style))
            {
	            m_SelectedGuid = entry.AssetGuid;
	            Selection.activeObject = BlueprintEditorWrapper.Wrap(entry.Asset);
            }
        }
		
		private void OnGUI()
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(false)))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false)))
					{
						OnDisable();
						BlueprintsDatabase.ListOfContainsShadowDeletedBlueprintWithoutIsShadowDeleted = null;
						OnEnable();
					}
				}
			}
			
			using (var scope = new EditorGUILayout.ScrollViewScope(m_Scroll, GUI.skin.box, GUILayout.ExpandHeight(true)))
			{
				m_Scroll = scope.scrollPosition;
				m_CurrentLine = 0;
				m_CurrentIndent = 0;
					
				DisplayList(m_LoadedAssets);
					
				GUILayoutUtility.GetRect(0, 0, m_CurrentLine, m_CurrentLine);
			}
		}
	}
}