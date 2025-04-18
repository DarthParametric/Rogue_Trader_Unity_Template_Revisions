using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	/// <summary>
	/// Base class for a set of distinct names, which are derived from asset filename.
	/// All name-assets of the same type are expected to be placed in the same folder
	/// to guarantee that their names are unique.
	/// </summary>
	public abstract class NamingBase : ScriptableObject
	{
	}
}