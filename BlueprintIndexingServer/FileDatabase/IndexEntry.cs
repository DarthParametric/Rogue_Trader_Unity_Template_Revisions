using System.Collections.Generic;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    internal readonly struct IndexEntry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Path;
        public readonly string TypeId;
        public readonly bool IsShadowDeleted;
        public readonly HashSet<string> DependsOnBlueprintsId;

        public IndexEntry(string id, string name, string typeId, string path, bool isShadowDeleted, HashSet<string> dependsOnBlueprintsId)
        {
            Id = id;
            Name = name;
            Path = path;
            TypeId = typeId;
            IsShadowDeleted = isShadowDeleted;
            DependsOnBlueprintsId = dependsOnBlueprintsId;
        }
    }
}