using Code.GameCore.Editor.CodeExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Kingmaker.Blueprints.JsonSystem.PropertyUtility;
using Kingmaker.Editor.Blueprints;
using Kingmaker.Editor.Cutscenes.CommandTest;
using Kingmaker.Editor.DragDrop;
using Kingmaker.Editor.Elements.SmartElementPopulation;
using Kingmaker.Editor.Utility;
using Kingmaker.ElementsSystem;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.Utility.DotNetExtensions;
using Owlcat.Editor.Core.Utility;
using Owlcat.Editor.Utility;
using Owlcat.Runtime.Core.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kingmaker.Editor.Cutscenes
{
    public class CutsceneEditorWindow : KingmakerWindowBase, ICutsceneHandler, IAreaHandler
    {
        class ColorsList
        {
            public readonly Color DragPoint;
            public readonly Color Selection;
            public readonly Color DebugActive = new Color(0.13f, 1f, 0.13f);
            public readonly Color DebugStopCommand = new Color(0.92f, 0.08f, 0.22f);
            public readonly Color DebugCommandCheckFailed = new Color(0.33f, 0.41f, 0.13f);
            public readonly Color GateUnreachable = new Color(0.92f, 0.82f, 0.08f);
            public readonly Texture PlayButton;
            public readonly Texture PauseButton;
            public readonly Texture StopButton;

            public ColorsList()
            {
                if (EditorGUIUtility.isProSkin)
                {
                    DragPoint = new Color(0, 1, 1);
                    Selection = new Color(0, 1, 1);
                    PlayButton = (Texture)EditorGUIUtility.LoadRequired("d_PlayButton");
                    PauseButton = (Texture)EditorGUIUtility.LoadRequired("d_PauseButton");
                    StopButton = (Texture)EditorGUIUtility.LoadRequired("d_StopButton.png");
                }
                else
                {
                    DragPoint = new Color(0, 0, 1);
                    Selection = new Color(0, 0, 1);
                    PlayButton = (Texture)EditorGUIUtility.LoadRequired("PlayButton");
                    PauseButton = (Texture)EditorGUIUtility.LoadRequired("PauseButton");
                    StopButton = (Texture)EditorGUIUtility.LoadRequired("StopButton.png");
                }
            }

        }

        private ColorsList m_Colors;

        private CutsceneReference m_CutsceneReference;

        public Cutscene Cutscene 
        { 
            get  => m_CutsceneReference;
            private set => m_CutsceneReference = CutsceneReference.CreateTyped<CutsceneReference>(value);
        }

        private object m_SelectedObject; // may be a gate, track or command

#pragma warning disable 414
        private Rect? m_SelectedObjectRect;
#pragma warning restore 414

        Layout m_Layout;

        private Vector2 m_ScrollPos;

        private static readonly List<Type> s_CommandTypes;

        private CutscenePlayerView m_DebuggedPlayer;
        public CutscenePlayerView DebuggedPlayer
            => m_DebuggedPlayer;

        bool m_NeedDestroyLastScene;

        private Action m_DrawDebugDelayed = () => { };

        private GateReactivationDrawer m_GateReactivationDrawer = new GateReactivationDrawer();

        private static object m_CutsceneObj;

#pragma warning disable 414
        private bool m_HierarchyCopy = false;
        
        private CutsceneEditorMiniConsole m_MiniConsole;
        public bool MiniConsoleActive = false;

        private const string DefaultTitle = "Cutscene Editor";

#pragma warning restore 414

        static CutsceneEditorWindow()
        {
            s_CommandTypes = TypeCache.GetTypesDerivedFrom(typeof(CommandBase))
                .Where(et => !et.HasAttribute<ObsoleteAttribute>())
                .OrderBy(t => t.Name)
                .ToList();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_MiniConsole = new CutsceneEditorMiniConsole(this);
            MiniConsoleActive = false;
            m_Colors = new ColorsList();
            UndoManager.Instance.UndoRedoPerformed += HandleUndoRedo;
            Selection.selectionChanged += HandleSelectionChanged;

            m_SelectedObject = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(Selection.activeObject);
            m_SelectedObjectRect = null;
            EventBus.Subscribe(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UndoManager.Instance.UndoRedoPerformed -= HandleUndoRedo;
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= HandleSelectionChanged;
            EventBus.Unsubscribe(this);
        }
        
        void ICutsceneHandler.HandleCutsceneStarted(bool queued) 
        {
            var cutscene = EventInvokerExtensions.Entity as CutscenePlayerData;
            if (cutscene.Cutscene == Cutscene && DebuggedPlayer == null)
                SetDebuggedPlayer(cutscene.View);
                
        }
        void ICutsceneHandler.HandleCutsceneRestarted() 
        {
        }
        void ICutsceneHandler.HandleCutscenePaused(CutscenePauseReason reason)
        {
            m_MiniConsole.ConsoleShowMode = CutsceneEditorMiniConsole.ShowMode.PauseReason;
            MiniConsoleActive = true;
        }
        void ICutsceneHandler.HandleCutsceneResumed()
        {
        }
        void ICutsceneHandler.HandleCutsceneStopped() 
        {
            var cutscene = EventInvokerExtensions.Entity as CutscenePlayerData;
            if (m_DebuggedPlayer != null && cutscene.View == m_DebuggedPlayer)
                m_NeedDestroyLastScene = true;
        }

        void IAreaHandler.OnAreaBeginUnloading()
        {
            if (m_DebuggedPlayer != null && m_NeedDestroyLastScene)
            {
                m_DebuggedPlayer.Data.Dispose();
            }
        }

        void IAreaHandler.OnAreaDidLoad() { }

        private void HandleSelectionChanged()
        {
            if (!(m_SelectedObject is Track) || Selection.activeObject != null)
            {
                m_SelectedObject = BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(Selection.activeObject);
                m_SelectedObjectRect = null;
            }
        }

        private void HandleUndoRedo()
        {
            m_Layout = null;
        }

        private static Dictionary<CutsceneEditorWindow, CutscenePlayerView> s_OpenedWindowsDebugging = new Dictionary<CutsceneEditorWindow, CutscenePlayerView>();

        private static bool CheckCutscenePlayerAlreadyOpen(CutscenePlayerView cutscenePlayer, out CutsceneEditorWindow openedWindow)
        {
            openedWindow = null;
            foreach (var (window, player) in s_OpenedWindowsDebugging)
            {
                if (player == cutscenePlayer)
                {
                    openedWindow = window;
                    break;
                }
            }

            return (openedWindow != null);
        }

        [MenuItem("Design/Cutscene Editor", false, 2001)]
        //Menu button opens any already opened window. Or creates a new one if there are none
        public static void OpenCutsceneEditor()
        {
            GetWindow<CutsceneEditorWindow>(DefaultTitle, true);
        }

        //if player is already debugging in opened window, then focus on it. Otherwise create a new one
        public static CutsceneEditorWindow OpenPlayerInEditor([CanBeNull] CutscenePlayerView cutscenePlayer)
        {

            CutsceneEditorWindow window;
            if (cutscenePlayer != null)
            {
                if (CheckCutscenePlayerAlreadyOpen(cutscenePlayer, out window))
                {
                    window.Focus();
                }
                else
                {
                    window = CreateWindow<CutsceneEditorWindow>();
                    window.SetDebuggedPlayer(cutscenePlayer);

                }
                window.Repaint();
                Selection.activeGameObject = cutscenePlayer.gameObject;
            }
            else
            {
                 window = CreateWindow<CutsceneEditorWindow>(DefaultTitle);
            }

            return window;
        }

        public static void OpenAssetInCutsceneEditor(Object obj)
        {
            if (!obj)
                return;

            OpenAssetInCutsceneEditor(BlueprintEditorWrapper.Unwrap<SimpleBlueprint>(obj));
        }

        public void SetDebuggedPlayer(CutscenePlayerView cutscenePlayer)
        {
            s_OpenedWindowsDebugging[this] = cutscenePlayer;
            m_DebuggedPlayer = cutscenePlayer;
            Open(m_DebuggedPlayer.Cutscene);
            m_DebuggedPlayer.PlayerData.PreventDestruction = true;
            m_DebuggedPlayer.PlayerData.TraceCommands = true;
            m_NeedDestroyLastScene = false;
            
            if (cutscenePlayer)
                Selection.activeGameObject = cutscenePlayer.gameObject;
        }
        
        [BlueprintContextMenu("Open in Cutscene Editor", BlueprintType = typeof(Gate))]
        [BlueprintContextMenu("Open in Cutscene Editor", BlueprintType = typeof(CommandBase))]
        public static void OpenAssetInCutsceneEditor(SimpleBlueprint bp)
        {
            if (!bp)
                return;

            string assetPath = BlueprintsDatabase.GetAssetPath(bp);
            string directory = Path.GetDirectoryName(assetPath);
            var ct = BlueprintsDatabase.LoadAllOfType<Cutscene>(directory);
            foreach (var cutscene in ct)
            {
                var window = GetWindow<CutsceneEditorWindow>();
                window.Open(cutscene);
                return;
            }
        }

        private void Open(Cutscene cutscene)
        {
            Cutscene = cutscene;
            //if was debugging other cutscene and open new one
            if (m_DebuggedPlayer != null && m_DebuggedPlayer.Cutscene != Cutscene)
            {
                m_DebuggedPlayer = null;
                
            }
            DoLayout();
            titleContent = new GUIContent(cutscene.name);
            Show();
            Repaint();
        }

        private void OnDestroy()
        {
            s_OpenedWindowsDebugging.Remove(this);
        }

        private bool IsDebugging
            => Application.isPlaying
               && !ReferenceEquals(m_DebuggedPlayer, null)
               && m_DebuggedPlayer.Cutscene == Cutscene
               && m_DebuggedPlayer.PlayerData != null;

        private void DoLayout()
        {
            m_Layout = m_Layout ?? new Layout();
            m_Layout.DoLayout(Cutscene);
            m_SelectedObjectRect = null;
        }

        private void OnInspectorUpdate()
        {
            if (IsDebugging)
                Repaint();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (m_Layout == null && Cutscene)
                DoLayout();

            DrawToolbar();

            if (Cutscene)
            {
                using (var scope = new EditorGUILayout.ScrollViewScope(m_ScrollPos))
                {
                    scope.handleScrollWheel = false;

                    if (Event.current.type != EventType.Ignore && Event.current.type != EventType.MouseEnterWindow) // this is magic.
                        m_ScrollPos = scope.scrollPosition; 
                    
                    // scroll using mmb                
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 2)
                    {
                        Event.current.Use();
                        m_ScrollPos -= Event.current.delta;
                        Repaint();
                        return;
                    }
                    if (Event.current.type == EventType.ScrollWheel)
                    {
                        Event.current.Use();
                        m_ScrollPos.x += Event.current.delta.y * 10; // use scroll wheel to scroll HORIZONTALLY
                        Repaint();
                        return;
                    }

                    // ReSharper disable once PossibleNullReferenceException (DoLayout ensures layout's not null)
                    // layout a rect big enough to fit all elements inside
                    GUILayoutUtility.GetRect(m_Layout.Width + 40, m_Layout.Width + 40, m_Layout.Height, m_Layout.Height);

                    // draw gates and tracks
                    foreach (var gate in m_Layout.GatesByDepth.EmptyIfNull())
                    {
                        var rect = m_Layout.GetRect(gate);
                        DrawGate(gate, rect);

                        for (int ti = 0; ti < gate.StartedTracks.Count; ti++)
                        {
                            var track = gate.StartedTracks[ti];
                            var rect1 = m_Layout.GetRect(track);
                            if (track != DragManager.Instance.DraggedObject)
                                DrawTrack(rect1, track);
                            DrawTracklistDropPoint(rect1, ti + 1, gate);
                            DrawTracklistCommandDropPoint(rect1, ti + 1, gate);
                        }
                    }

                    // draw gates flags (any/all) on top
                    foreach (var gate in m_Layout.GatesByDepth.EmptyIfNull())
                    {
                        if (gate == Cutscene)
                            continue;
                        var rect = m_Layout.GetRect(gate);
                        if (m_Layout.IsGateSplit(gate))
                            continue;

                        var style = gate.Op == Operation.And
                            ? OwlcatEditorStyles.Instance.GateFlagAnd
                            : OwlcatEditorStyles.Instance.GateFlagOr;
                        using (GuiScopes.Color(gate.Color))
                        {
                            GUI.Box(new Rect(rect.center.x - 16, rect.yMin - 10, 32, 15), "", style);
                        }
                    }

                    // draw track signal links on top
                    foreach (var gate in m_Layout.GatesByDepth.EmptyIfNull())
                    {
                        foreach (var track in gate.StartedTracks)
                        {
                            if (track.EndGate.EditorEndGate())
                            {
                                var trackRect = m_Layout.GetRect(track);
                                var nextGateRect = m_Layout.GetRect(track.EndGate.EditorEndGate());
                                var from = trackRect.xMax;
                                var to = nextGateRect.xMin;
                                var r = new Rect(
                                    from,
                                    trackRect.center.y - 8,
                                    to - from,
                                    16);
                                var style = track.IsContinuous
                                    ? OwlcatEditorStyles.Instance.TrackRepeatArrow
                                    : OwlcatEditorStyles.Instance.TrackArrow;
                                style.Draw(r);
                                ObjectSelectionButton(track, r, trackRect);

                                if (IsDebugging)
                                {
                                    var signalRect = new Rect(to - 6, trackRect.center.y - 12, 12, 24);
                                    if (m_DebuggedPlayer.PlayerData.IsTrackFinished(track))
                                    {
                                        OwlcatEditorStyles.Instance.GateSignalOn.Draw(signalRect);
                                    }
                                    else if (!track.IsContinuous)
                                    {
                                        OwlcatEditorStyles.Instance.GateSignalOff.Draw(signalRect);
                                    }
                                }
                            }
                        }
                    }

                    // orphan commands
                    DrawCommandDrawer();


                    // draw selection rect and selected object buttons
                    if (m_SelectedObjectRect.HasValue)
                    {
                        OwlcatEditorStyles.Instance.SelectionRect.Draw(m_SelectedObjectRect.Value, m_Colors.Selection);
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        m_GateReactivationDrawer.Draw();
                        m_DrawDebugDelayed?.Invoke();
                    }
                    m_DrawDebugDelayed = null;
                    m_GateReactivationDrawer.Clear();
                }

                if (MiniConsoleActive)
                {
                    GUILayout.FlexibleSpace();
                    m_MiniConsole.DrawHelpfulInfo();
                    if (m_MiniConsole.NeedRepaint)
                    {
                        m_MiniConsole.NeedRepaint = false;
                        Repaint();
                    }
                }
            }

            // scroll window if dragging near scroll border
            if (DragManager.Instance.DragInProgress)
            {
                var pos = Event.current.mousePosition;
                if (pos.x < 10)
                {
                    m_ScrollPos.x -= Mathf.Max(10 - Mathf.Max(pos.x, 0), 2) / 2;
                    Repaint();
                }
                if (pos.x > position.width - 10)
                {
                    m_ScrollPos.x += Mathf.Max(10 - Mathf.Max(position.width - pos.x, 0), 2) / 2;
                    Repaint();
                }
                if (pos.y < 30)
                {
                    m_ScrollPos.y -= Mathf.Max(30 - Mathf.Max(pos.y, 20), 2) / 2;
                    Repaint();
                }
                if (pos.y > position.height - 10)
                {
                    m_ScrollPos.y += Mathf.Max(10 - Mathf.Max(position.height - pos.y, 0), 2) / 2;
                    Repaint();
                }
            }

            // handle ctrl-D
            if (m_SelectedObject is CommandBase && Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == "Duplicate")
            {
                HandleDuplicate();
            }
            if (m_SelectedObject != null && Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == "SoftDelete")
            {
                HandleDelete();
            }
            if (Event.current.type == EventType.ValidateCommand)
            {
                //Debug.Log(Event.current.commandName);
                Event.current.Use();
            }
        }

        private void DrawDelayed(Action action)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_DrawDebugDelayed == null)
                {
                    m_DrawDebugDelayed = action;
                }
                else
                {
                    m_DrawDebugDelayed += action;
                }
            }
        }

        private void DrawCommandDrawer()
        {
            // draw orphaned commands
            foreach (var command in m_Layout.OrphanCommands)
            {
                var c = command ? command.GetCaption() : "???";

                var cdd = DragManager.Instance.DraggedObject as CommandDragData;
                if (cdd == null || cdd.Command != command)
                {
                    var cmdRect = m_Layout.GetRect(command);
                    var style = command.EntryCondition.HasConditions
                        ? OwlcatEditorStyles.Instance.CommandConditionMarker
                        : OwlcatEditorStyles.Instance.CommandBox;
                    GUI.Box(cmdRect, c, style);

                    if (Event.current.type == EventType.MouseDrag && cmdRect.Contains(Event.current.mousePosition))
                    {
                        var cr = new Rect(Vector2.zero, cmdRect.size);
                        DragManager.Instance.BeginDrag(
                            w => GUI.Box(cr, c, OwlcatEditorStyles.Instance.CommandBox),
                            new CommandDragData { Command = command, FromTrack = null },
                            cmdRect.size);
                    }

                    ObjectSelectionButton(command, cmdRect, cmdRect);
                }
            }
            // drop commands into drawer
            if (DragManager.Instance.IsDragging<CommandDragData>())
            {
                if (Event.current.mousePosition.y < m_Layout.DrawerTop)
                    return;
                if (Event.current.type != EventType.DragPerform)
                    return;
                var cdd = (CommandDragData)DragManager.Instance.DraggedObject;
                if (cdd.FromTrack == null)
                    return;

                RemoveCommand(cdd.Command);
                Event.current.Use();
            }
        }


        private void HandleDelete()
        {
            var track = m_SelectedObject as Track;
            if (track != null)
            {
                RemoveTrack(track);
            }
            var commandBase = m_SelectedObject as CommandBase;
            if (commandBase != null)
            {
                RemoveCommand(commandBase);
            }

            var gate = m_SelectedObject as Gate;
            if (gate && gate != Cutscene && m_Layout.IsGateOrphaned(gate))
            {
                BlueprintsDatabase.DeleteAsset(gate);
                DoLayout();
                Repaint();
            }
        }

        private void HandleDuplicate()
        {
            var command = (CommandBase)m_SelectedObject;
            var newCmd = (CommandBase)BlueprintsDatabase.DuplicateAsset(command);
            var track = m_Layout.FindTrackForCommand(command);
            if (track != null)
            {
                var gate = m_Layout.FindGateForTrack(track);
                track.Commands.Insert(track.Commands.IndexOf(command) + 1, newCmd);
                gate.SetDirtyUpdateTracks();
            }

            Event.current.Use();

            DoLayout();
            Repaint();
        }

        private bool DropPoint(Rect rect, Type acceptedObjectType)
        {
            if (DragManager.Instance.DragInProgress && acceptedObjectType.IsInstanceOfType(DragManager.Instance.DraggedObject))
            {
                if (!rect.Contains(Event.current.mousePosition))
                    return false;

                if (Event.current.type == EventType.Repaint)
                {
                    if (rect.width < rect.height)
                    {
                        var d = rect.width / 2 - 2;
                        rect.xMin += d;
                        rect.xMax -= d;
                    }
                    else
                    {
                        var d = rect.height / 2 - 2;
                        rect.yMin += d;
                        rect.yMax -= d;

                    }
                    EditorGUI.DrawRect(rect, m_Colors.DragPoint);
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    Event.current.Use();
                    return true;
                }
            }
            return false;
        }

        private void DrawTracklistDropPoint(Rect trackRect, int trackInsertionIndex, Gate gate)
        {
            var dropArea = new Rect(trackRect.xMin, trackRect.yMax - 5, trackRect.width, 10);
            if (!DropPoint(dropArea, typeof(Track)))
                return;

            var track = (Track)DragManager.Instance.DraggedObject;
            var fromGate = m_Layout.FindGateForTrack(track);
            if (!fromGate)
                return; // wtf?

            // adjust insertion index if removing track leads to changes in this very list
            if (fromGate == gate)
            {
                var idx = gate.StartedTracks.IndexOf(track);
                if (idx < trackInsertionIndex)
                    trackInsertionIndex--;
            }

            var old1 = fromGate.StartedTracks.ToList();
            var old2 = gate.StartedTracks.ToList();
            var old3 = track.EndGate.EditorEndGate();
            UndoManager.Instance.RegisterUndo(
                "track moved",
                () =>
                {
                    fromGate.StartedTracks = old1;
                    gate.StartedTracks = old2;
                    track.EndGate = old3;
                    fromGate.SetDirtyUpdateTracks();
                    gate.SetDirtyUpdateTracks();
                    Repaint();
                });


            fromGate.StartedTracks.Remove(track);
            gate.StartedTracks.Insert(trackInsertionIndex, track);

            // clear signal if it can cause cycles
            if (track.EndGate.EditorEndGate() &&
                m_Layout.GetGateDepth(track.EndGate.EditorEndGate()) <= m_Layout.GetGateDepth(gate))
                track.EndGate = null;

            fromGate.SetDirtyUpdateTracks();
            gate.SetDirtyUpdateTracks();

            DoLayout();
            Repaint();
        }

        private void DrawTracklistCommandDropPoint(Rect trackRect, int trackInsertionIndex, Gate gate)
        {
            var dropArea = new Rect(trackRect.xMin, trackRect.yMax - 5, trackRect.width, 10);
            if (!DropPoint(dropArea, typeof(CommandDragData)))
                return;

            var cdd = (CommandDragData)DragManager.Instance.DraggedObject;
            var fromGate = cdd.FromTrack == null ? null : m_Layout.FindGateForTrack(cdd.FromTrack);

            var old1 = fromGate ? cdd.FromTrack.Commands.ToList() : null;
            var old2 = gate.StartedTracks.ToList();
            UndoManager.Instance.RegisterUndo(
                "track created",
                () =>
                {
                    if (cdd.FromTrack != null)
                    {
                        cdd.FromTrack.Commands = old1;
                        fromGate?.SetDirtyUpdateTracks();
                    }
                    gate.StartedTracks = old2;
                    gate.SetDirtyUpdateTracks();
                    Repaint();
                });


            if (cdd.FromTrack != null)
                cdd.FromTrack.Commands.Remove(cdd.Command);
            var track = new Track();
            gate.StartedTracks.Insert(trackInsertionIndex, track);
            track.Commands.Add(cdd.Command);

            fromGate?.SetDirtyUpdateTracks();
            gate.SetDirtyUpdateTracks();
            DoLayout();
            Repaint();
        }

        private void DrawCommandDropPoint(Rect cmdRect, int cmdInsertionIndex, Track track)
        {
            var dropArea = new Rect(cmdRect.xMax - 5, cmdRect.yMin, 10, cmdRect.height);
            if (!DropPoint(dropArea, typeof(CommandDragData)) && !DropPoint(dropArea, typeof(Track)))
                return;

            var gate = m_Layout.FindGateForTrack(track);
            if (!gate)
                return; // wtf?

            var cdd = DragManager.Instance.DraggedObject as CommandDragData;
            if (cdd != null)
            {
                // dragging a command from one place to another
                var fromGate = cdd.FromTrack == null ? null : m_Layout.FindGateForTrack(cdd.FromTrack);

                if (!CanInsertCommand(track, cdd.Command, cmdInsertionIndex))
                {
                    ShowNotification(new GUIContent("Cannot add command: continuous command must be last."));
                    return;
                }

                // adjust insertion index if removing command leads to changes in this very list
                if (track == cdd.FromTrack)
                {
                    var idx = track.Commands.IndexOf(cdd.Command);
                    if (idx < cmdInsertionIndex)
                        cmdInsertionIndex--;
                }

                var old1 = fromGate ? cdd.FromTrack.Commands.ToList() : null;
                var old2 = track.Commands.ToList();
                UndoManager.Instance.RegisterUndo(
                    "command moved",
                    () =>
                    {
                        if (cdd.FromTrack != null)
                        {
                            cdd.FromTrack.Commands = old1;
                            fromGate?.SetDirtyUpdateTracks();
                        }
                        track.Commands = old2;
                        gate.SetDirtyUpdateTracks();
                        Repaint();
                    });


                if (cdd.FromTrack != null)
                    cdd.FromTrack.Commands.Remove(cdd.Command);
                track.Commands.Insert(cmdInsertionIndex, cdd.Command);

                fromGate?.SetDirtyUpdateTracks();
                gate.SetDirtyUpdateTracks();
            }

            var dropTrack = DragManager.Instance.DraggedObject as Track;
            if (dropTrack != null && dropTrack != track)
            {
                // dragging one track into another: merge tracks
                var fromGate = m_Layout.FindGateForTrack(dropTrack);
                fromGate.StartedTracks.Remove(dropTrack);
                for (int ii = 0; ii < dropTrack.Commands.Count; ii++)
                {
                    var command = dropTrack.Commands[ii];
                    track.Commands.Insert(cmdInsertionIndex + ii, command);
                }
                fromGate.SetDirtyUpdateTracks();
                gate.SetDirtyUpdateTracks();
            }

            DoLayout();
            Repaint();
        }

        private void DrawTrackSignalDropPoint(Rect gateRect, Gate gate)
        {
            var dropArea = new Rect(gateRect.xMin - 5, gateRect.yMin, 10, gateRect.height);
            if (!DropPoint(dropArea, typeof(Track)))
                return;

            var track = (Track)DragManager.Instance.DraggedObject;
            var fromGate = m_Layout.FindGateForTrack(track);
            if (!fromGate)
                return; // wtf?

            if (track.EndGate.EditorEndGate() == gate)
                return;

            if (!m_Layout.CanLink(fromGate, gate))
                return;

            var old = track.EndGate.EditorEndGate();
            UndoManager.Instance.RegisterUndo(
                "track moved",
                () =>
                {
                    track.EndGate = old;
                    fromGate.SetDirtyUpdateTracks();
                    Repaint();
                });
            track.EndGate = gate;

            fromGate.SetDirtyUpdateTracks();

            DoLayout();
            Repaint();
        }
        private void DrawTrackToGateDropPoint(Rect gateRect, Gate gate)
        {
            var dropArea = new Rect(gateRect.xMax - 5, gateRect.yMin, 10, gateRect.height);
            if (DropPoint(dropArea, typeof(Track)))
            {
                var track = (Track)DragManager.Instance.DraggedObject;
                var fromGate = m_Layout.FindGateForTrack(track);
                if (!fromGate)
                    return; // wtf?

                if (fromGate == gate)
                    return;

                var old1 = fromGate.StartedTracks.ToList();
                var old2 = gate.StartedTracks.ToList();
                UndoManager.Instance.RegisterUndo(
                    "track moved",
                    () =>
                    {
                        fromGate.StartedTracks = old1;
                        gate.StartedTracks = old2;
                        fromGate.SetDirtyUpdateTracks();
                        gate.SetDirtyUpdateTracks();
                        DoLayout();
                        Repaint();
                    });
                fromGate.StartedTracks.Remove(track);
                gate.StartedTracks.Add(track);

                fromGate.SetDirtyUpdateTracks();
                gate.SetDirtyUpdateTracks();

                DoLayout();
                Repaint();
            }

            if (DropPoint(dropArea, typeof(CommandDragData)))
            {
                var cdd = (CommandDragData)DragManager.Instance.DraggedObject;
                var fromGate = cdd.FromTrack != null ? m_Layout.FindGateForTrack(cdd.FromTrack) : null;

                var old1 = cdd.FromTrack != null ? cdd.FromTrack.Commands.ToList() : null;
                var old2 = gate.StartedTracks.ToList();
                UndoManager.Instance.RegisterUndo(
                    "command moved",
                    () =>
                    {
                        if (cdd.FromTrack != null)
                        {
                            cdd.FromTrack.Commands = old1;
                            fromGate?.SetDirtyUpdateTracks();
                        }
                        gate.StartedTracks = old2;
                        gate.SetDirtyUpdateTracks();
                        DoLayout();
                        Repaint();
                    });
                if (cdd.FromTrack != null)
                {
                    cdd.FromTrack.Commands.Remove(cdd.Command);
                    fromGate?.SetDirtyUpdateTracks();
                }
                var newTrack = new Track();
                newTrack.Commands.Add(cdd.Command);
                gate.StartedTracks.Add(newTrack);

                gate.SetDirtyUpdateTracks();

                DoLayout();
                Repaint();
            }
        }



        private void DrawTrack(Rect rect, Track track)
        {
            var activeTrack = IsDebugging ? m_DebuggedPlayer.PlayerData.GetTrackData(track) : null;
            var trackColor = activeTrack != null && activeTrack.IsPlaying ? m_Colors.DebugActive : GUI.color;

            // doubleclick
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount > 1 && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                track.IsCollapsed = !track.IsCollapsed;
                m_Layout.FindGateForTrack(track)?.SetDirty();
                DoLayout();
                Repaint();
            }

            // tracks background
            if (Event.current.type == EventType.Repaint)
            {
                var tex = OwlcatEditorStyles.Instance.TrackBackground.Style.normal.background;
                var tracksRect = new Rect(rect.xMin, rect.yMax - tex.height, rect.width, tex.height);
                using (GuiScopes.Color(trackColor))
                {
                    GUI.DrawTextureWithTexCoords(tracksRect, tex, new Rect(0, 0, rect.width / tex.width, 1));
                }
                if (track.Repeat)
                    OwlcatEditorStyles.Instance.TrackRepeatMarker.Draw(tracksRect);
            }
            // left track selection/drag handle
            var handleRect = new Rect(rect.xMin, rect.yMin, Layout.TrackSelectorWidth, rect.height);
            OwlcatEditorStyles.Instance.TrackSelector.Style.Draw(handleRect, trackColor);

            if (Event.current.type == EventType.MouseDrag && handleRect.Contains(Event.current.mousePosition))
            {
                DragManager.Instance.BeginDrag(w => DrawDraggedTrack(track), track, rect.size);
            }
            ObjectSelectionButton(track, handleRect, rect);

            /*
            bool isFailedCommandDoubleClicked = false;
            */
            float l = Layout.TrackSelectorWidth;
            if (track.IsCollapsed)
            {
                using (GuiScopes.Color(trackColor))
                {
                    string s;
                    if (activeTrack != null && activeTrack.IsPlaying)
                    {
                        s = string.Format("{0}({1}/{2})", track.Comment, activeTrack.CommandIndex + 1, track.Commands.Count);
                    }
                    else
                    {
                        s = string.Format("{0}({1})", track.Comment, track.Commands.Count);
                    }
                    var style = OwlcatEditorStyles.Instance.TrackCollapsed;
                    var r = new Rect(rect.xMin + l, rect.yMin, rect.width - 2 * Layout.TrackSelectorWidth, rect.height);
                    GUI.Box(r, s, style);
                    ObjectSelectionButton(track, r, rect);
                    l += r.width;
                }
            }
            else
            {
                // commands
                DrawCommandDropPoint(new Rect(rect.xMin + l, rect.yMin + 1, 0, 23), 0, track);
                for (int ci = 0; ci < track.Commands.Count; ci++)
                {
                    var command = track.Commands[ci];

                    var s = Layout.MeasureCommand(command);
                    var c = command ? command.GetCaption() : "???";

                    var cdd = DragManager.Instance.DraggedObject as CommandDragData;
                    if (cdd == null || cdd.Command != command)
                    {
                        var cmdRect = new Rect(rect.xMin + l, rect.yMin + 1, s, 23);

                        var style = command && (command.HasConditions || command.EvaluationErrorHandlingPolicy != EvaluationErrorHandlingPolicy.Default)
                            ? OwlcatEditorStyles.Instance.CommandConditionMarker
                            : OwlcatEditorStyles.Instance.CommandBox;
                        var isActiveCommand = activeTrack != null && activeTrack.CommandIndex == ci;
                        var col = isActiveCommand ? m_Colors.DebugActive : GUI.color;
                        if (IsDebugging && m_DebuggedPlayer.PlayerData.IsCommandFailed(command))
                            col = m_Colors.DebugStopCommand;
                        else if(IsDebugging && m_DebuggedPlayer.PlayerData.IsCommandCheckFailed(command))
                            col = m_Colors.DebugCommandCheckFailed;
                        using (GuiScopes.Color(col))
                        {
                            GUI.Box(cmdRect, c, style);
                        }
                        DrawDelayed(
                            () =>
                            {
                                using (GuiScopes.Color(col))
                                {
                                    if (isActiveCommand)
                                        GUI.Label(
                                            new Rect(cmdRect.xMin, cmdRect.yMin - 12, cmdRect.width, 12),
                                            activeTrack.PlayTime.ToString("0.0"),
                                            OwlcatEditorStyles.Instance.CommandTimeLabel);
                                    if (activeTrack != null && activeTrack.IsPlaying)
                                    {
                                        string w;
                                        using (m_DebuggedPlayer.PlayerData.Parameters.RequestContextData())
                                            w = command.GetWarning();
                                        if (!string.IsNullOrEmpty(w))
                                        {
                                            using (GuiScopes.Color(Color.red))
                                                GUI.Label(
                                                    new Rect(cmdRect.xMin, cmdRect.yMin - 12, cmdRect.width, 12),
                                                    w,
                                                    OwlcatEditorStyles.Instance.CommandTimeLabel);
                                        }
                                    }
                                }
                            });

                        DrawExtraSignals(command, cmdRect);

                        //handle doubleclick on failed command
                        if (IsDebugging && m_DebuggedPlayer.PlayerData.IsCommandFailed(command)
                                        && Event.current.type == EventType.MouseDown && Event.current.clickCount > 1 
                                        && cmdRect.Contains(Event.current.mousePosition))
                        {
                            MiniConsoleActive = true;
                            m_MiniConsole.ConsoleShowMode = CutsceneEditorMiniConsole.ShowMode.ErrorCallstack;
                            m_MiniConsole.ShowErrorLogCommand = command;
                            /*
                            isFailedCommandDoubleClicked = true;
                            */
                        }

                        if (Event.current.type == EventType.MouseDrag && cmdRect.Contains(Event.current.mousePosition))
                        {
                            var cr = new Rect(Vector2.zero, cmdRect.size);
                            DragManager.Instance.BeginDrag(
                                w => GUI.Box(cr, c, OwlcatEditorStyles.Instance.CommandBox),
                                new CommandDragData { Command = command, FromTrack = track },
                                cmdRect.size);
                        }

                        if (command?.IsContinuous??false)
                            OwlcatEditorStyles.Instance.TrackRepeatMarker.Draw(cmdRect);

                        ObjectSelectionButton(command, cmdRect, cmdRect);

                        DrawCommandDropPoint(cmdRect, ci + 1, track);
                    }

                    l += s;

                    if (l > rect.width && Event.current.type == EventType.Repaint)
                    {
                        // if command caption does not fit, command was probably changed from outside. Request re-layouting
                        m_Layout = null;
                        Focus();
                        GUIUtility.ExitGUI();
                        Repaint();
                    }
                }
            }
            
            /* // doubleclick on track. since track's rect overlaps with command's rect, doubleclick on failed command has higher priority
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount > 1 && rect.Contains(Event.current.mousePosition) && !isFailedCommandDoubleClicked)
            {
                Event.current.Use();
                track.IsCollapsed = !track.IsCollapsed;
                EditorUtility.SetDirty(m_Layout.FindGateForTrack(track));
                DoLayout();
                Repaint();
            }*/


            // track selection/drag handle
            handleRect = new Rect(rect.xMin + l, rect.yMin, Layout.TrackSelectorWidth, rect.height);
            OwlcatEditorStyles.Instance.TrackSelector.Style.Draw(handleRect, trackColor);

            if (Event.current.type == EventType.MouseDrag && handleRect.Contains(Event.current.mousePosition))
            {
                DragManager.Instance.BeginDrag(w => DrawDraggedTrack(track), track, rect.size);
            }

            ObjectSelectionButton(track, handleRect, rect);


        }

        private void DrawExtraSignals(CommandBase command, Rect cmdRect)
        {
            var xtra = command?.GetExtraSignals();
            if (xtra == null || xtra.Length == 0)
                return;

            for (int ii = 0; ii < xtra.Length; ii++)
            {
                var data = xtra[ii];
                var point = cmdRect.min + new Vector2(cmdRect.width / (1 + xtra.Length), 0);
                var rect = new Rect(point.x - 5, point.y - 5, 10, 10);

                GUI.Box(rect, "", OwlcatEditorStyles.Instance.ExtraSignal);

                if (Event.current.type == EventType.MouseDrag && rect.Contains(Event.current.mousePosition))
                {
                    DragManager.Instance.BeginDrag(
                        w => { GUI.Box(new Rect(0, 0, 16, 16), "", OwlcatEditorStyles.Instance.ExtraSignal); },
                        new CommandSignalDragData { Command = command, Data = data },
                        new Vector2(16, 16));
                }

                if (DragManager.Instance.DragInProgress)
                {
                    var csdd = DragManager.Instance.DraggedObject as CommandSignalDragData;
                    if (csdd?.Data == data)
                    {
                        m_GateReactivationDrawer.Add(point, new Rect(Event.current.mousePosition, Vector2.zero));
                        continue;
                    }
                }
                if (data.Gate)
                {
                    m_GateReactivationDrawer.Add(point, m_Layout.GetRect(data.Gate));
                }
            }
        }

        private void DrawDraggedTrack(Track track)
        {
            var rect = m_Layout.GetRect(track);
            DrawTrack(new Rect(Vector2.zero, rect.size), track);
        }

        private void DrawGate(Gate gate, Rect rect)
        {
            var drawActive = IsDebugging && m_DebuggedPlayer.PlayerData.IsGateActive(gate);
            var isGateSplit = m_Layout.IsGateSplit(gate);
            var isGateUnreachable = gate != Cutscene && m_Layout.IsGateUnreachable(gate);

            using (GuiScopes.Color(drawActive ? m_Colors.DebugActive : isGateUnreachable ? m_Colors.GateUnreachable : string.IsNullOrEmpty(gate.Comment) ? gate.Color : GUI.color))
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var tex = EditorGUIUtility.whiteTexture;
                    if (!isGateUnreachable)
                    {
                        switch (gate.ActivationMode)
                        {
                            case Gate.ActivationModeType.AllTracks:
                                tex = isGateSplit ? OwlcatEditorStyles.Instance.SplitGateTexture : OwlcatEditorStyles.Instance.GateTexture;
                                break;
                            case Gate.ActivationModeType.FirstTrack:
                                tex = OwlcatEditorStyles.Instance.GateTextureFirst;
                                break;
                            case Gate.ActivationModeType.RandomTrack:
                                tex = OwlcatEditorStyles.Instance.GateTextureRandom;
                                break;
                        }
                    }
                    var gateRect = new Rect(rect.xMin, rect.yMin, rect.width, rect.height);
                    GUI.DrawTextureWithTexCoords(gateRect, tex, new Rect(0, 0, 1, rect.height / tex.height));
                }
            }

            if (isGateSplit && gate.ActivationMode == Gate.ActivationModeType.AllTracks)
            {
                rect.xMin -= 3;
                rect.xMax += 3;
            }
            ObjectSelectionButton(gate, rect, rect);

            // dropping an activate-gate command on a gate links it
            if (DropPoint(rect, typeof(CommandSignalDragData)))
            {
                var csdd = (CommandSignalDragData)DragManager.Instance.DraggedObject;
                var old = csdd.Data.Gate;
                UndoManager.Instance.RegisterUndo(
                    "Reactivate gate linked",
                    () =>
                    {
                        csdd.Data.Gate = old;
                        csdd.Command?.SetDirty();
                        DoLayout();
                        Repaint();
                    });
                csdd.Data.Gate = gate;
                csdd.Command.SetDirty();
                Repaint();
            }
            DrawTrackSignalDropPoint(rect, gate);
            DrawTrackToGateDropPoint(rect, gate);

            // comment
            if (!string.IsNullOrEmpty(gate.Comment))
            {
                using (GuiScopes.Color(gate.Color))
                {
                    var content = new GUIContent(gate.Comment);
                    var size = OwlcatEditorStyles.Instance.Comment.CalcSize(content);
                    var r = new Rect(rect.xMax, rect.y, size.x, Layout.TrackHeight);
                    GUI.Box(r, content, OwlcatEditorStyles.Instance.Comment);
                    ObjectSelectionButton(gate, r, rect);
                }
            }
        }


        #region toolbar

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    BlueprintPicker.ShowAssetPicker(typeof(Cutscene), null, a => { Open(a as Cutscene); }, Cutscene);
                }

                if (Cutscene && m_Layout != null)
                {
                    CopyPastProcess();
                    GUILayout.Space(10);
                    if (m_SelectedObject is Track)
                    {
                        DrawTrackToolbar((Track)m_SelectedObject);
                    }
                    if (m_SelectedObject is Gate)
                    {
                        DrawGateToolbar((Gate)m_SelectedObject);
                    }
                    if (m_SelectedObject is CommandBase)
                    {
                        DrawCommandToolbar((CommandBase)m_SelectedObject);
                    }
                    GUILayout.Space(10);


                    if (GUILayout.Button("Fix broken links", EditorStyles.toolbarButton))
                    {
                        foreach (var gate in m_Layout.GatesByDepth)
                        {
                            foreach (var track in gate.StartedTracks)
                            {
                                track.Commands = track.Commands.Select(c => c.ReloadFromInstanceID()).NotNull().ToList();
                                track.EndGate = track.EndGate.EditorEndGate(); // this calls ReloadFromInstanceID internally
                            }
                        }
                        DoLayout();
                        Repaint();
                    }

                    GUILayout.FlexibleSpace();
                    if (Application.isPlaying)
                    {
                        DrawToolbarDebug();
                    }
                    GUILayout.FlexibleSpace();

                    if (m_CutsceneObj != default)
                    {
                        m_HierarchyCopy = GUILayout.Toggle(m_HierarchyCopy, "Hierarchy Copy");
                    }

                    if (GUILayout.Button("Layout", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        DoLayout();
                        Repaint();
                    }

                    var isDirty = m_Layout.GatesByDepth.Any(g => EditorUtilityEx.IsDirty(g));
                    using (GuiScopes.Color(isDirty ? new Color(0.86f, 0.35f, 0.35f) : GUI.color))
                    {
                        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                        {
                            AssetDatabase.SaveAssets();
                            DoLayout();
                            Repaint();
                        }
                    }
                }

                if (GUILayout.Button("New window", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    var window = OpenPlayerInEditor(null);
                    window.position = new Rect(
                        position.position + Vector2.one * EditorGUIUtility.singleLineHeight,
                        position.size);
                }
                
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Open MiniConsole", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
                {
                    MiniConsoleActive = true;
                }

            }
        }

        void CopyPastProcess()
        {
            var e = Event.current;
            if (e.commandName == "Copy")
            {
                if (e.type == EventType.ValidateCommand)
                {
                    e.Use();
                }
                else if (e.type == EventType.ExecuteCommand)
                {
                    e.Use();
                    m_CutsceneObj = m_SelectedObject;
                }
            }
            else if (e.commandName == "Paste")
            {
                if (e.type == EventType.ValidateCommand)
                {
                    e.Use();
                }
                else if (e.type == EventType.ExecuteCommand)
                {
                    e.Use();

                    if (m_CutsceneObj is CommandBase command && m_SelectedObject is Track track0)
                    {
                        var commCopy = CopyCommandToTrack(command, track0);
                        m_SelectedObject = commCopy;
                        Selection.activeObject = BlueprintEditorWrapper.Wrap(commCopy);

                        Cutscene.SetDirtyUpdateTracks();
                        AssetDatabase.SaveAssets();
                    }
                    else if (m_CutsceneObj is Track track1 && m_SelectedObject is Gate gate0)
                    {
                        CopyTrackToGate(track1, gate0);
                    }
                    else if (m_CutsceneObj is Gate gate1 && m_SelectedObject is Track track2)
                    {
                        CopyGateToTrack(gate1, track2);
                        DoLayout();
                    }

                    _stopCount = 0;
                    m_HierarchyGates.Clear();
                }
            }
        }
        
        private CommandBase CopyCommandToTrack(CommandBase command, Track track)
        {
            BlueprintsDatabase.TryGetAssetDirectory(Cutscene, out string directoryPath);
            var commCopy = CreateCopy(command, directoryPath);
            track.Commands.Add(commCopy);
            return commCopy;
        }

        private void CopyGateToTrack(Gate gate, Track track)
        {
            BlueprintsDatabase.TryGetAssetDirectory(Cutscene, out string directoryPath);
            var gateCopy = CopyGate(gate, directoryPath);
            track.EndGate = gateCopy;
        }

        private void CopyTrackToGate(Track track, Gate gate)
        {
            BlueprintsDatabase.TryGetAssetDirectory(gate, out string directoryPath);
            var trackCopy = CopyTrack(track, directoryPath);
            gate.StartedTracks.Add(trackCopy);
            gate.SetDirtyUpdateTracks();
        }

        Dictionary<Gate, Gate> m_HierarchyGates = new Dictionary<Gate, Gate>();

        private T CreateCopy<T>(T asset, string directoryPath = null) where T : SimpleBlueprint
        {
            return (T)BlueprintsDatabase.DuplicateAsset(asset, string.IsNullOrEmpty(directoryPath) || asset is not BlueprintScriptableObject so ?
                null : Path.Combine(directoryPath, so.AssetName).Replace('/', '\\'));
        }

        private Track CopyTrack(Track source, string directoryPath)
        {
            var endGate = m_HierarchyCopy ? GetHierarchyCopyGate(source.EndGate.EditorEndGate(), directoryPath) : default;
            var result = new Track()
            {
                Comment = source.Comment,
                IsCollapsed = source.IsCollapsed,
                Repeat = source.Repeat,
                EndGate = endGate
            };

            foreach (var comm in source.Commands)
            {
                var commCopy = CreateCopy(comm, directoryPath);
                result.Commands.Add(commCopy);
            }

            return result;
        }

        Gate GetHierarchyCopyGate(Gate endGate, string directoryPath)
        {
            if (endGate == null)
                return null;

            if (!m_HierarchyGates.TryGetValue(endGate, out var gate))
            {
                gate = CopyGate(endGate, directoryPath);
            }

            return gate;
        }

        int _stopCount = 0;

        private Gate CopyGate(Gate gate, string directoryPath)
        {
            if (_stopCount > 10)
                return null;

            _stopCount++;
            var gateCopy = CreateCopy(gate, directoryPath);
            m_HierarchyGates.Add(gate, gateCopy);

            for (int i = 0; i < gateCopy.StartedTracks.Count; i++)
            {
                var track = gateCopy.StartedTracks[i];
                var copyTrack = CopyTrack(track, directoryPath);
                gateCopy.StartedTracks[i] = copyTrack;
            }
            
            return gateCopy;
        }

        private void DrawToolbarDebug()
        {
            using (GuiScopes.Color(IsDebugging ? Color.cyan : GUI.color))
            {
                //GUILayout.Box("Debug mode", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));

                var isPlaying = IsDebugging && !m_DebuggedPlayer.PlayerData.Paused;
                if (isPlaying)
                {
                    if (GUILayout.Button(m_Colors.PauseButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        m_DebuggedPlayer.PlayerData.SetPaused(true, CutscenePauseReason.ManualPauseFromEditor);
                    }

                }
                else
                {
                    if (GUILayout.Button(m_Colors.PlayButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        if (!m_DebuggedPlayer)
                            SetDebuggedPlayer(CutscenePlayerView.Play(Cutscene));
                        else
                            m_DebuggedPlayer.PlayerData.SetPaused(false, CutscenePauseReason.ManualPauseFromEditor);
                    }
                    if (IsDebugging && GUILayout.Button(m_Colors.StopButton, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        m_DebuggedPlayer.PlayerData.PreventDestruction = false;
                        m_DebuggedPlayer.PlayerData.Stop();
                        Destroy(m_DebuggedPlayer.gameObject);
                        m_DebuggedPlayer = null;
                        m_NeedDestroyLastScene = false;
                        Repaint();
                    }
                }
            }
        }

        private void DrawTrackToolbar(Track selectedObject)
        {
            GUILayout.Label("Track  ");

            if (GUILayout.Button("New command", EditorStyles.toolbarButton))
            {
                TypePicker.Show(GUILayoutUtility.GetLastRect(), "Command", () => s_CommandTypes, t => AddCommandToTrack(selectedObject, t));
            }

            GUILayout.Space(10);
            if (selectedObject.EndGate.EditorEndGate() && GUILayout.Button("Unlink signal", EditorStyles.toolbarButton))
            {
                var gate = m_Layout.FindGateForTrack(selectedObject);
                var old = selectedObject.EndGate.EditorEndGate();
                selectedObject.EndGate = null;
                gate.SetDirty();

                UndoManager.Instance.RegisterUndo("create command",
                    () =>
                    {
                        selectedObject.EndGate = old;
                        gate.SetDirty();
                        DoLayout();
                        Repaint();
                    });

                DoLayout();
                Repaint();
            }
            if (GUILayout.Button("Link NEW gate", EditorStyles.toolbarButton))
            {
                var gate = m_Layout.FindGateForTrack(selectedObject);
                var path = BlueprintsDatabase.GetAssetPath(Cutscene);
                var newGate = BlueprintsDatabase.CreateAsset<Gate>(Path.GetDirectoryName(path), "gate.jbp");
                var old = selectedObject.EndGate.EditorEndGate();
                selectedObject.EndGate = newGate;
                gate.SetDirty();

                UndoManager.Instance.RegisterUndo("create command",
                    () =>
                    {
                        AssetDatabase.DeleteAsset(path);
                        selectedObject.EndGate = old;
                        gate.SetDirty();
                        DoLayout();
                        Repaint();
                    });

                DoLayout();
                Repaint();
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Remove track", EditorStyles.toolbarButton))
            {
                RemoveTrack(selectedObject);
            }
            var rep = GUILayout.Toggle(selectedObject.Repeat, "Repeat", EditorStyles.toolbarButton);
            if (rep != selectedObject.Repeat)
            {
                var gate = m_Layout.FindGateForTrack(selectedObject);
                selectedObject.Repeat = rep;
                gate.SetDirty();

                UndoManager.Instance.RegisterUndo("change repeat",
                    () =>
                    {
                        selectedObject.Repeat = !rep;
                        gate.SetDirty();
                        DoLayout();
                        Repaint();
                    });
                DoLayout();
                Repaint();
            }
            var cps = GUILayout.Toggle(selectedObject.IsCollapsed, "Collapse", EditorStyles.toolbarButton);
            if (cps != selectedObject.IsCollapsed)
            {
                selectedObject.IsCollapsed = cps;
                m_Layout.FindGateForTrack(selectedObject)?.SetDirty();
                DoLayout();
                Repaint();
            }
            var comment = EditorGUILayout.DelayedTextField(selectedObject.Comment ?? "", EditorStyles.toolbarTextField, GUILayout.MaxWidth(200));
            if (comment != selectedObject.Comment)
            {
                selectedObject.Comment = comment;
                m_Layout.FindGateForTrack(selectedObject)?.SetDirty();
                if (selectedObject.IsCollapsed)
                {
                    DoLayout();
                    Repaint();
                }
            }
            //GUILayout.Label("Ends in: " + selectedObject.EndGate.NameSafe());
        }

        private void RemoveTrack(Track selectedObject)
        {
            var gate = m_Layout.FindGateForTrack(selectedObject);
            gate.StartedTracks.Remove(selectedObject);
            gate.SetDirty();

            UndoManager.Instance.RegisterUndo(
                "remove track",
                () =>
                {
                    gate.StartedTracks.Add(selectedObject);
                    gate.SetDirty();
                    DoLayout();
                    Repaint();
                });
            DoLayout();
            Repaint();
        }

        private void DrawGateToolbar(Gate selectedObject)
        {
            GUILayout.Label("Gate  ");

            var rep = (Operation)EditorGUILayout.EnumPopup(selectedObject.Op, EditorStyles.toolbarPopup, GUILayout.Width(96));
            if (rep != selectedObject.Op)
            {
                var old = selectedObject.Op;
                selectedObject.Op = rep;
                selectedObject.SetDirty();

                UndoManager.Instance.RegisterUndo("change gate op",
                    () =>
                    {
                        selectedObject.Op = old;
                        selectedObject.SetDirty();
                        DoLayout();
                        Repaint();
                    });
                Repaint();
            }
            var mod = (Gate.ActivationModeType)EditorGUILayout.EnumPopup(selectedObject.ActivationMode, EditorStyles.toolbarPopup, GUILayout.Width(96));
            if (mod != selectedObject.ActivationMode)
            {
                var old = selectedObject.ActivationMode;
                selectedObject.ActivationMode = mod;
                selectedObject.SetDirty();

                UndoManager.Instance.RegisterUndo("change gate mode",
                    () =>
                    {
                        selectedObject.ActivationMode = old;
                        selectedObject.SetDirty();
                        DoLayout();
                        Repaint();
                    });
                DoLayout();
                Repaint();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Add track", EditorStyles.toolbarButton))
            {
                var track = new Track();
                selectedObject.StartedTracks.Add(track);
                selectedObject.SetDirty();
                DoLayout();
                Repaint();
            }

            if (selectedObject != Cutscene && m_Layout.IsGateOrphaned(selectedObject) && GUILayout.Button("Remove", EditorStyles.toolbarButton))
            {
                BlueprintsDatabase.DeleteAsset(selectedObject); 
                DoLayout();
                Repaint();
            }
        }

        private void DrawCommandToolbar(CommandBase selectedObject)
        {
            GUILayout.Label("Command  ");

            var commandTest = CommandTest.CommandTest.GetCommandTest(selectedObject);
            if (commandTest != null)
            {
                if (GUILayout.Button("Test", EditorStyles.toolbarButton))
                {
                    commandTest();
                }
                if (GUILayout.Button("▾", EditorStyles.toolbarButton))
                {
                    EditorCutsceneParamsInspector.Open();
                }
            }

            if (GUILayout.Button("Remove", EditorStyles.toolbarButton))
            {
                RemoveCommand(selectedObject);
            }

            var track = m_Layout.FindTrackForCommand(selectedObject);
            if (track == null)
            {
                return;
            }

            if (GUILayout.Button("Add next command", EditorStyles.toolbarButton))
            {
                TypePicker.Show(GUILayoutUtility.GetLastRect(), "Command", () => s_CommandTypes, t => AddCommandToTrack(track, t, selectedObject));
            }

            if (GUILayout.Button("Split track", EditorStyles.toolbarButton))
            {
                AddSplitTrack(track, selectedObject);
            }
        }

        private void RemoveCommand(CommandBase selectedObject)
        {
            var track = m_Layout.FindTrackForCommand(selectedObject);
            if (track == null)
            {
                // orphan command, delete completely, no undo!
                BlueprintsDatabase.DeleteAsset(selectedObject);
            }
            else
            {
                // remove from track and orphan
                var gate = m_Layout.FindGateForTrack(track);
                var old = track.Commands.ToList();
                track.Commands.Remove(selectedObject);
                gate.SetDirtyUpdateTracks();

                UndoManager.Instance.RegisterUndo("remove command",
                    () =>
                    {
                        track.Commands = old;
                        gate.SetDirtyUpdateTracks();
                    });
            }

            DoLayout();
            Repaint();
        }

        #endregion

        private void AddCommandToTrack(Track track, Type type, CommandBase after = null)
        {
            var gate = m_Layout.FindGateForTrack(track);
            var newCom = Activator.CreateInstance(type) as CommandBase;
            var insertionIndex = track.Commands.IndexOf(after) + 1;

            if (!CanInsertCommand(track, newCom, insertionIndex))
            {
                ShowNotification(new GUIContent("Cannot add command: continuous command must be last."));
                return;
            }

            // Generate kind of globally unique filename to avoid false-positive asset id guard triggering
            // when deleting old and adding new command with the same name
            string filename = type.Name;
            if (newCom != null)
            {
                string guid = Guid.NewGuid().ToString("N");
                newCom.AssetGuid = guid;
                filename = $"{type.Name}_{guid[..4]}";
            }

            var path = BlueprintsDatabase.GetAssetPath(Cutscene);
            BlueprintsDatabase.CreateAsset(newCom, Path.GetDirectoryName(path), filename);
            ElementWorkspaceContextualPopulationController.PrefillWithTargets(newCom, newCom);
            track.Commands.Insert(insertionIndex, newCom);
            Selection.activeObject = BlueprintEditorWrapper.Wrap(newCom);
            gate.SetDirtyUpdateTracks();

            UndoManager.Instance.RegisterUndo(
                "create command",
                () =>
                {
                    BlueprintsDatabase.DeleteAsset(newCom);
                    track.Commands.Remove(newCom);
                    gate.SetDirtyUpdateTracks();
                    DoLayout();
                    Repaint();
                });

            DoLayout();

            // scroll to where the command is
            var r = m_Layout.GetRect(track);
            foreach (var cmd in track.Commands)
            {
                r.xMin += Layout.MeasureCommand(cmd);
                if (cmd == newCom)
                {
                    break;
                }
            }

            m_ScrollPos.x = Mathf.Max(r.xMin - position.width / 2, 0);
            Repaint();
        }

        private static bool CanInsertCommand(Track track, CommandBase newCom, int insertionIndex)
        {
            if (insertionIndex > 0 && track.Commands.Last().IsContinuous)
                return false;
            if (newCom.IsContinuous && insertionIndex < track.Commands.Count)
                return false;
            return true;
        }

        private void AddSplitTrack(Track track, CommandBase afterCommand)
        {
            Gate splitGate;
            if (afterCommand == track.Commands.Last() && m_Layout.IsGateSplit(track.EndGate.EditorEndGate()))
            {
                splitGate = track.EndGate.EditorEndGate();
            }
            else
            {
                var path = BlueprintsDatabase.GetAssetPath(Cutscene);
                splitGate = BlueprintsDatabase.CreateAsset<Gate>(Path.GetDirectoryName(path), "gate.jbp");
                var restOfTrack = new Track();
                splitGate.StartedTracks.Add(restOfTrack);
                restOfTrack.EndGate = track.EndGate.EditorEndGate();

                var idx = track.Commands.IndexOf(afterCommand);
                if (idx < track.Commands.Count - 1)
                {
                    for (int ii = idx + 1; ii < track.Commands.Count; ii++)
                    {
                        restOfTrack.Commands.Add(track.Commands[ii]);
                    }
                    track.Commands.RemoveRange(idx + 1, track.Commands.Count - idx - 1);
                }

                track.EndGate = splitGate;
            }

            var source = m_Layout.FindGateForTrack(track);
            source.SetDirtyUpdateTracks();

            var splitTrack = new Track();
            splitGate.StartedTracks.Add(splitTrack);
            splitGate.SetDirtyUpdateTracks();

            DoLayout(); // this is so that AddCommandTotrack knows how to find the new gate
        }

        private void ObjectSelectionButton(object obj, Rect rect, Rect selectionRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseUp &&
                e.button == 0 &&
                rect.Contains(e.mousePosition))
            {
                Selection.activeObject = BlueprintEditorWrapper.Wrap(obj as SimpleBlueprint);
                m_SelectedObject = obj;
                m_SelectedObjectRect = selectionRect;

                e.Use();
            }

            if (m_SelectedObject == obj)
            {
                if (m_SelectedObjectRect == null)
                    m_SelectedObjectRect = selectionRect; // this restores object rect if we changed selection outside of editor window
            }
        }

        class CommandDragData
        {
            public CommandBase Command;
            public Track FromTrack;
        }
        class CommandSignalDragData
        {
            public CommandBase Command;
            public CommandBase.CommandSignalData Data;
            //	        public Track FromTrack;
        }
    }
}