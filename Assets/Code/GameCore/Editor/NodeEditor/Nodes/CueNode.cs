using Code.GameCore.Editor.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Utility;
using Kingmaker.Editor.NodeEditor.Window;
using Kingmaker.ElementsSystem;
using Kingmaker.Localization;
using Owlcat.Editor.Core.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace Kingmaker.Editor.NodeEditor.Nodes
{
	public class CueNode : EditorNode<BlueprintCue>
	{
		public CueNode(Graph graph, BlueprintCue asset) : base(graph, asset, new Vector2(200, 80))
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			try
			{
				#if OWLCAT_MODS
				BlueprintCueValidationVisitor.UpdateSpeaker(asset);
				#else
				asset.UpdateSpeaker();
				#endif
			}
			catch (Exception e)
			{
				LocalizedString.Logger.Error(e);
			}
#endif
		}

		public override Color GetWindowColor()
		{
			return Colors.CueWindow;
		}

		public override string GetText()
		{
#if UNITY_EDITOR && EDITOR_FIELDS
			return Asset.Text.GetText(LocalizationManager.Instance.CurrentLocale);
#else
			return "";
#endif
		}

		protected override void DrawContent()
		{
			using (GuiScopes.UpdateObject(SerializedObject))
			{
				if (Asset.SoulMarkShift.Value != 0)
				{
					Profiler.BeginSample("Alignment");
					using (GuiScopes.Horizontal())
					{
						EditorGUIUtility.fieldWidth = 150;
						EditorGUILayout.PropertyField(FindProperty("SoulMarkShift.Direction"), new GUIContent());
						EditorGUIUtility.fieldWidth = 20;
						EditorGUILayout.PropertyField(FindProperty("SoulMarkShift.Value"), new GUIContent());
					}
					Profiler.EndSample();
				}

				if (Asset.Speaker.NoSpeaker)
				{
					GUILayout.Label("No Speaker", GUI.skin.button);
				}
				else if (Asset.Speaker.Blueprint != null)
				{
					Profiler.BeginSample("Speaker Field");
					using (GuiScopes.LabelWidth(40))
					{
						EditorGUILayout.PropertyField(FindProperty("Speaker.m_Blueprint"), new GUIContent());
					}
					Profiler.EndSample();
				}

#if UNITY_EDITOR && EDITOR_FIELDS
				Profiler.BeginSample("Find Text Property");
				var property = FindProperty("Text");
				Profiler.EndSample();

				LocalizationEditorGUI.LocalizedStringField(property, Asset.Text, LocalizationManager.Instance.CurrentLocale, Graph.ShowTagButtons);
#endif
			}
		}

        protected override IEnumerable<SimpleBlueprint> GetAllReferencedAssetsInternal()
		{
			return Asset.Continue.Cues.Dereference()
                .Cast<SimpleBlueprint>()
				.Concat(Asset.Answers.Dereference());
		}

		protected override SerializedProperty GetListProperty(Type type, SimpleBlueprint r = null)
		{
			if (typeof(BlueprintCueBase).IsAssignableFrom(type))
				return FindProperty("Continue.Cues");
			if (typeof(BlueprintAnswerBase).IsAssignableFrom(type))
				return FindProperty("Answers");
			return null;
		}

		public override IEnumerable<string> GetMarkers(bool extended)
		{
			if (Asset.ShowOnce)
				yield return "Once";
			if (Asset.Continue.Strategy == Strategy.Random)
				yield return "Random";
			if (Asset.Conditions.HasConditions)
				yield return ElementsDescription.Conditions(extended, Asset.Conditions);
			if (Asset.OnShow.HasActions || Asset.OnStop.HasActions)
				yield return ElementsDescription.Actions(extended, Asset.OnShow, Asset.OnStop);
#if UNITY_EDITOR && EDITOR_FIELDS
			if (Application.isPlaying && Game.Instance.Player.Dialog.ShownCues.Contains(Asset))
				yield return "Seen";
#endif
		}
	}
}