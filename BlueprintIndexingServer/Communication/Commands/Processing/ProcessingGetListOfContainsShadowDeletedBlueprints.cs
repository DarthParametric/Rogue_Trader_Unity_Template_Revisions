using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetListOfContainsShadowDeletedBlueprints
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var ids = database.SearchAllUsingShadowDeletedBlueprints();
        var result = PayloadEntryListData.CreateEmpty();

        foreach (var id in ids)
        {
            var path = database.IdToPath(id);
            var isShadowDeleted = database.IdToIsShadowDeleted(id);
            result.Add(PayloadEntryData.Create(id, path, isShadowDeleted, true));
        }
        
        return new Command { Type = CommandTypes.Result, Payload = result.ToJson() };
    }
}