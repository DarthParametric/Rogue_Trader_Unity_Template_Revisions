namespace Kingmaker.Editor.Blueprints.Creation
{
    public class BlueprintBarkBanterListCreator : AssetCreatorBase
    {
        public override string CreatorName
            => "BanterList";

        public override string LocationTemplate
            => "Assets/Mechanics/Blueprints/World/Dialogs/BantersList/{name}_BanterList.asset";

        public override object CreateAsset()
        {
            return new BarkBanters.BlueprintBarkBanterList();
        }
    }
}