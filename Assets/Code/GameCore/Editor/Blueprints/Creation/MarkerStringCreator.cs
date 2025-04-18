using Kingmaker.Blueprints;
using Kingmaker.Localization;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class MarkerStringCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "String (Map marker)";

		public override string LocationTemplate =>
			"Assets/Mechanics/Blueprints/World/Dialogs/{folder}/{Area?}/Objects/Object_{name}.asset";
      
        public override bool CreatesBlueprints => false;

        public override object CreateAsset()
        {
            return CreateInstance<SharedStringAsset>();
        }
    }
}