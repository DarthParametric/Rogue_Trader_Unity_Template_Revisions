using System.Collections.Generic;
using Kingmaker.Blueprints;
using Kingmaker.Editor.UIElements.Workspace.Elements;
using UnityEditor.Experimental.GraphView;

namespace Kingmaker.Editor.UIElements.Workspace
{
	public class OwlcatWorkspaceGraphView : GraphView
	{
		private Dictionary<SimpleBlueprint, OwlcatWorkspaceGraphElement> m_OpenedElements = new();
		
		public void Open(SimpleBlueprint blueprint)
		{
			if (m_OpenedElements.ContainsKey(blueprint))
				return;

			var element = new BlueprintGraphElement(blueprint);
			AddElement(element);
			
			m_OpenedElements.Add(blueprint, element);
		}
	}
}