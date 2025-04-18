using Kingmaker.Editor.Blueprints.Creation.Naming;
using Kingmaker.Globalmap.Blueprints;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class BlueprintStarSystemCreator : BlueprintAreaCreator
    {
        private const string DefaultMechanicsTemplateScenePath = "Assets/Scenes/!Templates/StarSystem/Template_Mechanics.unity";

        protected override string MechanicsTemplateScenePath
            => Templates.instance == null
                ? DefaultMechanicsTemplateScenePath
                : AssetDatabase.GetAssetPath(Templates.instance.BlueprintStarSystemMap.MechanicsTemplateScene);

        protected override string DefaultMechanicsSceneTemplate => "Assets/Scenes/{Chapter}/{Location}/{Location}{name}_Mechanics.unity";

        protected override string MechanicsSceneTemplate
            => Templates.instance == null
                ? DefaultMechanicsSceneTemplate
                : Templates.instance.BlueprintStarSystemMap.MechanicsSceneTemplate;

        protected override string DefaultEntranceSuffix
            => "Enter_Warp";

        protected override string Template
            => Templates.instance == null
                ? DefaultTemplate
                : Templates.instance.BlueprintStarSystemMap.BlueprintStarSystemMapTemplate;

        public override string CreatorName => "StarSystem";

        public override object CreateAsset()
        {
            return new BlueprintStarSystemMap();
        }
    }
}