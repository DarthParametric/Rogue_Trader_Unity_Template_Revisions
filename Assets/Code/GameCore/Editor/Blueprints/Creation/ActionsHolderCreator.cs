using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class ActionsHolderCreator : AssetCreatorBase
    {
        public BlueprintAreaReference Area;

        public override string CreatorName
            => "Actions holder";

        public override string LocationTemplate
            => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ActionsHolders/{name}.asset";
        
        public override object CreateAsset()
        {
            return new ActionsHolder();
        }
    }
}