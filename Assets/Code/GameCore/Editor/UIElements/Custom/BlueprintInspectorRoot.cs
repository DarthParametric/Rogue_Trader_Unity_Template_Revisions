using System;
using Editors;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.Base;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.UIElements.Custom.Base;
using Kingmaker.Editor.UIElements.Custom.Prototypable;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.Utility.EditorPreferences;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.UIElements.Custom
{
	public class BlueprintInspectorRoot : OwlcatInspectorRoot
	{
		private Button m_SyncWithProtoBtn;

		private bool m_IgnoreNextChange;

		public SimpleBlueprint Blueprint
            => BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(SerializedObject.targetObject);
		
		private BlueprintScriptableObject BlueprintComplex
			=> BlueprintEditorWrapper.Unwrap<BlueprintScriptableObject>(SerializedObject.targetObject);

		
		public BlueprintInspectorRoot(SerializedObject serializedObject) : base(serializedObject, true)
		{
			if (!Blueprint)
			{
				throw new Exception(
					$"{nameof(BlueprintInspectorRoot)}(): {serializedObject.targetObject} isn't blueprint");
			}

            Root.Add(new IMGUIContainer(() => BlueprintInspector.DrawActionsForObject(Blueprint)));
            var type = new IMGUIContainer(() => BlueprintEditorUtility.ShowType("Script", Blueprint.GetType()));
			Root.Insert(0, type);
			
            var custom = BlueprintInspectorCustomGUI.GetForType(Blueprint.GetType());
            if (custom != null)
            {
                var header = new IMGUIContainer(() => custom.OnHeader(Blueprint));
                var mid = new IMGUIContainer(() => custom.OnBeforeComponents(Blueprint));
                Root.Add(header);
                header.SendToBack();
                Root.Add(mid);
            }

            CreateAdditionalMenu();
			// todo: [bp] fix prototype field
			//Allow moders create patches for NonOverridable blueprint types
			#if OWLCAT_MODS
			if (Blueprint is IHavePrototype prototype)
			#else
			if (Blueprint is IHavePrototype prototype && Blueprint.GetType().GetAttribute<NonOverridableAttribute>() == null)
		    #endif
			{
				var prop = SerializedObject.FindProperty("Blueprint");
				var protoProp = prop?.FindPropertyRelative("m_PrototypeId");
				if (protoProp != null)
				{
					var protoField = new PrototypeProperty(protoProp, Blueprint.GetType());
                    Root.Insert(Root.IndexOf(type) + 1, protoField);
					protoField.OnValueChangedEvent += PrototypeWarning;
				}
			}
			
			if (Blueprint is BlueprintScriptableObject)
            {
                var components = new ComponentsGroup(serializedObject) {style = {marginTop = 15}};
                Add(components);
            }

			if (custom != null)
            {
                var footer = new IMGUIContainer(() => custom.OnFooter(Blueprint));
                Root.Add(footer);
            }

			CreateSaveDiscardMenu();
		}

		private void CreateAdditionalMenu()
		{
			var menuRoot = new VisualElement();
			menuRoot.style.flexDirection = FlexDirection.Row;
			menuRoot.AddToClassList("labelPart");
			CreateScanBtn(menuRoot);
			CreateBaseButton(menuRoot);
			CreatePrototypableBtn(menuRoot);
			hierarchy.Insert(0, menuRoot);
		}

		private void CreateSaveDiscardMenu()
		{
			var menuRoot = new VisualElement();
			menuRoot.style.flexDirection = FlexDirection.Row;
			menuRoot.AddToClassList("labelPart");

			string id = Blueprint?.AssetGuid;
			var saveBtn = new Button(() =>
			{
				if (id != null)
				{
					BlueprintsDatabase.Save(id);
				}
			}) {text = "Save"};
			
			saveBtn.name = "SaveButton";
			saveBtn.AddToClassList("grow");
			menuRoot.Add(saveBtn);
			
			if (EditorPreferences.Instance.ProjectIsModTemplate)
			{
				var saveAsPatchBtn = new Button(
					() =>
					{
						BlueprintPatchEditorUtility.SavePatch(BlueprintComplex);
					}
				) { text = "Save as patch" };
				saveAsPatchBtn.name = "SavePatchButton";
				saveAsPatchBtn.AddToClassList("grow");
				menuRoot.Add(saveAsPatchBtn);
			}

			var discardBtn = new Button(Discard) {text = "Discard"};

			discardBtn.name = "DiscardButton";
			discardBtn.AddToClassList("grow");
			menuRoot.Add(discardBtn);

			hierarchy.Add(menuRoot);
		}


		private void Discard()
		{
			string? id = Blueprint?.AssetGuid;
			if (id == null)
				return;
			
			BlueprintsDatabase.Discard(id);
			
			SerializedObject.Update();
			var compGroup = this.Q<ComponentsGroup>();
			if (compGroup != null)
			{
				compGroup.UpdateComponents();
			}
			
		}

		private void CreateScanBtn(VisualElement menuRoot)
		{
			if (Blueprint is IBlueprintScanner scaner)
			{
				var scanBtn = new Button() { text = "Scan" };
				scanBtn.clicked += () =>
				{
					scaner.Scan();
                    BlueprintsDatabase.SetDirty(Blueprint.AssetGuid);
					SerializedObject.Update();
				};

				scanBtn.AddToClassList("grow");
				menuRoot.Add(scanBtn);
			}
		}

		private void CreateBaseButton(VisualElement menuRoot)
		{
			var windowBtn = new Button(() => BlueprintInspectorWindow.OpenFor(Blueprint)) { text = "New Window" };
			var findRefBtn = new Button(() =>
			{
				ReferencesWindow.ReferencesWindow.FindReferencesInProject(Blueprint);
			}) { text = "Find References" };

			windowBtn.AddToClassList("grow");
			findRefBtn.AddToClassList("grow");
			menuRoot.Add(windowBtn);
			menuRoot.Add(findRefBtn);
		}

		private void CreatePrototypableBtn(VisualElement menuRoot)
		{
			//Allow moders create patches for NonOverridable blueprint types
			#if OWLCAT_MODS
			if (Blueprint is BlueprintScriptableObject bso)
			#else 
            if (Blueprint is BlueprintScriptableObject bso && !Blueprint.GetType().HasAttribute<NonOverridableAttribute>())
            #endif 
            {
                var createInherited = new Button(() => PrototypableUtility.CreateInheritedAsset(bso)) { text = "Create inherited" };

				var syncChildBtn = new Button(() => PrototypableUtility.SyncWithChildren(bso)) { text = "Sync children" };
				m_SyncWithProtoBtn = new Button(()=> BlueprintEditorWrapper.SyncWithProto(Blueprint as BlueprintScriptableObject)) { text = "Sync with proto" };
                m_SyncWithProtoBtn.style.display = bso.PrototypeLink == null ? DisplayStyle.None : DisplayStyle.Flex;

                createInherited.AddToClassList("grow");
                syncChildBtn.AddToClassList("grow");
                m_SyncWithProtoBtn.AddToClassList("grow");

                menuRoot.Add(createInherited);
                menuRoot.Add(syncChildBtn);
                menuRoot.Add(m_SyncWithProtoBtn);
            } 
        }

		private void CreateProtoField()
		{
			var field = SerializedObject.FindProperty("PrototypeLink");
			var protoField = OwlcatProperty.CreateDefault(field);
			protoField.RegisterCallback<ChangeEvent<Object>>(evnt => PrototypeWarning());
			protoField.SetEnabled(false);
			hierarchy.Insert(1, protoField);
		}

		private void PrototypeWarning()
		{
			if (m_IgnoreNextChange)
			{
				m_IgnoreNextChange = false;
				return;
			}

			if (EditorUtility.DisplayDialog(
					   "Change prototype",
					   "Changing prototype link is a dangerous operation that may affect multiple blueprints. Are you sure?",
					   "Yes",
					   "No"))
			{
				BlueprintEditorWrapper.UpdateOverridesList(Blueprint as BlueprintScriptableObject);
				var compGroup = this.Q<ComponentsGroup>();
				if (compGroup != null)
				{
					compGroup.UpdateComponents();
				}

				EventBus.RaiseEvent<IOverrideProperty>(x => x.OnOverrideStateChanged(), false);
			}
			else
			{
				m_IgnoreNextChange = true;
            }
		}
	}
}