using System;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Cutscenes
{
    internal static class Layouts
    {
        private class VerticalLayout : IDisposable
        {
            public VerticalLayout(GUIStyle style, params GUILayoutOption[] options)
                => EditorGUILayout.BeginVertical(style, options);

            public void Dispose()
                => EditorGUILayout.EndVertical();
        }

        private class HorizontalLayout : IDisposable
        {
            public HorizontalLayout(params GUILayoutOption[] options)
                => EditorGUILayout.BeginHorizontal(options);

            public void Dispose()
                => EditorGUILayout.EndHorizontal();
        }

        public static IDisposable Horizontal(params GUILayoutOption[] options)
            => new HorizontalLayout(options);

        public static IDisposable Vertical(GUIStyle style, params GUILayoutOption[] options)
            => new VerticalLayout(style, options);
    }
}