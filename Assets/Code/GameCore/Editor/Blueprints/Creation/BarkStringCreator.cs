using Kingmaker.Blueprints;
using Kingmaker.Localization;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BarkStringCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "String (Bark)";

		public override string LocationTemplate =>
			"Assets/Mechanics/Blueprints/World/Dialogs/{folder}/{Area?}/Barks/Bark_{name}.asset";
		
       public override bool CreatesBlueprints => false;

        public override object CreateAsset()
        {
            return CreateInstance<SharedStringAsset>();
        }
    }
}