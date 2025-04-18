using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetBaseFolder
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        return new Command { Type = CommandTypes.Result, Payload = PayloadPathData.Create(database.BasePath).ToJson() };
    }
}