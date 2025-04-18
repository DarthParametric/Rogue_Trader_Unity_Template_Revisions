using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;
using UnityEngine;

namespace RogueTrader.Editor.Blueprints.ProjectView
{
	public class UsingRemoveWindow : EditorWindow
	{
		private Dictionary<string, List<(string, string)>> m_Removes;
		private Vector2 m_Scroll;
		
		private void OnEnable()
		{
			m_Removes ??= new Dictionary<string, List<(string, string)>>();
			m_Removes.Clear();
			
			var removes = BlueprintsDatabase.ListOfContainsRemoveBlueprints;
			foreach (var remove in removes)
			{
				if (!m_Removes.ContainsKey(remove.Key))
				{
					m_Removes.Add(remove.Key, new List<(string, string)>(remove.Value.Count));
				}

				foreach (string guid in remove.Value)
				{
					string path = BlueprintsDatabase.IdToPath(guid);
					m_Removes[remove.Key].Add((guid, path));
				}
			}
		}
		
		void OnGUI()
		{
			using EditorGUILayout.VerticalScope vScope = new(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
			using var scrollScope = new EditorGUILayout.ScrollViewScope(m_Scroll);
			m_Scroll = scrollScope.scrollPosition;
			if (GUILayout.Button("Refresh"))
			{
				BlueprintsDatabase.ListOfContainsRemoveBlueprints = null;
				OnEnable();
			}
			
			foreach (var pair in m_Removes)
			{
				var items = pair.Value;
				EditorGUILayout.SelectableLabel("Remove: " + pair.Key);
				EditorGUI.indentLevel++;
				foreach (var item in items)
				{
					using (new EditorGUILayout.HorizontalScope()) 
					{
						EditorGUILayout.SelectableLabel(item.Item2);
						if (GUILayout.Button("Open", GUILayout.ExpandWidth(false)))
						{
							Selection.activeObject = BlueprintEditorWrapper.Wrap(BlueprintsDatabase.LoadById<SimpleBlueprint>(item.Item1));
						}
					}
				}
				EditorGUI.indentLevel--;
			}
		}
	}
}