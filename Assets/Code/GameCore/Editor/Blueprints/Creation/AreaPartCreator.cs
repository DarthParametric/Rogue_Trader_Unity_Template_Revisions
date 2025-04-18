using Kingmaker.Blueprints.Area;
using System;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
    public class AreaPartCreator : AssetCreatorBase
    {
        public BlueprintAreaReference Area;

        public override string CreatorName => "Area Part";
        public override string LocationTemplate => GetNewPath();
        
        private string GetNewPath()
        {
            if (Area?.Get() == null)
                return "Assets/Mechanics/Blueprints/World/Areas/{name}.asset";

            string pathToAsset = GetMatchingFolder(BlueprintsDatabase.GetAssetPath(Area));

            return pathToAsset + "/{name}.asset";
        }
        
        public override object CreateAsset()
        {
            return new BlueprintAreaPart();
        }
        
        public override bool CanCreateAssetsOfType(Type type)
        {
            return type == typeof(BlueprintAreaPart);
        }
    }
}