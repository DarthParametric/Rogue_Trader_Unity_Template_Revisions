using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Interfaces;
using UnityEditor;

namespace Code.GameCore.EntitySystem.Entities
{
	public static class EntityViewBaseExtension
	{
		public static EntityReference ToEditorEntityReference(IEntityViewBase entityViewBase)
		{
			return new EntityReference()
			{
				UniqueId = entityViewBase.UniqueViewId,
				EntityNameInEditor = entityViewBase.GameObjectName,
				SceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entityViewBase.GO.scene.path)
			};
		}
	}
}