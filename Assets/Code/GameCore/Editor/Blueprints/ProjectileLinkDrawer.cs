using Kingmaker.ResourceLinks;
using Kingmaker.View;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(ProjectileLink))]
	public class ProjectileLinkDrawer : WeakLinkDrawer<ProjectileView>
	{		
	}
}