using System;
using Kingmaker.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintUnlockableFlagCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Unlockable flag";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/UnlockableFlags/{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintUnlockableFlag();
        }

		public override bool CanCreateAssetsOfType(Type type)
	    {
	        return type == typeof(BlueprintUnlockableFlag);
	    }

    }
}