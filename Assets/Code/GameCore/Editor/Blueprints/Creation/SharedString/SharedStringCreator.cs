using Kingmaker.Editor.Blueprints.Creation.Naming;
using Kingmaker.Localization;

#nullable enable

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class SharedStringCreator : NamingCreatorBase
	{
		protected override string NameTokenNotEmpty
			=>  NameToken + "_";

		protected override string DefaultFolder => "Assets/Mechanics/Blueprints/";

		protected override string DefaultTemplate
			=> "Assets/Mechanics/Blueprints/DLC/{Chapter}/Narrative/{Location}/{StringType}/{name}_{StringType}.asset";

		protected override string Template
			=> Templates.instance == null
				? DefaultTemplate
				: Templates.instance.SharedString;

		public override string CreatorName => "Shared String";

		public override bool CreatesBlueprints => false;

		public override object CreateAsset()
		{
			return CreateInstance<SharedStringAsset>();
		}
	}
}