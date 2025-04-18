using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public enum SceneReferenceType
	{
		Mechanics,
		Static,
		Light,
		Audio,
	}

	public class SceneCreator : AssetCreatorBase
	{ 
		public override string CreatorName
			=> "Scene Reference";

		public int ChapterNumber = 2;
		public SceneReferenceType SceneType;

		public string AdditionalSuffix;

		public override string LocationTemplate
			=> $"Assets/Scenes/Chapter_{ChapterNumber}" + "/{name}/{name}" + $"_{SceneType}{AdditionalSuffix}.unity";

		public string DefaultPath
			=> "Assets/Scenes/default.unity";

		public override bool CreatesBlueprints => false;

		public override object CreateAsset()
		{
			string dir = Path.GetDirectoryName(DefaultPath);
			if (dir != null)
				Directory.CreateDirectory(dir);
			
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
			EditorSceneManager.SaveScene(scene, DefaultPath);
			EditorSceneManager.CloseScene(scene, true);
			return AssetDatabase.LoadAssetAtPath(DefaultPath, typeof(Object));
		}
	}
}