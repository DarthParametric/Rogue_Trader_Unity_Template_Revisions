using Code.GameCore.Editor.CodeExtensions;
using System.Xml.Serialization;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Attributes;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.Quests;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Cutscenes;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.UI.Models.Tooltip;
using Kingmaker.UI.Models.Tooltip.Base;
using Owlcat.Editor.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Workspace
{
	public class WorkspaceItemBlueprint : WorkspaceItemBase
	{
		public string PathSaved;
		private string m_GuidSaved;
		private SimpleBlueprint m_Blueprint;

		public string BlueprintGuid
		{
			get { return m_GuidSaved; }
			set
			{
				Blueprint = BlueprintsDatabase.LoadById<SimpleBlueprint>(value);
				m_GuidSaved = value;
			}
		}

		[XmlIgnore]
		public SimpleBlueprint Blueprint
		{
			get { return m_Blueprint; }
			set
			{
				m_Blueprint = value;
				UpdateGUIContent();
			}
		}

		public override Vector2 Measure()
		{
			return OwlcatEditorStyles.Instance.BlueprintItem.CalcSize(GUIContent);
		}

		public override void DoubleClick()
		{
			if (Blueprint is BlueprintDialog || Blueprint is BlueprintCueBase || Blueprint is BlueprintAnswerBase)
			{
				DialogEditor.FocusAsset(Blueprint as BlueprintDialog, (Object)null);
			}
			else if (Blueprint is BlueprintQuest)
			{
				QuestEditor.Focus(Blueprint as BlueprintQuest, null);
			}
			else if (Blueprint is Gate)
			{
				CutsceneEditorWindow.OpenAssetInCutsceneEditor(Blueprint);
			}
			else if (Blueprint is BlueprintScriptableObject)
			{
				BlueprintNodeEditor.OpenNewAsset(Blueprint);
			}
			else
			{
#if UNITY_EDITOR && EDITOR_FIELDS
				BlueprintInspectorWindow.OpenFor(Blueprint);
#endif
			}
		}

		public override void Click()
		{
			Selection.activeObject = BlueprintEditorWrapper.Wrap(Blueprint);
		}

		public override Object GetDraggedObject()
		{
            return BlueprintEditorWrapper.Wrap(Blueprint);
        }

		public override void ShowContextMenu()
	    {
	        if (Blueprint)
	        {
                // tried to open unity context menu here, but it's kinda hard:
                // context menu entries may be found in any class marked with [MenuItem], or
                // in any blueprint parent class marked with [ContextMenu]
                // Easier to just make our own menu here
	            var menu = new GenericMenu();

				var area = Blueprint as BlueprintArea;
				if (area)
				{
					var window = EditorWindow.GetWindow<WorkspaceCanvasWindow>();
					if (window.ActiveArea != area)
					{
						menu.AddItem(new GUIContent("Show area info"), false, () => window.ActiveArea = area);
					}
				}

				if (Blueprint is BlueprintDialog || Blueprint is BlueprintCueBase || Blueprint is BlueprintAnswerBase)
                {
                    menu.AddItem(
                        new GUIContent("Open in dialog editor"),
                        false,
                        () =>
                            DialogEditor.FocusAsset(Blueprint as BlueprintDialog, (Object)null));
                }
                else if (Blueprint is Gate)
                {
                    menu.AddItem(
                        new GUIContent("Open in cutscene editor"),
                        false,
                        () =>
                            CutsceneEditorWindow.OpenAssetInCutsceneEditor(Blueprint));
                }
                else if (Blueprint is BlueprintAreaPart)
				{
                    menu.AddItem(
                        new GUIContent("Open in scene view"),
                        false,
                        () =>
                            ((BlueprintAreaPart)Blueprint).OpenInEditorWindow());
                }
                menu.AddItem(
                    new GUIContent("Open in node editor"),
                    false,
                    () => BlueprintNodeEditor.OpenNewAsset(Blueprint));

				menu.ShowAsContext();
			}
	    }

	    protected override void UpdateGUIContent()
	    {
		    var attr = Blueprint ? Blueprint.GetType().GetAttribute<WorkspaceVisualAttribute>() : null;
		    var uiDataProvider = Blueprint as IUIDataProvider;
		    var icon = uiDataProvider?.Icon.Or(null)?.texture;
		    
		    if (!icon && Blueprint)
		    {
			    string name = "BlueprintIcons/" + Blueprint.GetType().Name + ".png";
			    icon = EditorGUIUtility.Load(name) as Texture2D;
			    icon = icon.Or(OwlcatEditorStyles.Instance.BlueprintItemIcon);
		    }

		    GUIStyle = OwlcatEditorStyles.Instance.BlueprintItem;

		    BackgroundColor = Target != null
			    ? TargetBackgroundColor
			    : attr == null ? Color.white : new Color(attr.R, attr.G, attr.B);
		    
		    string text = TargetPrefix + (Blueprint ? Blueprint.name : "??");
            if (Blueprint != null)
            {
                PathSaved = BlueprintsDatabase.GetAssetPath(Blueprint);
				m_GuidSaved = Blueprint.AssetGuid;
            }
			string tooltip = PathSaved;
		    GUIContent = new GUIContent(text, icon, tooltip);
		    
	    }
	}
}