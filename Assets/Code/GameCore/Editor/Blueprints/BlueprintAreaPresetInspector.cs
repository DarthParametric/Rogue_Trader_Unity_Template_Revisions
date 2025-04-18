#if UNITY_EDITOR 
using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Editor.Utility;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    [BlueprintCustomEditor(typeof(BlueprintAreaPreset))]
    public class BlueprintAreaPresetInspector : BlueprintInspectorCustomGUI
    {
        private Func<AssetPicker.HierarchyEntry, bool> m_DependenciesFilter;

        // todo: [bp] fix preset inspector (might not be needed at all)
        public override void OnHeader(SimpleBlueprint bp)
        {
            var preset = bp as BlueprintAreaPreset;

            var dependencies = AreaDependenciesInfo(preset);

            m_DependenciesFilter = he => dependencies == null || dependencies.BlueprintPaths.Contains(he.Path);
            AssetPicker.DependenciesFilters.Add(m_DependenciesFilter);
        }

        public override void OnBeforeComponents(SimpleBlueprint bp)
        {
            AssetPicker.DependenciesFilters.Remove(m_DependenciesFilter);
        }

        private static AreaDependenciesInfo AreaDependenciesInfo(BlueprintAreaPreset preset)
        {
            using (GuiScopes.Horizontal())
            {
                GUILayout.Space(16);
                if (preset.DependenciesInfo == null)
                {
                    if (GUILayout.Button("Show Only Dependencies", EditorStyles.miniButton))
                    {
                        preset.DependenciesInfo = AreaInfoCollector.CollectDependencies(preset.Area);
                    }

                    GUILayout.FlexibleSpace();
                }
                else
                {
                    if (GUILayout.Button("Show All", EditorStyles.miniButtonLeft))
                    {
                        preset.DependenciesInfo = null;
                    }

                    if (GUILayout.Button("Rescan Dependencies", EditorStyles.miniButtonRight))
                    {
                        preset.DependenciesInfo = AreaInfoCollector.CollectDependencies(preset.Area);
                    }
                }

                GUILayout.FlexibleSpace();
            }

            var dependencies = preset.DependenciesInfo as AreaDependenciesInfo;
            if (dependencies != null)
            {
                if (dependencies.ReinforcedAlignments.Count > 0)
                {
                    using (GuiScopes.Horizontal())
                    {
                        GUILayout.Space(16);
                        GUILayout.Label("Reinforced Alignments");
                    }

                    foreach (var a in dependencies.ReinforcedAlignments)
                    {
                        using (GuiScopes.Horizontal())
                        {
                            GUILayout.Space(32);
                            GUILayout.Label(a.ToString());
                        }
                    }
                }

                if (dependencies.CheckedAlignments.Count > 0)
                {
                    using (GuiScopes.Horizontal())
                    {
                        GUILayout.Space(16);
                        GUILayout.Label("Checked Alignments");
                    }

                    foreach (var a in dependencies.CheckedAlignments)
                    {
                        using (GuiScopes.Horizontal())
                        {
                            GUILayout.Space(32);
                            GUILayout.Label(a.ToString());
                        }
                    }
                }
            }

            return dependencies;
        }
    }
}
#endif