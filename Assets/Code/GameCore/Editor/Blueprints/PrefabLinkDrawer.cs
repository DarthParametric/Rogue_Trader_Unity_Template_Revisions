using Kingmaker.ResourceLinks;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(PrefabLink))]
	public class PrefabLinkDrawer : WeakLinkDrawer<GameObject>
	{		
	}
}