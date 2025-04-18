using System;
using System.Collections.Generic;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class FixDuplicatesWindow:EditorWindow
    {
        private Dictionary<string, string[]> m_Dupes;
        
        private Vector2 m_Scroll;

        void OnEnable()
        {
            m_Dupes = BlueprintsDatabase.DuplicatedIds.ToDictionary(id => id,
                id => DatabaseServerConnector.ClientInstance
                    .IdToPath(id).Path
                    .Split(';'));
        }

        void OnGUI()
        {
            using EditorGUILayout.VerticalScope vScope = new(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            using var scrollScope = new EditorGUILayout.ScrollViewScope(m_Scroll);
            m_Scroll = scrollScope.scrollPosition;
            if (GUILayout.Button("Refresh"))
            {
                BlueprintsDatabase.InvalidateAllCache();
                OnEnable();
            }
            foreach (var pair in m_Dupes)
            {
                var paths =pair.Value;
                EditorGUILayout.SelectableLabel("Duplicate: "+pair.Key);
                EditorGUI.indentLevel++;
                foreach (var path in paths)
                {
                    using (new EditorGUILayout.HorizontalScope()) 
                    {
                        EditorGUILayout.SelectableLabel(path);
                        if (GUILayout.Button("New Guid", GUILayout.ExpandWidth(false)))
                        {
                            var w = BlueprintJsonWrapper.Load(BlueprintsDatabase.DbPathPrefix + path);
                            w.Data.AssetGuid = w.AssetId = Guid.NewGuid().ToString("N");
                            w.Save(BlueprintsDatabase.DbPathPrefix + path);
                            EditorApplication.delayCall += () => // delay to give the server a chance to update. Doesn't work anyway.
                            {
                                OnEnable();
                                Repaint();
                            };
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}