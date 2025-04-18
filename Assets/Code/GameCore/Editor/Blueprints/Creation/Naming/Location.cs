using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[CreateAssetMenu(
		menuName = Str.MenuPrefix + nameof(Location),
		order = (int)NamingMenuOrder.Location,
		fileName = Str.RootFolder + nameof(Location) + "/NewName")]
	public class Location : NamingBase
	{
	}
}