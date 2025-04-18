using Kingmaker.ResourceLinks;
using Kingmaker.Visual.CharacterSystem;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints
{
	[CustomPropertyDrawer(typeof(EquipmentEntityLink))]
	public class EquipmentEntityLinkDrawer : WeakLinkDrawer<EquipmentEntity>
	{		
	}
}