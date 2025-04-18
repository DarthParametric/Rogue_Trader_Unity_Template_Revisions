using System;
using JetBrains.Annotations;
using Kingmaker.Editor;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Elements.Inspector
{
    public class PropertyCalculatorFormulaWindow : KingmakerWindowBase
    {
        private string m_Formula;

        [CanBeNull]
        private string m_FieldName;

        private static GUIStyle TextAreaStyle;

        //private static readonly GUIStyle TextAreaStyle = new("TextArea")
        //{
        //    richText = true
        //};

        public static void Show(string formula)
        {
            var window = GetWindow<PropertyCalculatorFormulaWindow>("Formula");
            window.m_Formula = formula;

            window.ShowAuxWindow();
            window.Focus();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (TextAreaStyle == null)
            {
                TextAreaStyle = new("TextArea")
                {
                    richText = true
                };
            }

            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
            {
                GUILayout.TextArea(m_Formula, TextAreaStyle, GUILayout.MinHeight(400), GUILayout.ExpandHeight(true));
                //GUILayout.TextArea(m_Formula, GUILayout.MinHeight(400), GUILayout.ExpandHeight(true));
            }
        }
    }
}