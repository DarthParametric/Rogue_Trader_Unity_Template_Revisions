using System.Collections.Generic;

namespace Owlcat.Blueprints.Server.FileDatabase
{
    public interface IFileData
    {
        string Name { get; set; }
        string TypeId { get; set; }
        string UniqueId { get; set; }
        bool IsShadowDeleted { get; }
        HashSet<string> UsesBlueprints { get; set; }
    }
}