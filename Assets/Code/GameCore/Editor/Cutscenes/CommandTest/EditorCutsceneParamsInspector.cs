using Kingmaker.Editor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace Kingmaker.Editor.Cutscenes.CommandTest
{
	public class EditorCutsceneParamsInspector : EditorWindow
	{
		private void OnEnable()
		{
			titleContent = new GUIContent("EditorCutsceneParams");

			var scroll = new ScrollView(ScrollViewMode.Vertical);

			var inspector = UIElementsUtility.CreateInspector(
				new SerializedObject(EditorCutsceneParams.instance),
				isHideScriptField: true,
				force: true);
			inspector.style.paddingLeft = 15;
			inspector.style.paddingRight = 6;
			inspector.style.paddingBottom = 2;
			inspector.style.paddingTop = 2;
			scroll.contentContainer.Add(inspector);

			var box = new Box();
			box.Add(scroll);

			rootVisualElement.Add(box);
		}

		// [MenuItem("Test/EditorCutsceneParams")]
		public static void Open()
		{
			var window = (EditorCutsceneParamsInspector)GetWindow(typeof(EditorCutsceneParamsInspector));
			window.minSize = new Vector2(400f, 200f);
			window.Show();
		}
	}
}