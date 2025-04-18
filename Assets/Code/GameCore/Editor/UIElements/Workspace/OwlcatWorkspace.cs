using ExitGames.Client.Photon.StructWrapping;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Kingmaker.Editor.UIElements.Workspace
{
	public class OwlcatWorkspace : EditorWindow
	{
		private OwlcatWorkspaceGraphView m_GraphView;
		
		[MenuItem("Window/Owlcat Workspace")]
		public static void ShowWindow()
		{
			var window = GetWindow<OwlcatWorkspace>();
			window.titleContent = new GUIContent("Owlcat Workspace");
			window.minSize = new Vector2(640, 480);
		}

		private void CreateGUI()
		{
			var openBlueprint = new Button {text = "Open Blueprint"};
			openBlueprint.RegisterCallback<ClickEvent>(HandleOpenBlueprint);
			rootVisualElement.Add(openBlueprint);
			
			m_GraphView = new OwlcatWorkspaceGraphView();
			rootVisualElement.Add(m_GraphView);
		}

		private void HandleOpenBlueprint(ClickEvent evt)
		{
			BlueprintPicker.ShowAssetPicker(
				typeof(SimpleBlueprint),
				null,
				obj => m_GraphView.Open(obj));
		}
	}
}