using System;
using Kingmaker.AreaLogic.Etudes;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintEtudeConflictingGroupCreator : AssetCreatorBase
	{
		public override string CreatorName 
			=> "Etude Conflicting Group";

		public override string LocationTemplate 
			=> "Assets/Mechanics/Blueprints/World/Etudes/ConflictingGroups/{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintEtudeConflictingGroup();
        }
        
		public override bool CanCreateAssetsOfType(Type type)
		{
			return type == typeof(BlueprintEtudeConflictingGroup);
		}
	}
}