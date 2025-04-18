using Kingmaker.BarkBanters;
using Kingmaker.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class BlueprintBarkBanterCreator : AssetCreatorBase
	{
		public BlueprintUnitReference Companion1;
		public BlueprintUnitReference Companion2;

		public override string CreatorName => "Banters";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Dialogs/Banters/{folder}/{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintBarkBanter();
        }

		protected override string GetAdditionalTemplate(string propName)
		{
			if (propName == "companions" && Companion1.Get())
			{
				var companionNames = Companion1.NameSafe().Replace("_Companion", "");
				if (Companion2.Get() != null)
				{
					companionNames += Companion2.NameSafe().Replace("_Companion", "");
				}
				return companionNames;
			}

			return base.GetAdditionalTemplate(propName);
		}

		public override void PostProcess(object asset)
		{
			var barkBanter = (BlueprintBarkBanter) asset;

			barkBanter.Unit = Companion1;
			if (Companion2 != null)
			{
				barkBanter.Conditions.Unique = true;
				barkBanter.Conditions.ResponseRequired = true;
				barkBanter.Responses = new BlueprintBarkBanter.BanterResponseEntry[1];
				barkBanter.Responses[0] = new BlueprintBarkBanter.BanterResponseEntry { Unit = Companion2 };
			}
		}
	}
}