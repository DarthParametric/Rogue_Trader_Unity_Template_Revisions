using System.IO;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.EditorDatabase;

namespace Kingmaker.Editor.Blueprints.Creation
{
	public class BlueprintUnitCreator : AssetCreatorBase
	{
		public enum UnitType
		{
			Reference,
			Monster,
			Boss,
			NPC
		}

		public UnitType Type;
		public BlueprintUnitReference Prototype;
		public BlueprintAreaReference Area;

		public override string CreatorName => "Unit";
		public override string LocationTemplate 
		{
			get
			{
				switch (Type)
				{
						case UnitType.Reference:
							return "Assets/Mechanics/Blueprints/Units/Monsters/{folder}/{name}.asset";
						case UnitType.Monster:
							return "{prototype_path}/{Area}/{name}.asset";
						case UnitType.Boss:
							return "Assets/Mechanics/Blueprints/Units/Bosses/{name}/{name}.asset";
						case UnitType.NPC:
							return "Assets/Mechanics/Blueprints/Units/NPC/{folder}/{Area}/{name}.asset";
				}
				return "{wtf}";
			}
		}
		
        public override object CreateAsset()
        {
            return new BlueprintUnit();
        }

		public override void PostProcess(object asset)
		{
			var u = (BlueprintUnit) asset;
			// todo: [bp] fix when prototypes are a thing
			u.SetPrototype(Prototype.Get());
			u.SetDirty();
		}

		public override bool ShouldSkipProperty(string propName)
		{
			if (propName == nameof(Area))
				return Type != UnitType.Monster && Type != UnitType.NPC;
			return base.ShouldSkipProperty(propName);
		}

		protected override string GetAdditionalTemplate(string propName)
		{
			if (propName == "prototype_path")
			{
				return Prototype.Get() ? Path.GetDirectoryName(BlueprintsDatabase.GetAssetPath(Prototype)) : null;
			}
			return base.GetAdditionalTemplate(propName);
		}
	}
}