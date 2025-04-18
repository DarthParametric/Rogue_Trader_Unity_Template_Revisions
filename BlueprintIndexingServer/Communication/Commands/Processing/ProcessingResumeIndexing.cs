using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public static class ProcessingResumeIndexing
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        database.ResumeIndexing();
        return new Command { Type = CommandTypes.SearchResult, Payload = PayloadOkData.Create().ToJson() };
    }
}