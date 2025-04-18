using System;
using Kingmaker.QA.Clockwork;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class BlueprintClockworkScenarioCreator : AssetCreatorBase
    {
        public override string CreatorName => "Clockwork Scenario";
        public override string LocationTemplate => "Blueprints/QA/Clockwork/Scenario/{folder}/{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintClockworkScenario();
        }

        public override bool CanCreateAssetsOfType(Type type)
        {
            return type == typeof(BlueprintClockworkScenario);
        }
    }
}