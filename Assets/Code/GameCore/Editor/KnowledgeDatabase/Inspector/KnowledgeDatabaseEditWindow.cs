using System;
using System.Reflection;
using JetBrains.Annotations;
using Kingmaker.Editor;
using Kingmaker.ElementsSystem;
using UnityEditor;
using UnityEngine;

namespace Code.Editor.KnowledgeDatabase.Inspector
{
	public class KnowledgeDatabaseEditWindow : KingmakerWindowBase
	{
		private Type m_Type;
		
		[CanBeNull]
		private string m_FieldName;
		private Vector2 m_ScrollDescription;

        public static void Show(Type type, string fieldName = null)
		{
			var window = GetWindow<KnowledgeDatabaseEditWindow>("Knowledge Database Description");
			window.m_Type = type;
			window.m_FieldName = fieldName;
			window.ShowAuxWindow();
			window.Focus();
		}

		public static void Show(SerializedProperty property)
		{
			(var type, string fieldName) = property.GetTypeAndName();
			Show(type, fieldName);
		}

		protected override void OnGUI()
		{
			base.OnGUI();

			using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
			{
				string description = KnowledgeDatabaseSearch.GetDescription(m_Type, m_FieldName);
				if (description == null)
				{
					GUILayout.Label($"Not found in database: {m_Type.Name}.{m_FieldName}");
					GUILayout.Label("Try to update database (Designers/Knowledge Database/Update)");
					GUILayout.Label("If update doesn't help then current pair of (Type, Field name) isn't supported yet");
					return;
				}

				GUILayout.Label($"Type: {m_Type.Name} ({m_Type.Namespace})");
				if (m_FieldName != null)
				{
					GUILayout.Label($"Field: {m_FieldName}");
				}

                string codeDescription = KnowledgeDatabaseSearch.GetCodeDescription(m_Type, m_FieldName);
                if (codeDescription != null)
				{
                    GUILayout.Space(10);
                    GUILayout.Label("Programmer's description:");
					GUILayout.TextArea(codeDescription, GUILayout.MinHeight(100));
				}

                GUILayout.Space(10);
                using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollDescription))
                {
	                m_ScrollDescription = scrollScope.scrollPosition;
	                string newDescription = GUILayout.TextArea(description, GUILayout.MinHeight(400), GUILayout.ExpandHeight(true));
	                KnowledgeDatabaseSearch.SetDescription(m_Type, m_FieldName, newDescription);
                }

                if (GUILayout.Button("Save"))
				{
					KnowledgeDatabase.Save();
				}
			}
		}
	}
}