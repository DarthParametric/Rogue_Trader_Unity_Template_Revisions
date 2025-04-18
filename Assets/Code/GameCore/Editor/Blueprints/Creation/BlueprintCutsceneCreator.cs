using Kingmaker.AreaLogic.Cutscenes;
using Kingmaker.Blueprints;
using Kingmaker.Editor.Cutscenes;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintCutsceneCreator : AssetCreatorBase
	{
		public BlueprintAreaReference Area;

		public override string CreatorName => "Cutscene";
		public override string LocationTemplate => "Assets/Mechanics/Blueprints/World/Cutscenes/{Area?}/{name}/{name}.asset";

		
        public override object CreateAsset()
        {
            return new Cutscene();
        }
        
        public override void PostProcess(object asset)
        {
			CutsceneEditorWindow.OpenAssetInCutsceneEditor((Cutscene)asset);
		}
	}
}