using Kingmaker.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class RestrictionsHolderCreator : AssetCreatorBase
	{
		public override string CreatorName
			=> "Restrictions holder";

		public override string LocationTemplate
			=> "Assets/Mechanics/Blueprints/RestrictionsHolders/{name}.asset";

		public override object CreateAsset()
		{
			return new RestrictionsHolder();
		}
	}
}