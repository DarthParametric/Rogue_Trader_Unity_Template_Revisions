using System.Collections.Generic;

namespace Kingmaker.Editor.EtudesViewer
{
    public class EtudeIdReferences
    {
        public enum EtudeState
        {
            NotStarted = 0,
            Started = 1,
            Playing = 2,
            CompleteBeforeActive = 3,
            Completed = 4,
            CompletionInProgress = 5
        }

        public string Name;
        public string ParentId;
        public List<string> LinkedId = new List<string>();
        public List<string> ChainedId = new List<string>();
        public string LinkedTo;
        public string ChainedTo;
        public List<string> ChildrenId = new List<string>();
        public EtudeState State;
        public string LinkedArea;
        public string LinkedAreaName;
        public bool CompleteParent;
        public bool HasSomeMechanics;
        public string Comment;
        public bool Foldout;
        public List<string> ConflictingGroups = new List<string>();
        public int Priority;
        public string Id;
    }
}