using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class AreaPartBoundsCreator:AssetCreatorBase
    {
        public BlueprintAreaReference Area;
        public override string CreatorName => "Area Part Bounds";
        public override string LocationTemplate => GetNewPath();

        private string GetNewPath()
        {
            if (Area?.Get() == null)
                return "Assets/Mechanics/Blueprints/World/Areas/{name}.asset";

            string pathToAsset = BlueprintsDatabase.GetAssetPath(Area);

            return GetMatchingFolder(pathToAsset) + "/Bounds/{name}.asset";
        }
        
       public override bool CreatesBlueprints => false;

        public override object CreateAsset()
        {
            return CreateInstance<AreaPartBounds>();
        }

        public override bool CanCreateAssetsOfType(Type type)
        {
            return type == typeof(AreaPartBounds);
        }
    }
}