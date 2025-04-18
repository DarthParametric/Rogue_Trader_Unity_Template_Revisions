using Kingmaker.Localization;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class UITextStringCreator : AssetCreatorBase
	{
		public override string CreatorName => "String (UI)";

		public override string LocationTemplate =>
			"Assets/Mechanics/Blueprints/Root/Strings/{folder}/{name}.asset";
       
        public override bool CreatesBlueprints => false;
        
        public override object CreateAsset()
        {
	        return CreateInstance<SharedStringAsset>();
        }
	}
}