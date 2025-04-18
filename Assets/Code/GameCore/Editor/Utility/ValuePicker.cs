using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kingmaker.Editor
{
	public abstract class ValuePicker<T> : KingmakerWindowBase
	{
		private static Vector2 s_Size = new Vector2(200, 300);
	    private static T m_PrevPicked;

		[NotNull]
		private Action<T> m_Callback;

		[NotNull]
		private List<T> m_AllValues = new List<T>();

		[NotNull]
		private List<T> m_DisplayValues = new List<T>();

		private T m_Selected;

		[NotNull]
		private string m_SearchString = "";

		[NotNull]
		private GUIStyle m_CommonStyle;

		[NotNull]
		private GUIStyle m_SelectedStyle;

		[CanBeNull]
		private EditorWindow m_Parent;

		private bool m_FirstFrame = true;

		private bool m_MustScrollToSelected = false;

		private Vector2 m_ScrollPosition;

		public void Init(string buttonText, EditorWindow parent, Action<T> callback, IEnumerable<T> values)
		{
			titleContent = new GUIContent(buttonText);
			minSize = s_Size;

			m_CommonStyle = new GUIStyle(EditorStyles.label);
			m_CommonStyle.active = m_CommonStyle.normal;

			m_SelectedStyle = new GUIStyle(EditorStyles.label);
			m_SelectedStyle.normal = m_SelectedStyle.focused;
			m_SelectedStyle.active = m_SelectedStyle.focused;

			m_Parent = parent;
			m_Callback = callback;
			m_AllValues = values.ToList();
			UpdateFilteredValues();
		}

		protected static void Button(Func<ValuePicker<T>> windowCreator, string buttonText, Func<IEnumerable<T>> valuesCollector, Action<T> callback, bool showNow = false, GUIStyle style=null, params GUILayoutOption[] options)
		{
		    style = style ?? GUI.skin.button;
			var rect = GUILayoutUtility.GetRect(new GUIContent(buttonText), style, options);
			Button(windowCreator, rect, buttonText, valuesCollector, callback, showNow, style);
		}

		protected static void Button(
			Func<ValuePicker<T>> windowCreator, Rect rect, string buttonText, Func<IEnumerable<T>> valuesCollector,
			Action<T> callback, bool showNow = false, GUIStyle style = null)
		{
			var parent = focusedWindow;
			var actualStyle = style ?? GUI.skin.button;
			if (showNow || GUI.Button(rect, buttonText, actualStyle))
			{
				var window = windowCreator();
				var values = valuesCollector();
				window.Init(buttonText, parent, callback, values);

				var size = window.position.size;

				var screenRect = rect;
				screenRect.position = GUIUtility.GUIToScreenPoint(rect.position);
				window.ShowAsDropDown(screenRect, s_Size);
				window.maxSize = new Vector2(4000, 4000);
				var newPos = window.position;
				newPos.size = size;
				window.position = newPos;
			}
		}

		protected static VisualElement CreateButton(Func<ValuePicker<T>> windowCreator, string buttonText, Func<IEnumerable<T>> valuesCollector,
			Action<T> callback)
		{
			var button = new Button() { text = buttonText, style = { marginTop = new StyleLength(4), marginBottom = new StyleLength(4)} };
			button.clicked += () => ShowPickerMenu(button, windowCreator, buttonText, valuesCollector, callback);

			return button;
		}

		public static void ShowPickerMenu(VisualElement source, Func<ValuePicker<T>> windowCreator, string windowTitle, Func<IEnumerable<T>> valuesCollector,
			Action<T> callback)
		{
			var parent = focusedWindow;
			var window = windowCreator();
			var values = valuesCollector();
			window.Init(windowTitle, parent, callback, values);
			
			var size = window.position.size;

			var rect = source.layout;
			var screenRect = rect;
			screenRect.position = GUIUtility.GUIToScreenPoint(source.LocalToWorld(source.layout).position);
			window.ShowAsDropDown(screenRect, s_Size);
			window.maxSize = new Vector2(4000, 4000);
			var newPos = window.position;
			newPos.size = size;
			window.position = newPos;
		}

		protected override void OnGUI()
		{
			base.OnGUI();
			HandleHotkeys();
			DrawFilter();
			DrawContent();
		}
		
	    public void OnLostFocus()
	    {
		    EditorApplication.delayCall += Close;
	    }

		private void DrawFilter()
		{
			string prevSearch = m_SearchString;

			GUI.SetNextControlName("filter");
			m_SearchString = EditorGUILayout.TextField(m_SearchString);
			if (m_FirstFrame)
			{
				m_FirstFrame = false;
				GUI.FocusControl("filter");
			}

			if (prevSearch != m_SearchString)
				UpdateFilteredValues();
	    }

	    //private void DrawPrevPicked()
	    //{
	    //    if (m_PrevPicked != null && m_AllValues.Contains(m_PrevPicked))
	    //    {
	    //        string valueName = GetValueName(m_PrevPicked);
	    //        var style = m_PrevPicked.Equals(m_Selected) ? m_SelectedStyle : m_CommonStyle;
     //           var btnRect = GUILayoutUtility.GetRect(new GUIContent(valueName), style);
	    //        if (GUI.Button(btnRect, valueName, style))
	    //        {
	    //            if (m_PrevPicked.Equals(m_Selected))
	    //            {
	    //                Select(m_Selected);
	    //            }

	    //            m_Selected = m_PrevPicked;
	    //        }

     //           GUILayout.Box(GUIContent.none);
     //       }
	    //}

        protected virtual string GetValueName(T value)
		{
			return value.ToString();
		}

		private void DrawContent()
		{
			float selectedY = 0f;
			using (var scroll = new GUILayout.ScrollViewScope(m_ScrollPosition))
			{
				foreach (var v in m_DisplayValues)
				{
					var style = v.Equals(m_Selected) ? m_SelectedStyle : m_CommonStyle;

					string valueName = GetValueName(v);
					var btnRect = GUILayoutUtility.GetRect(new GUIContent(valueName), style);
					if (GUI.Button(btnRect, valueName, style))
					{
						if (v.Equals(m_Selected))
						{
							Select(m_Selected);
						}

						m_Selected = v;
					}

					if (v.Equals(m_Selected))
					{
						selectedY = btnRect.yMin;
					}

				    if (m_PrevPicked != null && v.Equals(m_PrevPicked))
				    {
				        GUILayout.Space(2);
                        Handles.DrawLine(new Vector3(btnRect.xMin, btnRect.yMax + 2), new Vector3(btnRect.xMax, btnRect.yMax + 2));
				    }
				}
				m_ScrollPosition = scroll.scrollPosition;
			}

			if (Event.current.type == EventType.Repaint)
			{
				if (m_MustScrollToSelected)
				{
					m_ScrollPosition.y = selectedY - 150;
					m_MustScrollToSelected = false;
					Repaint();
				}
			}
		}

		private void HandleHotkeys()
		{
			var e = Event.current;
			if (e.type != EventType.KeyDown)
				return;

			switch (e.keyCode)
			{
				case KeyCode.DownArrow:
					int i1 = m_DisplayValues.IndexOf(m_Selected);
					if (i1 < m_DisplayValues.Count - 1)
						m_Selected = m_DisplayValues[i1 + 1];
					m_MustScrollToSelected = true;
					e.Use();
					break;
				case KeyCode.UpArrow:
					int i2 = m_DisplayValues.IndexOf(m_Selected);
					if (i2 > 0)
						m_Selected = m_DisplayValues[i2 - 1];
					m_MustScrollToSelected = true;
					e.Use();
					break;
				case KeyCode.KeypadEnter:
				case KeyCode.Return:
					Select(m_Selected);
					e.Use();
					break;
				case KeyCode.Escape:
					Cancel();
					e.Use();
					break;
			}
		}

		private void Select(T result)
		{
			Close();
			if (result != null)
			{
			    m_PrevPicked = result;
				m_Callback(result);
			}
			if (m_Parent != null)
			{
				m_Parent.Repaint();
				m_Parent.Focus();
			}
		}

		private void Cancel()
		{
			Close();
			if (m_Parent != null)
			{
				m_Parent.Repaint();
				m_Parent.Focus();
			}
		}


		private void UpdateFilteredValues()
		{
			if (m_SearchString == "")
			{
				m_DisplayValues = m_AllValues;
			}
			else
			{
				string[] filters = m_SearchString.ToLowerInvariant().Split();
				m_DisplayValues = m_AllValues.Where(t => filters.All(f => GetValueName(t).ToLowerInvariant().Contains(f))).ToList();
			}

		    if (m_PrevPicked != null)
		    {
		        int lastPickedIndex = m_DisplayValues.IndexOf(m_PrevPicked);
		        if (lastPickedIndex > 0)
		        {
		            m_DisplayValues.RemoveAt(lastPickedIndex);
		            m_DisplayValues.Insert(0, m_PrevPicked);
		        }
		    }

            if (!m_DisplayValues.Contains(m_Selected))
				m_Selected = m_DisplayValues.FirstOrDefault();
			m_MustScrollToSelected = true;
		}
	}
}