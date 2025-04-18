using System.Collections.Generic;

namespace Code.Editor.KnowledgeDatabase.Data
{
	public class KnowledgeDatabaseField
	{
		public string FieldPath;
		public string FieldName;
		public string Type;
		public string AllowedEntity;
        public string CodeDescription;
        public string Description;
		public string Keywords;
		public string Link;
		public bool IsRemoved;
		public bool IsObsolete;

		public readonly Dictionary<string, string> Meta = new();
	}
}