﻿using Code.GameCore.EntitySystem.Entities.Base;
using Kingmaker.Blueprints;
using Kingmaker.Code.Editor.Utility;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.View;
using System;
using System.Linq;
using Code.GameCore.Mics;
using Kingmaker.Editor.UIElements.Custom.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Kingmaker.Utility.UnityExtensions;
using UnityEngine.SceneManagement;
using Color = System.Drawing.Color;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class EntityReferenceProperty : OwlcatProperty
	{
		private VisualElement m_NotFoundLabel;
		
		public EntityReferenceProperty(SerializedProperty property) : base(property, Layout.Vertical)
		{
			HeaderContainer.style.display = DisplayStyle.None;
			ControlsContainer.style.display = DisplayStyle.None;
			m_UidProp = Property.FindPropertyRelative("UniqueId");
			m_NameProp = property.FindPropertyRelative("EntityNameInEditor");
			m_SceneProp = property.FindPropertyRelative("SceneAssetGuid");
			m_NotFoundLabel = new Label("NOT FOUND");
			m_NotFoundLabel.style.backgroundColor = Color.Brown.ToUnityColor();
			ValidateProps();

			var objItem = CreateNameField(property, m_NameProp);
			var sceneItem = CreateSceneField(m_SceneProp);

			ContentContainer.Add(m_NotFoundLabel);
			ContentContainer.Add(objItem);
			ContentContainer.Add(sceneItem);
			m_SceneControls.SetEnabled(!m_SceneProp.stringValue.IsNullOrEmpty());

			CheckMissingReference();
			
			ContentContainer.RegisterCallback<MouseDownEvent>(e =>
			{
				if (m_SceneField.value != null)
				{
					e.StopPropagation();
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent("Open Scene"), false, () => AssetDatabase.OpenAsset(m_SceneField.value));
					menu.AddItem(new GUIContent("Open and Find"), false, () =>
					{
						AssetDatabase.OpenAsset(m_SceneField.value);
						OnClickFind();
					});
					menu.AddItem(new GUIContent("Del"), false, OnClickDel);
					
					menu.ShowAsContext();
				}
			});

			AddComponent(new DragAndDropComponent(() => GetValidComponent() != null, ApplyDrop));
		}

		TextField m_ObjectName;

		ObjectField m_SceneField;

		VisualElement m_SceneControls;

		Type m_TypeRestriction;

		SerializedProperty m_NameProp;

		SerializedProperty m_SceneProp;

		SerializedProperty m_UidProp;

		private void CheckMissingReference()
		{
			var uid = m_UidProp.hasMultipleDifferentValues ? "" : m_UidProp.stringValue;
			if (IsRelatedSceneLoaded() && 
			    EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid) == null)
			{
				m_NotFoundLabel.style.display = DisplayStyle.Flex;
			}
			else
			{
				m_NotFoundLabel.style.display = DisplayStyle.None;
			}
		}

		void ValidateProps()
		{
			var fieldInfo = Property.GetFieldInfo();

			var uid = m_UidProp.hasMultipleDifferentValues ? "" : m_UidProp.stringValue;
			var typeAttr =
				fieldInfo.GetCustomAttributes(typeof(AllowedEntityTypeAttribute), true)
					.OfType<AllowedEntityTypeAttribute>()
					.FirstOrDefault();
			m_TypeRestriction = (typeAttr != null) && (typeAttr.Type != null) && typeAttr.Type.IsSubclassOf(typeof(EntityViewBase))
				? typeAttr.Type
				: typeof(EntityViewBase);

			if (m_NameProp.stringValue == "" && !string.IsNullOrEmpty(uid))
			{
				var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
				m_NameProp.stringValue = view?.GO ? view.GO.name : "-not found-";
			}

			if (m_SceneProp.stringValue.IsNullOrEmpty() && !string.IsNullOrEmpty(uid))
			{
				var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
				if (view != null)
				{
					m_SceneProp.stringValue = AssetDatabase.AssetPathToGUID(view.GO.scene.path);
				}
			}
		}

		VisualElement CreateNameField(SerializedProperty property, SerializedProperty nameProp)
		{
			var objItem = CreteNameTextField(property, nameProp);
			m_SceneControls = CreateNameControls();

			var objRoot = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
			objRoot.Add(objItem);
			objRoot.Add(m_SceneControls);

			return objRoot;
		}

		VisualElement CreteNameTextField(SerializedProperty property, SerializedProperty nameProp)
		{
			var objItem = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
			var objLabel = new Label(property.displayName) { style = { unityTextAlign = TextAnchor.MiddleCenter } };
			m_ObjectName = new TextField() { value = nameProp.stringValue, style = { flexGrow = 1 } };
			objItem.Add(objLabel);
			objItem.Add(m_ObjectName);

			objItem.SetEnabled(false);
			return objItem;
		}

		VisualElement CreateSceneField(SerializedProperty sceneProp)
		{
			var sceneItem = new VisualElement() { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
			var sceneLabel = new Label("Scene") { style = { unityTextAlign = TextAnchor.MiddleCenter } };

			var sceneAsset =
				AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneProp.stringValue);
			if (sceneAsset == null)
				sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneProp.stringValue));
			
			m_SceneField = new ObjectField()
			{
				value = sceneAsset,
				objectType = typeof(SceneAsset),
				style = {flexGrow = 1}
			};
			sceneItem.Add(sceneLabel);
			sceneItem.Add(m_SceneField);

			sceneItem.SetEnabled(false);
			return sceneItem;
		}

		VisualElement CreateNameControls()
		{
			var controls = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
			var btnFind = new Button(OnClickFind) { text = "Find" };
			var btnDel = new OwlcatSmallButton(OnClickDel) { text = "X" };
			btnDel.AddToClassList("red-button");
			controls.Add(btnFind);
			controls.Add(btnDel);

			return controls;
		}

		private bool IsRelatedSceneLoaded()
		{
			var sceneName = m_SceneProp.stringValue;
			var scene = SceneManager.GetSceneByPath(sceneName);
			return scene.IsValid();
		}

		private bool TryFindGO(out GameObject result)
		{
			var uid = m_UidProp.stringValue;
			var view = EntityViewBaseCache.All.FirstOrDefault(v => v != null && v.UniqueViewId == uid);
			result = view?.GO;
			return result != null;
		}

		void OnClickFind()
		{
			if (!TryFindGO(out var go))
				Debug.Log("No object with id " + m_UidProp.stringValue);
			else
				EditorGUIUtility.PingObject(go);
		}

		void OnClickDel()
		{
			m_UidProp.stringValue = "";
			m_NameProp.stringValue = "";
			m_SceneProp.stringValue = "";
			m_SceneField.value = default;
			m_ObjectName.value = string.Empty;

			m_UidProp.serializedObject.ApplyModifiedProperties();
			m_SceneControls.SetEnabled(false);
		}

		void ApplyDrop()
		{
			var newObj = GetValidComponent();
			if (newObj != null)
			{
				m_UidProp.stringValue = newObj.UniqueId;
				m_NameProp.stringValue = newObj.name;
				m_ObjectName.value = newObj.name;

				var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(newObj.gameObject.scene.path);
				m_SceneProp.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
				m_SceneField.value = sceneAsset;
				m_SceneControls.SetEnabled(true);
				m_UidProp.serializedObject.ApplyModifiedProperties();
			}
			
			CheckMissingReference();
		}

		EntityViewBase GetValidComponent()
			=> DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is GameObject go ? go.GetComponent(m_TypeRestriction) as EntityViewBase : default;
	}
}