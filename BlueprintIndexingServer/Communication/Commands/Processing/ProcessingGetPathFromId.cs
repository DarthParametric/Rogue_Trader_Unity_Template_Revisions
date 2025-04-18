using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetPathFromId
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var payloadData = PayloadIdData.FromJson(command.Payload);
        return new Command { Type = CommandTypes.Result, Payload = PayloadPathData.Create(database.IdToPath(payloadData?.Id ?? "")).ToJson() };
    }
}