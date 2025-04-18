using Code.GameCore.EntitySystem.Entities;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Evaluators;
using Kingmaker.View.MapObjects;

namespace Kingmaker.Editor.Elements.SmartElementPopulation.Factories.Evaluators.Unit
{
	[UsedImplicitly]
	public class MapObjectFromSceneFromMapObjectViewFactory : ElementFactory<MapObjectFromScene, MapObjectView>
	{
		protected override void Populate(SimpleBlueprint owner, MapObjectFromScene element, MapObjectView source)
		{
			element.MapObject = EntityViewBaseExtension.ToEditorEntityReference(source);
		}
	}
}