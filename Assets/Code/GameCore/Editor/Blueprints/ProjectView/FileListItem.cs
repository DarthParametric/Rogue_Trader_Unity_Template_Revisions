using System;
using System.IO;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Utility.CodeTimer;
using Owlcat.Editor.Utility;
using Owlcat.QA.Validation;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.ProjectView
{
    public class FileListItem
    {
        private readonly string m_FullPath;
        private string m_Name;
        private string m_Id;
        private GUIContent m_Content = new GUIContent();
        private Type m_Type;
        private FileListView m_Owner;
        private bool m_IsShadowDeleted;
        private bool m_ContainsShadowDeletedBlueprints;
        private string m_DisplayName;

        private static Texture2D s_ErrorIcon;
        private static GUIStyle s_Style;

        public FileListItem(string fullPath, FileListView owner)
        {
            m_FullPath = fullPath.Replace("/", "\\"); // only \ works for BlueprintDatabase
            m_Owner = owner;
            m_Name = Path.GetFileNameWithoutExtension(fullPath);
            var relativePath = BlueprintsDatabase.FullToRelativePath(fullPath);
            using (ProfileScope.New("PathToId"))
            {
                m_Id = BlueprintsDatabase.PathToId(relativePath);
                m_IsShadowDeleted = BlueprintsDatabase.GetMetaById(m_Id).ShadowDeleted;
                m_ContainsShadowDeletedBlueprints = BlueprintsDatabase.IdToContainsShadowDeletedBlueprints(m_Id);
            }

            m_DisplayName = m_IsShadowDeleted 
                ? $"<color=#ff0000ff>{m_Name}</color>" 
                : m_ContainsShadowDeletedBlueprints 
                    ? $"<color=#ffa500ff>{m_Name}</color>" 
                    : m_Name;
        }
        public FileListItem((string id, string path, bool isShadowDeleted, bool containsShadowDeletedBlueprints) searchResult, FileListView owner)
        {
            m_FullPath = BlueprintsDatabase.RelativeToFullPath(searchResult.path);
            m_Owner = owner;
            m_Name = Path.GetFileNameWithoutExtension(m_FullPath);
            m_Id = searchResult.id;
            m_IsShadowDeleted = searchResult.isShadowDeleted;
            m_ContainsShadowDeletedBlueprints = searchResult.containsShadowDeletedBlueprints;
            m_DisplayName = m_IsShadowDeleted 
                ? $"<color=#ff0000ff>{m_Name}</color>" 
                : m_ContainsShadowDeletedBlueprints 
                    ? $"<color=#ffa500ff>{m_Name}</color>" 
                    : m_Name;
        }
        
        private void InitContent()
        {
            if(m_Content.text!="")
                return;
            
            m_Type = string.IsNullOrEmpty(m_Id) ? null : BlueprintsDatabase.GetTypeById(m_Id);
            m_Content.text = m_Name;

            string name = "BlueprintIcons/" + m_Type?.Name + ".png";
            var icon = EditorGUIUtility.Load(name) as Texture2D;
            m_Content.image = icon.Or(OwlcatEditorStyles.Instance.BlueprintItemIcon);
            
            s_Style ??= new GUIStyle(EditorStyles.label)
            {
                richText = true
            };
        }

        public string Name
            => m_Name;

        public string Id
            => m_Id;

        public string FullPath
            => m_FullPath;

        public bool IsShadowDeleted
            => m_IsShadowDeleted;

        public bool ContainsShadowDeletedBlueprints
            => m_ContainsShadowDeletedBlueprints;
        
        public void OnGUI(Rect rect)
        {
            InitContent();
            
            var changed = BlueprintsDatabase.IsChangedOnDisk(m_Id);
            var dirty = BlueprintsDatabase.IsDirty(m_Id);

            Rect position = rect;
            position.width = 16;
            Texture effectiveIcon = m_Content.image;
            bool hasValidationError = BlueprintsDatabase.GetCached(Id)?.HasValidationError() ?? false;
            if (hasValidationError)
            {
                s_ErrorIcon = s_ErrorIcon ? s_ErrorIcon : EditorGUIUtility.FindTexture("d_console.erroricon.sml");
                effectiveIcon = s_ErrorIcon;
            }
            if (effectiveIcon != null)
            {
                GUI.DrawTexture(position, effectiveIcon, ScaleMode.ScaleToFit);
                rect.xMin += 18;
            }

            string name = m_Type == null ? "??" + m_DisplayName : m_DisplayName;
            s_Style.fontStyle = m_Owner.IsSelected(this) ? FontStyle.Bold : FontStyle.Normal;
            
            GUI.Label(rect, name, s_Style);

            {
                var statusPos = new Rect(rect.xMin - 5, rect.yMin, 10, 16);
                if (changed)
                {
                    GUI.DrawTexture(statusPos, OwlcatEditorStyles.Instance.BlueprintHasChangesIcon);
                }
                if (dirty)
                {
                    GUI.DrawTexture(statusPos, OwlcatEditorStyles.Instance.BlueprintIsDirtyIcon);
                }
                // tooltip
                var c = new GUIContent("","");
                if (hasValidationError)
                {
                    c.tooltip += (string.IsNullOrEmpty(c.tooltip) ? "" : "\n") + "Has validation errors!";
                }
                
                if (dirty)
                {
                    c.tooltip += (string.IsNullOrEmpty(c.tooltip) ? "" : "\n") + "Has unsaved changes";
                }
                
                if (changed)
                {
                    c.tooltip += (string.IsNullOrEmpty(c.tooltip) ? "" : "\n") + "Has changes in JSON";
                }

                if (IsShadowDeleted)
                {
                    c.tooltip += (string.IsNullOrEmpty(c.tooltip) ? "" : "\n") + "<b><color=#ff0000ff>Is shadow deleted</color></b>";
                }

                if (ContainsShadowDeletedBlueprints)
                {
                    c.tooltip += (string.IsNullOrEmpty(c.tooltip) ? "" : "\n") + "<b><color=#ffa500ff>Contains shadow deleted blueprints</color></b>";
                }

                GUI.Label(rect, c, GUIStyle.none);
            }
        }
    }
    public class FolderListItem
    {
        private readonly string m_FullPath;
        private string m_Name;
        private GUIContent m_Content = new GUIContent();
        private FileListView m_Owner;
        private static Texture2D m_FolderIcon;
        
        public FolderListItem(string fullPath, FileListView owner)
        {
            m_FullPath = fullPath;
            m_Owner = owner;
            m_Name = Path.GetFileNameWithoutExtension(fullPath);
            m_Content.text = m_Name;

            m_FolderIcon = m_FolderIcon ?? EditorGUIUtility.FindTexture("Folder Icon");
            m_Content.image = m_FolderIcon;
        }

        public string Name
            => m_Name;

        public void OnGUI(Rect rect)
        {
            var style = (GUIStyle)"TV Line";
            if (rect == default(Rect))
            {
                rect = GUILayoutUtility.GetRect(m_Content, style.name, GUILayout.MaxHeight(18),
                    GUILayout.ExpandWidth(true));
            }

            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(rect, m_Content, false, false, false, true);
            }
        }
    }
}
