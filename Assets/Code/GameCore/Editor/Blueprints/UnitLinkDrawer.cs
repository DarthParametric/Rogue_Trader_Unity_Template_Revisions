using Kingmaker.ResourceLinks;
using Kingmaker.View;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(UnitViewLink))]
	public class UnitLinkDrawer : WeakLinkDrawer<UnitEntityView>
	{
	}
}