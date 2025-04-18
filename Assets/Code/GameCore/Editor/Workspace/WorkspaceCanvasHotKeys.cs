using JetBrains.Annotations;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Kingmaker.Editor.Workspace
{
	public static class WorkspaceCanvasHotKeys
	{
		[UsedImplicitly]
		[Shortcut("MarkFirstTarget", KeyCode.F1)]
		public static void MarkFirstTarget()
		{
			UpdateTarget(first: true);
		}

		[UsedImplicitly]
		[Shortcut("MarkSecondTarget", KeyCode.F2)]
		public static void MarkSecondTarget()
		{
			UpdateTarget(first: false);
		}

		private static void UpdateTarget(bool first)
		{
			var mouseOverWindow = EditorWindow.mouseOverWindow;
			if (mouseOverWindow.Or(null)?.GetType() != typeof(WorkspaceCanvasWindow))
				return;

			WorkspaceCanvasWindow.UpdateTarget(first);
			mouseOverWindow.Repaint();
		}
	}
}