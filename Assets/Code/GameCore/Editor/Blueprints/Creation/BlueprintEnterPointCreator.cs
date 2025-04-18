using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintEnterPointCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Area Enter Point";
		public override string LocationTemplate => "{Area_Path}/EnterPoints/{Area}_{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintAreaEnterPoint();
        }
        
        public override void PostProcess(object asset)
        {
			((BlueprintAreaEnterPoint)asset).Area = Area;
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