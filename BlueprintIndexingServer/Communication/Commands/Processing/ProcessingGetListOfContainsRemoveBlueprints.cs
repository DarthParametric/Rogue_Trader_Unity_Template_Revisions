using Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

namespace Owlcat.Blueprints.Server.Communication.Commands.Processing;

public class ProcessingGetListOfContainsRemoveBlueprints
{
    public static Command? Processing(Command command, FileDatabase.FileDatabase database)
    {
        var responseData = PayloadContainsRemoveBlueprintsData.CreateEmpty();

        var removeList = database.GetAllRemoveBlueprints();
        foreach (var removeGuid in removeList)
        {
            var dependingOn = database.GetAllDependingOn(removeGuid);
            foreach (var guid in dependingOn)
            {
                responseData.Add(removeGuid, guid);
            }
        }
        
        return new Command { Type = CommandTypes.Result, Payload = responseData.ToJson() };
    }
}