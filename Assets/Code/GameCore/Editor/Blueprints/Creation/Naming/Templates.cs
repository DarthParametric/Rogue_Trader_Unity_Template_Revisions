using System;
using UnityEditor;
using UnityEngine;

namespace Kingmaker.Editor.Blueprints.Creation.Naming
{
	[FilePath("Assets/Editor/Naming/" + nameof(Templates) + ".asset", FilePathAttribute.Location.ProjectFolder)]
	public class Templates : ScriptableSingleton<Templates>
	{
		[Serializable]
		public record BlueprintAreaSettings
		{
			public string BlueprintAreaTemplate;
			public string MechanicsSceneTemplate;
			public string StaticSceneTemplate;
			public string LightSceneTemplate;
			public SceneAsset MechanicsTemplateScene;
			public SceneAsset StaticTemplateScene;
			public SceneAsset LightTemplateScene;
		}

		[Serializable]
		public record BlueprintStarSystemMapSettings
		{
			public string BlueprintStarSystemMapTemplate;
			public string MechanicsSceneTemplate;
			public SceneAsset MechanicsTemplateScene;
		}

		[SerializeField]
		public string SharedString;

		[SerializeField]
		public BlueprintAreaSettings BlueprintArea;

		[SerializeField]
		public BlueprintStarSystemMapSettings BlueprintStarSystemMap;
	}
}