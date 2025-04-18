using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Cutscenes
{
    public class SimpleList
    {
        public interface IListGUI<in T>
        {
            bool DrawItem(int index, T item, bool selected);
            void RemoveAt(int index);
            void AddItem();
            void DrawTitle(Rect rect);
        }

        private static Styles s_Styles;

        public int SelectedIndex { get; set; }
        public string Title { get; set; }
	    public bool IsFolded { get; set; }
	    public bool NoAddRemove { get; set; }

		public SimpleList()
        {
            SelectedIndex = -1;
        }

        public void DrawList<T>(IEnumerable<T> list, IListGUI<T> gui)
        {
            s_Styles ??= new Styles();

            if (!string.IsNullOrEmpty(Title))
            {
                using (SetColor.Background(IsFolded ? Color.grey : Color.white))
                {
                    GUILayout.Box("", s_Styles.headerBackground, GUILayout.ExpandWidth(true));
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.x += 4;
                    rect.width -= 4;
                    GUI.Label(rect, Title, EditorStyles.boldLabel);
                    if (!IsFolded)
                        gui.DrawTitle(rect);
                    if (GUI.Button(rect, "", GUIStyle.none))
                        IsFolded = !IsFolded;
                }
            }
            if (!IsFolded)
            {
                using (Layouts.Vertical(s_Styles.boxBackground, GUILayout.ExpandHeight(false)))
                {
                    int i = 0;
                    foreach (var obj in list)
                    {
                        if (gui.DrawItem(i, obj, i == SelectedIndex) && i!=SelectedIndex)
                        {
                            SelectedIndex = i;
                            GUIUtility.hotControl = -1;
                            GUIUtility.keyboardControl = -1;
                        }
                        i++;
                    }
                    GUILayout.Space(4);
                }
	            if (!NoAddRemove)
	            {
		            using (Layouts.Horizontal())
		            {
			            GUILayout.FlexibleSpace();
			            GUILayout.Box("", s_Styles.footerBackground,
				            GUILayout.Width(EditorGUIUtility.singleLineHeight * 4 + 3),
				            GUILayout.Height(EditorGUIUtility.singleLineHeight));
			            var rect = GUILayoutUtility.GetLastRect();
			            rect.y -= 2;
			            rect.width = (EditorGUIUtility.singleLineHeight * 2);
			            if (GUI.Button(rect, s_Styles.iconToolbarPlus, s_Styles.preButton))
			            {
				            gui.AddItem();
			            }
			            rect.x += rect.width + 3;
			            if (GUI.Button(rect, s_Styles.iconToolbarMinus, s_Styles.preButton))
			            {
				            if (SelectedIndex >= 0)
				            {
					            gui.RemoveAt(SelectedIndex);
					            SelectedIndex = -1;
				            }
			            }
		            }
	            }
            }
        }

        private class Styles
        {
            public GUIContent iconToolbarPlus = EditorGUIUtility.IconContent("Toolbar Plus", "Add to list");

            public GUIContent iconToolbarMinus = EditorGUIUtility.IconContent("Toolbar Minus",
                "Remove selection from list");

            public readonly GUIStyle headerBackground = (GUIStyle) "RL Header";
            public readonly GUIStyle footerBackground = (GUIStyle) "RL Footer";
            public readonly GUIStyle boxBackground = (GUIStyle) "RL Background";
            public readonly GUIStyle preButton = (GUIStyle) "RL FooterButton";
        }
    }
}