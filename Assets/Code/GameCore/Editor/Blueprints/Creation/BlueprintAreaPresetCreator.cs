using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintAreaPresetCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Area Preset";
		public override string LocationTemplate => "{Area_Path}/Presets/{Area}_{name}_Preset.asset";
		
        public override object CreateAsset()
        {
            return new BlueprintAreaPreset();
        }

        public override void PostProcess(object asset)
		{
			((BlueprintAreaPreset)asset).Area = Area;
		}

		protected override string GetAdditionalTemplate(string propName)
		{
			if (propName == "Area_Path" && Area.Get())
			{
				return Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(Area));
			}
			return base.GetAdditionalTemplate(propName);
		}
	}
}