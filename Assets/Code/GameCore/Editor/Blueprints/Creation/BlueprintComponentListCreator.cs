using Kingmaker.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintComponentListCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Component list";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ComponentsLists/{name}.asset";
		
        public override object CreateAsset()
        {
            return new BlueprintComponentList();
        }
    }
}