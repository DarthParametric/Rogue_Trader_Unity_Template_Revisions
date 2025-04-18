using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingGetListOfDuplicates
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        return new Command { Type = CommandTypes.Result, Payload = PayloadDuplicatedIdListData.Create(database.GetDuplicatedIds()).ToJson() };
    }
}