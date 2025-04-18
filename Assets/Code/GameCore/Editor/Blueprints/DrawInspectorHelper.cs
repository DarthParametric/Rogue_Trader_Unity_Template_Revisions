using System.Linq;
using System.Reflection;
using Code.GameCore.EntitySystem.Entities.Base;
using Kingmaker.Blueprints;
using Kingmaker.Editor.DragDrop;
using Kingmaker.Editor.Workspace;
using Kingmaker.Utility.UnityExtensions;
using Kingmaker.View;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using Kingmaker.AreaLogic.SceneControllables;

namespace Kingmaker.Editor.Blueprints
{
	public static class DrawInspectorHelper
	{
		public static void DrawEntityReference(FieldInfo field, SerializedProperty p)
		{
			var uidProp = p.FindPropertyRelative("UniqueId");
			var nameProp = p.FindPropertyRelative("EntityNameInEditor");
			var sceneProp = p.FindPropertyRelative("SceneAssetGuid");

			var uid = uidProp.hasMultipleDifferentValues ? "" : uidProp.stringValue;
			var typeAttr =
				field.GetCustomAttributes(typeof(AllowedEntityTypeAttribute), true)
					.OfType<AllowedEntityTypeAttribute>()
					.FirstOrDefault();
			var type = (typeAttr != null) && (typeAttr.Type != null) && typeAttr.Type.IsSubclassOf(typeof(EntityViewBase))
				? typeAttr.Type
				: typeof(EntityViewBase);

			EditorGUI.BeginProperty(new Rect(), GUIContent.none, p);

			if (nameProp.stringValue == "" && !string.IsNullOrEmpty(uid))
			{
				var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
				nameProp.stringValue = view?.GO ? view.GO.name : "-not found-";
			}

			if (sceneProp.stringValue.IsNullOrEmpty() && !string.IsNullOrEmpty(uid))
			{
				var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
				if (view != null)
				{
					sceneProp.stringValue = AssetDatabase.AssetPathToGUID(view.GO.scene.path);
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope())
				{
					using (new EditorGUI.DisabledGroupScope(true))
					{
						var sceneAsset =
							AssetDatabase.LoadAssetAtPath<SceneAsset>(
								AssetDatabase.GUIDToAssetPath(sceneProp.stringValue));
						EditorGUILayout.TextField(p.displayName, nameProp.stringValue);
						EditorGUILayout.ObjectField("Scene", sceneAsset, typeof(SceneAsset), true);
					}
				}

				using (GuiScopes.Color(Color.clear))
				{
					var rect = GUILayoutUtility.GetLastRect();
					// handle special drag-and-drop from workspace - references from workspace should work even when their scene is NOT opened
					if (Event.current.type ==
					    EventType.Ignore // in inspector, Unity turns our DragPerform into Ignore in its inscrutable wisdom
					    && DragManager.Instance.IsDragging<DraggedWorkspaceItems>()
					    && rect.Contains(DragManager.Instance.DropPoint)
					   )
					{
						var dragged = (DraggedWorkspaceItems)DragManager.Instance.DraggedObject;
						var item = dragged.Items.OfType<WorkspaceItemReference>().FirstOrDefault();
						if (item != null)
						{
							uidProp.stringValue = item.UniqueId;
							nameProp.stringValue = item.ObjectName;

							sceneProp.stringValue = AssetDatabase.AssetPathToGUID(item.ScenePath);
							Event.current.Use();
						}
					}
					// invisible object field to handle clicks and drag-drop etc
					var newObj = EditorGUI.ObjectField(rect, p.displayName, null, type, true) as EntityViewBase;
					if (newObj)
					{
						uidProp.stringValue = newObj.UniqueId;
						nameProp.stringValue = newObj.name;

						sceneProp.stringValue = AssetDatabase.AssetPathToGUID(newObj.gameObject.scene.path);
					}
				}
				using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(uid)))
				{
					if (GUILayout.Button("find", GUILayout.Width(50)))
					{
						var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
						if (!view?.GO)
							Debug.Log("No object with id " + uid);
						else
							EditorGUIUtility.PingObject(view.GO);
					}
					if (GUILayout.Button("del", GUILayout.Width(50)))
					{
						uidProp.stringValue = "";
						nameProp.stringValue = "";
						sceneProp.stringValue = "";
					}
				}
			}
			EditorGUI.EndProperty();
		}
		public static void DrawControllableReference(FieldInfo field, SerializedProperty p)
		{
			var uidProp = p.FindPropertyRelative("UniqueId");
			var nameProp = p.FindPropertyRelative("EntityNameInEditor");
			var sceneProp = p.FindPropertyRelative("SceneAssetGuid");

			var uid = uidProp.hasMultipleDifferentValues ? "" : uidProp.stringValue;
			var type = typeof(ControllableComponent);

			EditorGUI.BeginProperty(new Rect(), GUIContent.none, p);

			if (nameProp.stringValue == "" && !string.IsNullOrEmpty(uid))
			{
				var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
				nameProp.stringValue = view?.GO ? view.GO.name : "-not found-";
			}

			if (sceneProp.stringValue.IsNullOrEmpty() && !string.IsNullOrEmpty(uid))
			{
				var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
				if (view != null)
				{
					sceneProp.stringValue = AssetDatabase.AssetPathToGUID(view.GO.scene.path);
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUILayout.VerticalScope())
				{
					using (new EditorGUI.DisabledGroupScope(true))
					{
						var sceneAsset =
							AssetDatabase.LoadAssetAtPath<SceneAsset>(
								AssetDatabase.GUIDToAssetPath(sceneProp.stringValue));
						EditorGUILayout.TextField(p.displayName, nameProp.stringValue);
						EditorGUILayout.ObjectField("Scene", sceneAsset, typeof(SceneAsset), true);
					}
				}

				using (GuiScopes.Color(Color.clear))
				{
					var rect = GUILayoutUtility.GetLastRect();
					// handle special drag-and-drop from workspace - references from workspace should work even when their scene is NOT opened
					if (Event.current.type ==
					    EventType.Ignore // in inspector, Unity turns our DragPerform into Ignore in its inscrutable wisdom
					    && DragManager.Instance.IsDragging<DraggedWorkspaceItems>()
					    && rect.Contains(DragManager.Instance.DropPoint)
					   )
					{
						var dragged = (DraggedWorkspaceItems)DragManager.Instance.DraggedObject;
						var item = dragged.Items.OfType<WorkspaceItemReference>().FirstOrDefault();
						if (item != null)
						{
							uidProp.stringValue = item.UniqueId;
							nameProp.stringValue = item.ObjectName;

							sceneProp.stringValue = AssetDatabase.AssetPathToGUID(item.ScenePath);
							Event.current.Use();
						}
					}
					// invisible object field to handle clicks and drag-drop etc
					var newObj = EditorGUI.ObjectField(rect, p.displayName, null, type, true) as ControllableComponent;
					if (newObj)
					{
						uidProp.stringValue = newObj.UniqueId;
						nameProp.stringValue = newObj.name;

						sceneProp.stringValue = AssetDatabase.AssetPathToGUID(newObj.gameObject.scene.path);
					}
				}
				using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(uid)))
				{
					if (GUILayout.Button("find", GUILayout.Width(50)))
					{
						var view = ControllableComponentCache.All.FirstOrDefault(v => v != null && v.UniqueId == uid);
						if (!view?.gameObject)
							Debug.Log("No object with id " + uid);
						else
							EditorGUIUtility.PingObject(view.gameObject);
					}
					if (GUILayout.Button("del", GUILayout.Width(50)))
					{
						uidProp.stringValue = "";
						nameProp.stringValue = "";
						sceneProp.stringValue = "";
					}
				}
			}
			EditorGUI.EndProperty();
		}
	}
}