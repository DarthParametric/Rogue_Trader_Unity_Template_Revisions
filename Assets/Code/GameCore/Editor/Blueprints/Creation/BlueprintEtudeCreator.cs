using System;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintEtudeCreator : AssetCreatorBase
	{
		public BlueprintEtudeReference Parent;

		public override string CreatorName => "Etude";

		public override string LocationTemplate => GetNewPath();

		private string GetNewPath()
		{
			if (Parent?.Get() == null)
				return "Assets/Mechanics/Blueprints/World/Etudes/{name}.asset";

			string pathToAsset = GetMatchingFolder(BlueprintsDatabase.GetAssetPath(Parent));

			return pathToAsset + "/" + Parent.NameSafe() + "/{name}.asset";

		}

        public override object CreateAsset()
        {
            return new BlueprintEtude();
        }

		public override bool CanCreateAssetsOfType(Type type)
		{
			return type == typeof(BlueprintEtude);
		}

		public override void PostProcess(object asset)
		{
			var etude = (BlueprintEtude)asset;
			etude.Parent = Parent;
			etude.SetDirty();
			BlueprintsDatabase.SaveAllDirty();
			Parent = Parent.GetBlueprint().ToReference<BlueprintEtudeReference>();
		}
	}
}