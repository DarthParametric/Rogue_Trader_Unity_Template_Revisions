using System;
using Kingmaker.Blueprints.Area;
using System.Collections.Generic;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
    [CustomEditor(typeof(AreaPartBounds))]
    public class AreaPartBoundsInspector : UnityEditor.Editor
    {
        private enum EditBoundsType
        {
            None,
            Default,
            Mechanics,
            FogOfWar,
            LocalMap,
            CameraBounds,
            BakedGroundBounds
        }

        private static readonly Dictionary<EditBoundsType, Color> Colors = new()
        {
            { EditBoundsType.Default, Color.red },
            { EditBoundsType.Mechanics, Color.cyan },
            { EditBoundsType.FogOfWar, Color.magenta },
            { EditBoundsType.LocalMap, Color.yellow },
            { EditBoundsType.CameraBounds, Color.green },
            { EditBoundsType.BakedGroundBounds, Color.blue }
        };

        private static GUIContent s_DefaultBoundsContent;
        private static GUIContent DefaultBoundsContent
            => s_DefaultBoundsContent ??= new GUIContent(EditorGUIUtility.IconContent("EditCollider", "Default"))
            {
                tooltip = "Edit Default Bounds"
            };
        
        private static GUIContent s_MechanicsBoundsContent;
        private static GUIContent MechanicsBoundsContent
            => s_MechanicsBoundsContent ??= new GUIContent(EditorGUIUtility.IconContent("EditCollider", "Mechanics"))
            {
                tooltip = "Edit Mechanics Bounds"
            };

        private static GUIContent s_FogOfWarBoundsContent;
        private static GUIContent FogOfWarBoundsContent
            => s_FogOfWarBoundsContent ??= new GUIContent(EditorGUIUtility.IconContent("EditCollider", "FOW"))
            {
                tooltip = "Edit Fog Of War Bounds"
            };

        private static GUIContent s_LocalMapBoundsContent;
        private static GUIContent LocalMapBoundsContent
            => s_LocalMapBoundsContent ??= new GUIContent(EditorGUIUtility.IconContent("EditCollider", "LocalMap"))
            {
                tooltip = "Edit Local Map Bounds"
            };

        private static GUIContent s_CameraBoundsContent;
        private static GUIContent CameraBoundsContent
            => s_CameraBoundsContent ??= new GUIContent(EditorGUIUtility.IconContent("EditCollider", "Camera"))
            {
                tooltip = "Edit Camera Bounds"
            };

        private static GUIContent s_BakedGroundBoundsContent;
        private static GUIContent BakedGroundBoundsContent
            => s_BakedGroundBoundsContent ??=
                new GUIContent(EditorGUIUtility.IconContent("EditCollider", "BakedGround"))
                {
                    tooltip = "Edit BakedGround Bounds"
                };


        private static GUIStyle s_SingleButtonStyle;
        private static GUIStyle SingleButtonStyle
            => s_SingleButtonStyle ??= "EditModeSingleButton";

        private EditBoundsType m_EditBoundsType = EditBoundsType.None;

        private readonly BoxBoundsHandle m_BoundsHandle = new();

        private AreaPartBounds Target => target as AreaPartBounds;

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        public override void OnInspectorGUI()
        {
            using (GuiScopes.Horizontal())
            {
                GUILayout.FlexibleSpace();
                DoEditTypeToggle(EditBoundsType.Default);
                using (new EditorGUI.DisabledScope(!Target.OverrideMechanicBounds))
                {
                    DoEditTypeToggle(EditBoundsType.Mechanics);
                }
                using (new EditorGUI.DisabledScope(!Target.OverrideFogOfWarBounds))
                {
                    DoEditTypeToggle(EditBoundsType.FogOfWar);
                }
                using (new EditorGUI.DisabledScope(!Target.OverrideLocalMapBounds))
                {
                    DoEditTypeToggle(EditBoundsType.LocalMap);
                }
                using (new EditorGUI.DisabledScope(!Target.OverrideCameraBounds))
                {
                    DoEditTypeToggle(EditBoundsType.CameraBounds);
                }
                using (new EditorGUI.DisabledScope(!Target.OverrideBakedGroundBounds))
                {
                    DoEditTypeToggle(EditBoundsType.BakedGroundBounds);
                }
                GUILayout.FlexibleSpace();
            }

            PrototypedObjectEditorUtility.DisplayProperties(serializedObject);
        }

        private void DoEditTypeToggle(EditBoundsType editType)
        {
            m_EditBoundsType = GUILayout.Toggle(m_EditBoundsType == editType, GetContent(editType), SingleButtonStyle, GUILayout.Height(30), GUILayout.Width(40)) ? editType : m_EditBoundsType == editType ? EditBoundsType.None : m_EditBoundsType;
        }

        private GUIContent GetContent(EditBoundsType editType)
        {
            switch (editType)
            {
                case EditBoundsType.Default:
                    return DefaultBoundsContent;
                case EditBoundsType.Mechanics:
                    return MechanicsBoundsContent;
                case EditBoundsType.FogOfWar:
                    return FogOfWarBoundsContent;
                case EditBoundsType.LocalMap:
                    return LocalMapBoundsContent;
                case EditBoundsType.CameraBounds:
                    return CameraBoundsContent;
                case EditBoundsType.BakedGroundBounds:
                    return BakedGroundBoundsContent;
            }

            return new GUIContent();
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            ValidateEditType();

            DrawGizmos();

            if (m_EditBoundsType != EditBoundsType.None)
            {
                DoBoxEditing();
            }
        }

        private static void DrawBounds(Bounds bounds, EditBoundsType color)
        {
            var oldColor = Handles.color;
            Handles.color = Colors[color];
            Handles.DrawWireCube(bounds.center, bounds.size);
            Handles.color = oldColor;
        }

        private void DrawGizmos()
        {
            if (m_EditBoundsType != EditBoundsType.Default) 
                DrawBounds(Target.DefaultBounds, EditBoundsType.Default);

            if (m_EditBoundsType != EditBoundsType.Mechanics)
                DrawBounds(Target.MechanicBounds, EditBoundsType.Mechanics);

            if (m_EditBoundsType != EditBoundsType.FogOfWar && Target.OverrideFogOfWarBounds)
                DrawBounds(Target.FogOfWarBounds, EditBoundsType.FogOfWar);

            if (m_EditBoundsType != EditBoundsType.LocalMap && Target.OverrideLocalMapBounds)
                DrawBounds(Target.LocalMapBounds, EditBoundsType.LocalMap);

            if (m_EditBoundsType != EditBoundsType.CameraBounds && Target.OverrideCameraBounds)
                DrawBounds(Target.CameraBounds, EditBoundsType.CameraBounds);

            if (m_EditBoundsType != EditBoundsType.BakedGroundBounds && Target.OverrideBakedGroundBounds)
                DrawBounds(Target.BakedGroundBounds, EditBoundsType.BakedGroundBounds);
        }

        private void ValidateEditType()
        {
            switch (m_EditBoundsType)
            {
                case EditBoundsType.None:
                    break;
                case EditBoundsType.Default:
                    break;
                case EditBoundsType.Mechanics:
                    m_EditBoundsType = (m_EditBoundsType == EditBoundsType.Mechanics && !Target.OverrideMechanicBounds) ? EditBoundsType.None : m_EditBoundsType;
                    break;
                case EditBoundsType.FogOfWar:
                    m_EditBoundsType = (m_EditBoundsType == EditBoundsType.FogOfWar && !Target.OverrideFogOfWarBounds) ? EditBoundsType.None : m_EditBoundsType;
                    break;
                case EditBoundsType.LocalMap:
                    m_EditBoundsType = (m_EditBoundsType == EditBoundsType.LocalMap && !Target.OverrideLocalMapBounds) ? EditBoundsType.None : m_EditBoundsType;
                    break;
                case EditBoundsType.CameraBounds:
                    m_EditBoundsType = (m_EditBoundsType == EditBoundsType.CameraBounds && !Target.OverrideCameraBounds) ? EditBoundsType.None : m_EditBoundsType;
                    break;
                case EditBoundsType.BakedGroundBounds:
                    m_EditBoundsType = (m_EditBoundsType == EditBoundsType.BakedGroundBounds && !Target.OverrideBakedGroundBounds) ? EditBoundsType.None : m_EditBoundsType;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Bounds GetBounds(EditBoundsType editType)
            => editType switch
            {
                EditBoundsType.None => Target.DefaultBounds,
                EditBoundsType.Default => Target.DefaultBounds,
                EditBoundsType.Mechanics => Target.MechanicBounds,
                EditBoundsType.FogOfWar => Target.FogOfWarBounds,
                EditBoundsType.LocalMap => Target.LocalMapBounds,
                EditBoundsType.CameraBounds => Target.CameraBounds,
                EditBoundsType.BakedGroundBounds => Target.BakedGroundBounds,
                _ => throw new ArgumentOutOfRangeException(nameof(editType), editType, null)
            };

        private void SetBounds(in Bounds bounds)
        {
            switch (m_EditBoundsType)
            {
                case EditBoundsType.None:
                    break;
                case EditBoundsType.Default:
                    Target.DefaultBounds = bounds;
                    break;
                case EditBoundsType.Mechanics:
                    Target.MechanicBounds = bounds;
                    break;
                case EditBoundsType.FogOfWar:
                    Target.FogOfWarBounds = bounds;
                    break;
                case EditBoundsType.LocalMap:
                    Target.LocalMapBounds = bounds;
                    break;
                case EditBoundsType.CameraBounds:
                    Target.CameraBounds = bounds;
                    break;
                case EditBoundsType.BakedGroundBounds:
                    Target.BakedGroundBounds = bounds;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // from ReflectionProbeEditor
        private void DoBoxEditing()
        {
            EditorGUI.BeginChangeCheck();
            Bounds bounds = GetBounds(m_EditBoundsType);
            bounds.center = Handles.PositionHandle(bounds.center, Quaternion.identity);
            m_BoundsHandle.center = bounds.center;
            m_BoundsHandle.size = bounds.size;
            m_BoundsHandle.handleColor = Colors[m_EditBoundsType];
            m_BoundsHandle.wireframeColor = Colors[m_EditBoundsType];
            this.m_BoundsHandle.DrawHandle();
            if (!EditorGUI.EndChangeCheck())
                return;
            Undo.RecordObject(Target, "Modified AreaPartBounds AABB");
            Vector3 center = m_BoundsHandle.center;
            Vector3 size = m_BoundsHandle.size;
            bounds.center = center;
            bounds.size = size;

            SetBounds(bounds);

            EditorUtility.SetDirty(Target);
            Event.current.Use();
        }
    }
}
