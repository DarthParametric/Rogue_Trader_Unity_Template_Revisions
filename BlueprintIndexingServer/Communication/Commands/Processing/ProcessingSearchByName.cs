using System.Collections.Generic;
using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingSearchByName
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var payloadData = PayloadNameListData.FromJson(command.Payload);
        var ids = database.SearchByName(payloadData?.NameList ?? new List<string>());
        var result = PayloadEntryListData.CreateEmpty();
        foreach (var id in ids)
        {
            var path = database.IdToPath(id);
            var isShadowDeleted = database.IdToIsShadowDeleted(id);
            var containsShadowDeletedBlueprints = database.ContainsShadowDeletedBlueprints(id);
            result.Add(PayloadEntryData.Create(id, path, isShadowDeleted, containsShadowDeletedBlueprints));
        }
        return new Command { Type = CommandTypes.SearchResult, Payload = result.ToJson() };
    }
}