using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintScriptZoneCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Script zone";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ScriptZones/{name}.asset";
		
        public override object CreateAsset()
        {
            return new BlueprintScriptZone();
        }
    }
}