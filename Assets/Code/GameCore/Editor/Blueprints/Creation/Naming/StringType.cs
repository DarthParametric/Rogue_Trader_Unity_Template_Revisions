using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[CreateAssetMenu(
		menuName = Str.MenuPrefix + nameof(StringType),
		order = (int)NamingMenuOrder.StringType,
		fileName = Str.RootFolder + nameof(StringType) + "/NewName")]
	public class StringType : NamingBase
	{
	}
}