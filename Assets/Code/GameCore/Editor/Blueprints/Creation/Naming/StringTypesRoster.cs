using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[FilePath("Assets/Editor/Naming/" + nameof(StringTypesRoster) + ".asset", FilePathAttribute.Location.ProjectFolder)]
	public class StringTypesRoster : ScriptableSingleton<StringTypesRoster>
	{
		[SerializeField]
		public NamingBase Action;
		[SerializeField]
		public NamingBase Bark;
		[SerializeField]
		public NamingBase Buff;
		[SerializeField]
		public NamingBase EntryPoint;
		[SerializeField]
		public NamingBase Item;
		[SerializeField]
		public NamingBase LocationName;
		[SerializeField]
		public NamingBase Name;
		[SerializeField]
		public NamingBase Other;
	}
}