using Kingmaker.Blueprints;
using Kingmaker.ElementsSystem;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class ConditionsHolderCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Conditions holder";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ConditionsHolders/{name}.asset";
		
        public override object CreateAsset()
        {
            return new ConditionsHolder();
        }
    }
}