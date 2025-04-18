using System.Collections.Generic;

namespace Kingmaker.Editor.EtudesViewer
{
    public class EtudeIdReferences
    {
        public enum EtudeState
        {
            NotStated = 0,
            Started = 1,
            Active = 2,
            CompleteBeforeActive = 3,
            Completed = 4,
            ComplitionBlocked = 5
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
        public bool CompleteParent;
        public string Comment;
        public bool Foldout;
        public List<string> ConflictingGroups = new List<string>();
        public int Priority;
        public string Id;
    }
}