using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.Editor.NodeEditor.Nodes;
using Kingmaker.Localization;
using Newtonsoft.Json;
using UnityEngine;

namespace LocalizationTracker.Data.Shared
{
    [Serializable]
    public class DialogsData
    {
        [System.Diagnostics.CodeAnalysis.NotNull]
        [JsonProperty("name")]
        public string Name;

        [System.Diagnostics.CodeAnalysis.NotNull]
        [JsonProperty("nodes")]
        public List<Node> Nodes = new List<Node>();
        
        [JsonIgnore]
        public string FileSource { get; set; }

        public static string GetNicePath(ScriptableObject p)
        {
            return GetNicePath(BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(p));
        }
        
        public static string GetNicePath(SimpleBlueprint p)
        {
            return BlueprintsDatabase.GetAssetPath(p)
                .Replace(".jbp", "")
                .Replace("\\", "/");;
        }

        [Serializable]
        public class Node
        {
            private Node() {}
            
            public Node(EditorNode editorNode)
            {
                Name = editorNode.GetName();
                if (editorNode.Parent != null)
                    Parents.Add(DialogsData.GetNicePath(editorNode.Parent.GetAsset()));
                
                switch (editorNode)
                {
                    case DialogNode dialog:
                        Kind = "root";
                        var bpDialog = BlueprintEditorWrapper.Unwrap<BlueprintDialog>(dialog.GetAsset());
                        Id = GetNicePath(bpDialog);
                        Comment = bpDialog.Comment;
                        Children.AddRange(bpDialog.FirstCue.Cues.Select(p => GetNicePath(p)));
                        break;
                        
                    case CueNode cue: 
                        Kind = "cue";
                        var bpCue = BlueprintEditorWrapper.Unwrap<BlueprintCue>(cue.GetAsset());
                        Id = GetNicePath(bpCue);
                        Text = new TextString()
                        {
                            Key = LocalizedString.Dereference(bpCue.Text)?.Key
                        };
                        
                        if (bpCue.Text?.Shared != null)
                        {
                            Shared = true;
                        }
                        
                        Children.AddRange(bpCue.Answers.Select(p => GetNicePath(p)));
                        Children.AddRange(bpCue.Continue.Cues.Select(p => GetNicePath(p)));
                        
                        if (bpCue.Speaker?.Blueprint != null)
                            Speaker = bpCue.Speaker.Blueprint.AssetName;
                        
                        Comment = bpCue.Comment;
                        break;
                    
                    case AnswersListNode answerList:
                        var bpAnswerList = BlueprintEditorWrapper.Unwrap<BlueprintAnswersList>(answerList.GetAsset());
                        Kind = "answerlist";
                        Id = GetNicePath(bpAnswerList);
                        Children.AddRange(bpAnswerList.Answers.Select(p => GetNicePath(p)));
                        Comment = bpAnswerList.Comment;
                        break;
                    
                    case AnswerNode answer: 
                        Kind = "answer";
                        var bpAnswer = BlueprintEditorWrapper.Unwrap<BlueprintAnswer>(answer.GetAsset());
                        Id = GetNicePath(bpAnswer);
                        Text = new TextString()
                        {
                            Key = LocalizedString.Dereference(bpAnswer.Text)?.Key
                        };
                        
                        if (bpAnswer.Text?.Shared != null)
                        {
                            Shared = true;
                        }
                        
                        Children.AddRange(bpAnswer.NextCue.Cues.Select(p => GetNicePath(p)));
                        Comment = bpAnswer.Comment;
                        break;
                    
                    case CueSequenceNode cueSequence:
                        Kind = "cuesequence";
                        var bpCueSequence = BlueprintEditorWrapper.Unwrap<BlueprintCueSequence>(cueSequence.GetAsset());
                        Id = GetNicePath(bpCueSequence);
                        Children.AddRange(bpCueSequence.Cues.Select(p => GetNicePath(p)));
                        Children.Add(GetNicePath(bpCueSequence.Exit));
                        Comment = bpCueSequence.Comment;
                        break;
                    
                    case CueSequenceExitNode cueSequenceExit:
                        Kind = "sequenceexit";
                        var bpCueSequenceExit = BlueprintEditorWrapper.Unwrap<BlueprintSequenceExit>(cueSequenceExit.GetAsset());
                        Id = GetNicePath(bpCueSequenceExit);
                        Children.AddRange(bpCueSequenceExit.Answers.Select(p => GetNicePath(p)));
                        Comment = bpCueSequenceExit.Comment;
                        break;
                }
            }
            
            [System.Diagnostics.CodeAnalysis.NotNull]
            [JsonProperty("name")]
            public string Name;

            [System.Diagnostics.CodeAnalysis.NotNull]
            [JsonProperty("id")]
            public string Id;

            [System.Diagnostics.CodeAnalysis.NotNull]
            [JsonProperty("kind")]
            public string Kind;

            [System.Diagnostics.CodeAnalysis.NotNull]
            [JsonProperty("parents")]
            public List<string> Parents = new List<string>();

            [System.Diagnostics.CodeAnalysis.NotNull]
            [JsonProperty("children")]
            public List<string> Children = new List<string>();

            [JsonProperty("text", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public TextString? Text;
            
            [JsonProperty("speaker", DefaultValueHandling = DefaultValueHandling.Ignore)]
            [CanBeNull]
            public string Speaker;

            [JsonProperty("emotion", DefaultValueHandling = DefaultValueHandling.Ignore)]
            [CanBeNull]
            public string Emotion;

            [JsonProperty("text_is_shared", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool Shared;

            [JsonProperty("comment")]
            [CanBeNull]
            public string Comment { get; set; }
        }
        
        [Serializable]
        public class TextString
        {
            [JsonProperty("Namespace", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string? Namespace;

            [JsonProperty("Key")]
            public string? Key;
        }
    }
}

