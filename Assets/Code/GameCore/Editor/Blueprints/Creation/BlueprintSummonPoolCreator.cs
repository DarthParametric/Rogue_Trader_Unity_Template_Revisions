using System;
using Kingmaker.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintSummonPoolCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Summon pool";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/SummonPools/{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintSummonPool();
        }

		public override bool CanCreateAssetsOfType(Type type)
	    {
	        return type == typeof(BlueprintSummonPool);
	    }

    }
}