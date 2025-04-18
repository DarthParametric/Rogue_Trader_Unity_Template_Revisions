using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingSearchByNameExact
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var requestPayload = PayloadNameData.FromJson(command.Payload);
        var id = database.GetByName(requestPayload?.Name ?? "");
        return new Command { Type = CommandTypes.SearchResult, Payload = PayloadIdData.Create(id).ToJson() };
    }
}