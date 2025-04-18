using System;
using System.IO;
using System.Linq;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;
using UnityEditor;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintAreaMechanicsCreator : AssetCreatorBase
	{
		private const string MechanicsAddSceneTemplate = "Assets/Scenes/!Templates/Template_Mechanics_Add.unity";

		public BlueprintAreaReference Area;

		public override string CreatorName => "Area Mechanics";

		public override string LocationTemplate => "{Area_Path}/Addons/{Area}_{name}.asset";

        public override object CreateAsset()
        {
            return new BlueprintAreaMechanics();
        }

		protected override string GetAdditionalTemplate(string propName)
		{
			if (propName == "Area_Path" && Area.Get()!=null)
			{
				return Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(Area));
			}
			return base.GetAdditionalTemplate(propName);
		}

        public override void PostProcess(object asset)
		{
			var mec = (BlueprintAreaMechanics)asset;
			var mechanicsPath = Path.GetDirectoryName(Area.Get().DynamicScene.ScenePath);
			mechanicsPath = mechanicsPath + $"/{mec.name}_Mechanics.unity";

			AssetDatabase.CopyAsset(MechanicsAddSceneTemplate, mechanicsPath);

			mec.Area = Area;
			mec.Scene = new SceneReference(AssetDatabase.LoadAssetAtPath<SceneAsset>(mechanicsPath));

			EditorBuildSettings.scenes =
				EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene { path = mechanicsPath } }).ToArray();

			mec.SetDirty();

			AssetDatabase.SaveAssets();
            BlueprintsDatabase.SaveAllDirty();
			Selection.activeObject = BlueprintEditorWrapper.Wrap(mec);
		}

	    public override bool CanCreateAssetsOfType(Type type)
	    {
	        return type == typeof(BlueprintAreaMechanics);
	    }

    }
}