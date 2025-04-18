using Kingmaker.Blueprints;
using Kingmaker.DialogSystem.Blueprints;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintDialogCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Dialog";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Dialogs/{folder}/{Area?}/{name}/Dialogue_{name}.asset";
		
        public override object CreateAsset()
        {
            return new BlueprintDialog();
        }

        // todo: [bp] fix this when dialog editor can show blueprints
    }
}