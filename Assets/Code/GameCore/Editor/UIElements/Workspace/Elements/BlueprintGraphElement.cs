using ExitGames.Client.Photon.StructWrapping;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

namespace Kingmaker.Editor.UIElements.Workspace.Elements
{
	public class BlueprintGraphElement : OwlcatWorkspaceGraphElement
	{
		public readonly SimpleBlueprint Blueprint;
		
		public BlueprintGraphElement(SimpleBlueprint blueprint) : base(new SerializedObject(BlueprintEditorWrapper.Wrap(blueprint)))
		{
			Blueprint = blueprint;
		}
	}
}