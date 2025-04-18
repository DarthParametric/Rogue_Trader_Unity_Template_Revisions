using Kingmaker.EntitySystem.Persistence.Versioning;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class UnitUpgraderCreator : AssetCreatorBase
	{
		public override string CreatorName
			=> "Unit Upgrader";

		public override string LocationTemplate => "Assets/Mechanics/Blueprints/Upgraders/Units/{name}.asset";

		public override object CreateAsset()
		{
			return new BlueprintUnitUpgrader();
		}
	}
}