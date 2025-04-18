using Code.GameCore.Editor.CodeExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using Owlcat.Editor.Utility;
using UnityEngine;
using Kingmaker.Utility.DotNetExtensions;

namespace Kingmaker.Editor.Cutscenes
{
	public class Layout
	{
		public const int SlotHeight = 30;
		public const int TrackHeight = 25;
		public const int TrackPadding = SlotHeight - TrackHeight;
		public const int TrackArrowAllowance = 16;
		public const int TrackSelectorWidth = 12;
		public const int CommandPadding = 4;
		public const int GateWidth = 15;
		public const int LeftPadding = 5;
		public const int TopPadding = 20;
		public const int OrphanCommandsBreak = 20;
		public const int CollapsedTrackWidth = 24;

		public float Width { get; private set; }
		public float Height { get; private set; }
		public float DrawerTop { get; private set; }

		public void DoLayout(Gate cutscene)
		{
			m_TrackLayout.Clear();
			m_GateLayout.Clear();
			Width = Height = 0;

			// find ALL gates and commands in the folder
			GetAllAssets(cutscene);

			// find gate depths - i.e. how many gates have tracks leading to every gate in the cutscene. This can be used to sort them left-to-right
			m_GatesByDepths = new List<GateLayoutData>();
			TopoSortRecursively(cutscene);
			m_GatesByDepths.Reverse();

			// tack orphan gates at the end so they get laid out after the actual cutscene
			m_GatesByDepths = m_GatesByDepths
				.Concat(m_OrphanGates.Select(Get))
				.ToList();

			// store depth value - this ensures no two gates have the same depth
			for (int ii = 0; ii < m_GatesByDepths.Count; ii++)
			{
				m_GatesByDepths[ii].Depth = ii;
			}

			// list of tracks that finished layouting
			var laidOutTracks = new List<TrackLayoutData>();

			for (int gi = 0; gi < m_GatesByDepths.Count; gi++)
			{
				var layoutData = m_GatesByDepths[gi];
				layoutData.HeightSlot = layoutData.IncomingTracks.Count > 0
					? layoutData.IncomingTracks.Min(tld => tld.HeightSlot)
					: 0;


				// sort out started tracks height slots
				var hs = layoutData.HeightSlot;
				if (!string.IsNullOrEmpty(layoutData.Gate.Comment))
				{
					hs++; // empty "track" for drawing gate comment
					var size = OwlcatEditorStyles.Instance.Comment.CalcSize(new GUIContent(layoutData.Gate.Comment));
					layoutData.CommentWidth = size.x;
				}
				foreach (var track in layoutData.Gate.StartedTracks)
				{
					var tl = Get(track);
					// move track down - if we've measured a track before that ends in a gate to the right of the current one, and is on the same line - move down to make way for it
					while (
						laidOutTracks.Any(
							tld => tld.HeightSlot == hs && tld.Track.EndGate.EditorEndGate() && Get(tld.Track.EndGate.EditorEndGate()).Depth > layoutData.Depth))
					{
						hs++;
					}
					tl.HeightSlot = hs;
					hs++;
				}

				// ensure the gate stretches low enough if any incoming tracks are low
				foreach (var tl in layoutData.IncomingTracks)
				{
					hs = Math.Max(hs, tl.HeightSlot + 1);
				}

				// absolutely empty gates have at least some height
				if (hs == layoutData.HeightSlot)
					hs++;

				// new we know gate vertical pos
				var top = layoutData.HeightSlot * SlotHeight + TopPadding;
				var bottom = hs * SlotHeight + TopPadding;


				// hs is the lowest height slot in this gate - meaning it stretches from layoutData.HeightSlot to hs exclusive
				// move gate to the right until all existing tracks in these slots are clear
				float left = LeftPadding;
				if (gi > 0)
				{
					left = m_GatesByDepths[gi - 1].Rect.xMax + m_GatesByDepths[gi - 1].CommentWidth;
				}
				foreach (var tl in laidOutTracks)
				{
					if (tl.HeightSlot >= layoutData.HeightSlot && tl.HeightSlot < hs)
					{
						var trackEnd = tl.Rect.xMax + TrackArrowAllowance;
						left = Math.Max(left, trackEnd);
					}
				}

				// and we can still intersect some other gate if no tracks are on the same height slot
				for (int gj = 0; gj < gi; gj++)
				{
					var gate = m_GatesByDepths[gj];
					if (gate.Rect.yMin < bottom && gate.Rect.yMax > top)
						left = Mathf.Max(left, gate.Rect.xMax + TrackArrowAllowance);
				}

				// lay out gate
			    var w = (layoutData.IncomingTracks.Count == 1 &&
			             layoutData.Gate.ActivationMode == Gate.ActivationModeType.AllTracks)
			        ? 2
			        : GateWidth;
				layoutData.Rect = new Rect(left, top, w, bottom - top);
				Width = Math.Max(Width, layoutData.Rect.xMax);
				Height = Math.Max(Height, layoutData.Rect.yMax);

				// lay out tracks
				foreach (var track in layoutData.Gate.StartedTracks)
				{

					var tl = Get(track);

					var l = track.IsCollapsed ? MeasureCollapsedTrack(track) : track.Commands.Select(c => MeasureCommand(c)).Sum();

					l += TrackSelectorWidth * 2;

					tl.Rect = new Rect(layoutData.Rect.xMax + 2, tl.HeightSlot * SlotHeight + TopPadding, l, TrackHeight);
					Width = Math.Max(Width, tl.Rect.xMax);
					Height = Math.Max(Height, tl.Rect.yMax);
					laidOutTracks.Add(tl);

					m_OrphanCommands.ExceptWith(track.Commands);
				}
			}

			// layout orphaned commands
			Height += OrphanCommandsBreak + SlotHeight;
			DrawerTop = Height - SlotHeight;

			float cmdLeft = LeftPadding;
			foreach (var command in m_OrphanCommands)
			{
				var rect = new Rect(cmdLeft, Height - SlotHeight, MeasureCommand(command), TrackHeight);
				cmdLeft += rect.width + 2;
				if (cmdLeft > Width - 150)
				{
					cmdLeft = LeftPadding;
					Height += SlotHeight;
				}
				m_OrphanCommandLayout[command] = rect;
			}

		}


		private float MeasureCollapsedTrack(Track track)
		{
			var s = string.Format("{0}({1}/{1})", track.Comment, track.Commands.Count);
			var style = OwlcatEditorStyles.Instance.TrackCollapsed;

			var size = style.Style.CalcSize(new GUIContent(s));
			return size.x;
		}


		private void GetAllAssets(Gate cutscene)
		{
			var folderPath = Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(cutscene));

			m_OrphanGates.Clear();
			m_OrphanCommands.Clear();
			m_OrphanCommandLayout.Clear();

			var guids = BlueprintsDatabase.SearchByFolder(folderPath);
            foreach (var p in guids)
            {
                var asset = BlueprintsDatabase.LoadById<SimpleBlueprint>(p.Item1);

                if (asset is CommandBase)
                    m_OrphanCommands.Add((CommandBase)asset);
                if (asset is Gate)
                    m_OrphanGates.Add((Gate)asset);
            }
		}

		public static float MeasureCommand(CommandBase command)
		{
			var cap = command ? command.GetCaption() : "???";
			cap = cap ?? "unknown";
			var style = command && command.HasConditions
				? OwlcatEditorStyles.Instance.CommandConditionMarker
				: OwlcatEditorStyles.Instance.CommandBox;

			var size = style.Style.CalcSize(new GUIContent(cap));
			return size.x + CommandPadding;
		}

		void TopoSortRecursively(Gate gate)
		{
			var gl = Get(gate);

			if (gl.InSort)
            {
                PFLog.Default.Error(gate, "Cycle in cutscene detected!");
				return;
			}

			if (!m_OrphanGates.Remove(gate)) // if found in CalcDepth, this gate is not orphaned
			{
				return; 
			}

			gate.Cleanup(); // ensure we remove all NULL commands

			gl.HeightSlot++;
			gl.InSort = true;

			foreach (var track in gate.StartedTracks)
			{
				if (track.EndGate.EditorEndGate())
				{
					TopoSortRecursively(track.EndGate.EditorEndGate());
					var egl = Get(track.EndGate.EditorEndGate());
					var tl = Get(track);

					if(!egl.IncomingTracks.Contains(tl)) // can be already there if we've visited this gate already
						egl.IncomingTracks.Add(tl);
				}
                // go through command that may signal gates
			    foreach (var command in track.Commands)
			    {
			        foreach (var signalData in (command?.GetExtraSignals()).EmptyIfNull())
			        {
			            if (signalData.Gate && m_OrphanGates.Contains(signalData.Gate)) // only call recursively if we haven't laid out this gate yet. Commands may introduce cycles!
			            {
			                TopoSortRecursively(signalData.Gate);
			                var egl = Get(signalData.Gate);
			                var tl = Get(track);
			                if (!egl.IncomingTracks.Contains(tl)) // can be already there if we've visited this gate already
			                    egl.IncomingTracks.Add(tl);
			            }
                    }
			    }
			}

			m_GatesByDepths.Add(gl);

			gl.InSort = false;
		}

		GateLayoutData Get(Gate gate)
		{
			GateLayoutData existing;
			if (!m_GateLayout.TryGetValue(gate, out existing))
			{
				existing = new GateLayoutData { Gate = gate };
				m_GateLayout[gate] = existing;
			}
			return existing;
		}

		TrackLayoutData Get(Track track)
		{
			TrackLayoutData existing;
			if (!m_TrackLayout.TryGetValue(track, out existing))
			{
				existing = new TrackLayoutData { Track = track };
				m_TrackLayout[track] = existing;
			}
			return existing;
		}

		readonly Dictionary<Gate, GateLayoutData> m_GateLayout = new Dictionary<Gate, GateLayoutData>();
		readonly Dictionary<Track, TrackLayoutData> m_TrackLayout = new Dictionary<Track, TrackLayoutData>();
		readonly Dictionary<CommandBase, Rect> m_OrphanCommandLayout = new Dictionary<CommandBase, Rect>();
		private List<GateLayoutData> m_GatesByDepths;

		private HashSet<CommandBase> m_OrphanCommands = new HashSet<CommandBase>();
		private List<Gate> m_OrphanGates = new List<Gate>();

		class GateLayoutData
		{
			public Gate Gate;
			public Rect Rect;
			public int Depth;
			public int HeightSlot;
			public bool InSort { get; set; }
			public float CommentWidth { get; set; }

			public readonly List<TrackLayoutData> IncomingTracks = new List<TrackLayoutData>();
		}
		class TrackLayoutData
		{
			public Track Track;
			public Rect Rect;
			public int HeightSlot;
		}

		public IEnumerable<Gate> GatesByDepth { get { return m_GatesByDepths.Select(l => l.Gate); } }
		public IEnumerable<CommandBase> OrphanCommands { get { return m_OrphanCommands; } }

		public Rect GetRect(Track t)
		{
			return Get(t).Rect;
		}
		public Rect GetRect(Gate g)
		{
			return Get(g).Rect;
		}

		public Rect GetRect(CommandBase c)
		{
			Rect r;
			m_OrphanCommandLayout.TryGetValue(c, out r);
			return r;
		}

		public Gate FindGateForTrack(Track track)
		{
			var gld = m_GatesByDepths.FirstOrDefault(g => g.Gate.StartedTracks.Contains(track));
			return gld == null ? null : gld.Gate;
		}

		public int GetGateDepth(Gate gate)
		{
			return Get(gate).Depth;
		}

		public Track FindTrackForCommand(CommandBase command)
		{
			return m_TrackLayout.Keys.FirstOrDefault(k => k.Commands.Contains(command));
		}

		public bool CanLink(Gate fromGate, Gate gate)
		{
			// return true if there's no cycle formed when linking fromgate's track to gate
			// todo this hangs up if we ALREADY have a cycle somewhere
			if (fromGate == gate)
				return false;
			foreach (var track in gate.StartedTracks)
			{
				if (track.EndGate.EditorEndGate() == fromGate)
					return false;
				if (track.EndGate.EditorEndGate() && !CanLink(fromGate, track.EndGate.EditorEndGate()))
					return false;
			}
			return true;
		}

		public bool IsGateOrphaned(Gate gate)
		{
		    return m_OrphanGates.Contains(gate);// Get(gate).IncomingTracks.Count == 0;
		}

		public bool IsGateSplit(Gate gate)
		{
			if (!gate)
				return false;
			return Get(gate).IncomingTracks.Count == 1;
		}

		public bool IsGateUnreachable(Gate gate)
		{
			return Get(gate).IncomingTracks.All(t => t.Track.IsContinuous);
		}
	}
}