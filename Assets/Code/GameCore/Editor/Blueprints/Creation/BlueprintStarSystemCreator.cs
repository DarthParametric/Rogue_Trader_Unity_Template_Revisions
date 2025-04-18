using Kingmaker.Globalmap.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class BlueprintStarSystemCreator : BlueprintAreaCreator
    {
        protected override string MechanicsSceneTemplate => "Assets/Scenes/!Templates/StarSystem/Template_Mechanics.unity";
        protected override string MechanicsScenePath => "Assets/Scenes/{folder}/{name}/{name}_Mechanics.unity";
        protected override string DefaultEntranceSuffix
            => "_Enter_Warp";

        public override string CreatorName => "StarSystem";
        public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Areas/{folder}/{name}/{name}.asset";
        
        public override object CreateAsset()
        {
            return new BlueprintStarSystemMap();
        }

        public override void PostProcess(object asset)
        {
            base.PostProcess(asset);
        }
    }
}