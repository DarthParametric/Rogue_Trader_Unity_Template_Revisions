using Kingmaker.EntitySystem.Persistence.Versioning;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class PlayerUpgraderCreator : AssetCreatorBase
	{
		public override string CreatorName
			=> "Player Upgrader";

		public override string LocationTemplate => "Assets/Mechanics/Blueprints/Upgraders/{name}.asset";

		public override object CreateAsset()
		{
			return new BlueprintPlayerUpgrader();
		}
	}
}