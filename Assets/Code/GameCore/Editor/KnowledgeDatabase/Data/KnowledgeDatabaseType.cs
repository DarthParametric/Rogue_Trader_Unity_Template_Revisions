using System.Collections.Generic;

namespace Code.Editor.KnowledgeDatabase.Data
{
	public class KnowledgeDatabaseType
	{
		private string m_Type;
		
		public string Name;
		public string Guid;
		public string Type
		{
			get => m_Type;
			set { m_Type = value; }
		}
		public string AllowedOn;
		public bool HasRuntime;
		public Dictionary<string, KnowledgeDatabaseField> Fields = new();
        public string CodeDescription;
        public string Description;
		public string Keywords;
		public bool IsRemoved;
		public bool IsObsolete;

		public readonly Dictionary<string, string> Meta = new();
	}
}