using Assets.Editor;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints.ProjectView;
#if UNITY_EDITOR && EDITOR_FIELDS
using Kingmaker.Assets.Code.Editor.EtudesViewer;
#endif
using Kingmaker.Editor.Workspace;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Kingmaker.Editor.Elements.SmartElementPopulation
{
	[InitializeOnLoad]
	public static class ElementCopyAndPasteController
	{
		private const string ProjectWindowName = "ProjectBrowser";
		private const string HierarchyWindowName = "SceneHierarchyWindow";

		private static Rect s_ScreenSelectionRect;
		private static Vector2 s_ScreenMousePosition;

		private static Object s_Buffer;

		private static string s_ProjectWindowMouseOverGuid;
		private static Object s_WorkspaceWindowMouseOverObject;
		private static BlueprintScriptableObject s_EtudeViewerWindowMouseOverObject;
		private static int? s_HierarchyWindowMouseOverInstanceId;

		static ElementCopyAndPasteController()
		{
			EditorApplication.projectWindowItemOnGUI += HandleProjectWindowMouseOver;
			EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowMouseOver;
			WorkspaceCanvasWindow.WorkspaceWindowItemOnGUI += HandleWorkspaceWindowMouseOver;
            BlueprintProjectView.OnItemGUI += HandleBlueprintProjectWindowMouseOver;
#if UNITY_EDITOR && EDITOR_FIELDS
			EtudeChildrenDrawer.EtudeViewerWindowItemOnGUI += HandleEtudeViewerWindowMouseOver;
			foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if (IsProjectWindow(window) || IsWorkspace(window) || IsHierarchyWindow(window) || IsEtudeViewer(window))
					AdjustMouseOverSettings(window);
			}
#endif
		}

		private static void HandleEtudeViewerWindowMouseOver(BlueprintScriptableObject obj, Rect selectionRect)
		{
			var window = EditorWindow.mouseOverWindow;
#if UNITY_EDITOR && EDITOR_FIELDS
			if (!IsEtudeViewer(window))
				return;
#endif
			HandleCopiableArea(window, selectionRect, bp: obj);
		}

		private static void HandleWorkspaceWindowMouseOver(Object obj, Rect selectionRect)
		{
			var window = EditorWindow.mouseOverWindow;
			if (!IsWorkspace(window))
				return;

			HandleCopiableArea(window, selectionRect, obj: obj);
		}

        private static void HandleProjectWindowMouseOver(string guid, Rect selectionRect)
        {
            var window = EditorWindow.mouseOverWindow;
            if (!IsProjectWindow(window))
                return;

            HandleCopiableArea(window, selectionRect, guid: guid);
        }

        private static void HandleBlueprintProjectWindowMouseOver(Rect selectionRect, FileListItem item)
        {
            var window = EditorWindow.mouseOverWindow;
            if (window is BlueprintProjectView)
            {
                HandleCopiableArea(window, selectionRect, guid: item.Id);
            }
        }

		private static void HandleHierarchyWindowMouseOver(int instanceId, Rect selectionRect)
		{
			var window = EditorWindow.mouseOverWindow;
			if (!IsHierarchyWindow(window))
				return;

			HandleCopiableArea(window, selectionRect, instanceId: instanceId);
		}

		private static void HandleCopiableArea(
			EditorWindow editorWindow,
			Rect selectionRect,
			int? instanceId = null,
			string guid = null,
			Object obj = null, 
            BlueprintScriptableObject bp=null)
		{
			AdjustMouseOverSettings(editorWindow);
			s_ScreenMousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			var screenSelectionRect = GUIUtility.GUIToScreenRect(selectionRect);
			if (!screenSelectionRect.Contains(s_ScreenMousePosition))
				return;

			s_ScreenSelectionRect = screenSelectionRect;
			s_ProjectWindowMouseOverGuid = guid;
			s_WorkspaceWindowMouseOverObject = obj;
			s_EtudeViewerWindowMouseOverObject = bp;
			s_HierarchyWindowMouseOverInstanceId = instanceId;
		}

		//If these settings are not set to true there will be a significant delay
		//between actual mouse over and first onGUI event which will lead to unpleasant experience for users.
		private static void AdjustMouseOverSettings(EditorWindow window)
		{
			if (window == null)
				return;
			if (!window.wantsMouseMove)
				window.wantsMouseMove = true;
			if (!window.wantsMouseEnterLeaveWindow)
				window.wantsMouseEnterLeaveWindow = true;
		}

		[UsedImplicitly]
		[Shortcut("ElementCopyPaste", KeyCode.C)]
		public static void ProcessCopyPaste()
		{
			var mouseOverWindow = EditorWindow.mouseOverWindow;
			if (mouseOverWindow == null)
				return;

			bool copied = ProcessCopy(mouseOverWindow);
			if (copied)
			{
				string objectName = s_Buffer is MonoScript script
					? script.name
					: s_Buffer.ToString();

				mouseOverWindow.ShowNotification(new GUIContent($"Captured {objectName}"));
				return;
			}

			ProcessPaste(mouseOverWindow);
		}

		private static bool ProcessCopy(EditorWindow mouseOverWindow)
		{
			if (!s_ScreenSelectionRect.Contains(s_ScreenMousePosition))
				return false;

			bool hadCopy = false;
			if (IsHierarchyWindow(mouseOverWindow) && s_HierarchyWindowMouseOverInstanceId != null)
			{
				s_Buffer = EditorUtility.InstanceIDToObject(s_HierarchyWindowMouseOverInstanceId.Value);
				hadCopy = true;
			}
			else if (IsProjectWindow(mouseOverWindow) && s_ProjectWindowMouseOverGuid != null)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(s_ProjectWindowMouseOverGuid);
				s_Buffer = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
				hadCopy = true;
			}
            else if (IsWorkspace(mouseOverWindow) && s_WorkspaceWindowMouseOverObject != null)
            {
                s_Buffer = s_WorkspaceWindowMouseOverObject;
                hadCopy = true;
            }
            else if (mouseOverWindow is BlueprintProjectView && s_ProjectWindowMouseOverGuid != null)
            {
                s_Buffer = BlueprintEditorWrapper.Wrap(BlueprintsDatabase.LoadById<SimpleBlueprint>(s_ProjectWindowMouseOverGuid));
                hadCopy = true;
            }
#if UNITY_EDITOR && EDITOR_FIELDS
			else if (IsEtudeViewer(mouseOverWindow) && s_EtudeViewerWindowMouseOverObject != null)
			{
                s_Buffer = BlueprintEditorWrapper.Wrap(s_EtudeViewerWindowMouseOverObject);
                hadCopy = true; 
            }
#endif

			return hadCopy;
		}

		private static void ProcessPaste(EditorWindow mouseOverWindow)
		{
			if (s_Buffer == null)
				return;

			var mousePosition = Event.current.mousePosition;
			var focusedWindow = EditorWindow.focusedWindow;

			if (focusedWindow != mouseOverWindow)
			{
				mousePosition += focusedWindow.position.position;
				mousePosition -= mouseOverWindow.position.position;
			}

			if (IsWorkspace(mouseOverWindow))
			{
				var workspaceWindow = (WorkspaceCanvasWindow)mouseOverWindow;
				workspaceWindow.AcceptDraggedItems(mousePosition, new[] { s_Buffer }, true);
			}
			else
			{
				var evt = new Event() { type = EventType.DragPerform, mousePosition = mousePosition };
				DragAndDrop.objectReferences = new[] { s_Buffer };
				EditorApplication.delayCall += () => mouseOverWindow.SendEvent(evt);
			}
		}

#if UNITY_EDITOR && EDITOR_FIELDS
		private static bool IsEtudeViewer(EditorWindow window)
		{
			return window != null && window.GetType() == typeof(Assets.Code.Editor.EtudesViewer.EtudesViewer);
		}
#endif

		private static bool IsWorkspace(EditorWindow window)
		{
			return window != null && window.GetType() == typeof(WorkspaceCanvasWindow);
		}

		private static bool IsProjectWindow(EditorWindow window)
		{
			return window != null && window.GetType().Name == ProjectWindowName;
		}

		private static bool IsHierarchyWindow(EditorWindow window)
		{
			return window != null && window.GetType().Name == HierarchyWindowName;
		}
	}
}