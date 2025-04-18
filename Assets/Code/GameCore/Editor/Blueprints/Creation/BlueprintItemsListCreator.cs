using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintItemsListCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Items List";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Encounters/{Area}/ItemsList/{name}.asset";
		
        public override object CreateAsset()
        {
            return new BlueprintItemsList();
        }

		public override bool CanCreateAssetsOfType(Type type)
	    {
	        return type == typeof(BlueprintItemsList);
	    }

    }
}