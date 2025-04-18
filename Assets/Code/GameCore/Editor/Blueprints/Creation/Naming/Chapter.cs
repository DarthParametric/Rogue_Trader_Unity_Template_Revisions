using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[CreateAssetMenu(
		menuName = Str.MenuPrefix + nameof(Chapter),
		order = (int)NamingMenuOrder.Chapter,
		fileName = Str.RootFolder + nameof(Chapter) + "/NewName")]
	public class Chapter : NamingBase
	{
	}
}