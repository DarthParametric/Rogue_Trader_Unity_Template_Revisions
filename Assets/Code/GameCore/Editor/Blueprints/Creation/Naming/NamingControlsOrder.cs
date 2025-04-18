using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[FilePath("Assets/Editor/Naming/" + nameof(NamingControlsOrder) + ".asset", FilePathAttribute.Location.ProjectFolder)]
	public class NamingControlsOrder : ScriptableSingleton<NamingControlsOrder>
	{
		[SerializeField]
		public string[] NamingTypeNames;
	}
}