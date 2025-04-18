using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetNameFromId
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        return new Command { Type = CommandTypes.Result, Payload = PayloadNameData.Create(null).ToJson() };
    }
}