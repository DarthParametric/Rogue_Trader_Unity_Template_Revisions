using System.Collections.Generic;
using Kingmaker.Editor.EtudesViewer;
using UnityEngine;

namespace Kingmaker.Assets.Code.Editor.EtudesViewer
{
    public class EtudeDrawerData
    {
        public Rect EtudeRect;
        public Rect EtudeButtonRect;
        public bool ShowChildren;
        public Vector2 LeftEnterPoint;
        public Vector2 LinkedStartPoint;
        public Vector2 RightExitPoint;
        public Dictionary<string, EtudeIdReferences> ChainStarts = new Dictionary<string,EtudeIdReferences>();
        public bool NeedToPaint;
        public int Depth;

        public Rect StartedReferencesLabelRect;
        public List<Rect> StartedRects = new List<Rect>();
        public Rect CompletedReferencesLabelRect;
        public List<Rect> CompletedRects = new List<Rect>();
        public Rect CheckedReferencesLabelRect;
        public List<Rect> CheckedRects = new List<Rect>();
        public Rect SynchronizedReferencesLabelRect;
        public List<Rect> SynchronizedRects = new List<Rect>();
        public Rect OtherReferencesLabelRect;
        public List<Rect> OtherRects = new List<Rect>();
        public Rect ConflictingGroupsLabelRect;
        public List<Rect> ConflictingGroupsRects = new List<Rect>();
        
    }
}