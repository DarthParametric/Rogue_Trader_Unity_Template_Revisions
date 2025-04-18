using Kingmaker.Blueprints.Quests;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintQuestCreator : AssetCreatorBase
	{
		public override string CreatorName => "Quest";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Quests/{folder}/{name}/{name}.asset";

		public override object CreateAsset()
        {
            return new BlueprintQuest();
        }
    }
}