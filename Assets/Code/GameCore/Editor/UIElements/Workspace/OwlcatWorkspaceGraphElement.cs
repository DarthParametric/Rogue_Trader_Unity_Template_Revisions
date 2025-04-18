using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace Kingmaker.Editor.UIElements.Workspace
{
	public abstract class OwlcatWorkspaceGraphElement : GraphElement
	{
		public readonly SerializedObject SerializedObject; 
		
		protected OwlcatWorkspaceGraphElement(SerializedObject serializedObject)
		{
			SerializedObject = serializedObject;
			var elementInspector = UIElementsUtility.CreateInspector(SerializedObject, force: true);
			Add(elementInspector);
		}
	}
}