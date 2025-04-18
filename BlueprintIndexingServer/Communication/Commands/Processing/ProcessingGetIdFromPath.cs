using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetIdFromPath
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var requestPayload = PayloadPathData.FromJson(command.Payload);
        return new Command { Type = CommandTypes.Result, Payload = PayloadIdData.Create(database.PathToId(requestPayload?.Path ?? "")).ToJson() };
    }
}