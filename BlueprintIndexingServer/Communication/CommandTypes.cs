namespace Owlcat.Blueprints.Server.Communication;

public enum CommandTypes
{
    GetBaseFolder,
    GetIdFromPath,
    GetPathFromId,
    GetNameFromId,
    GetIsShadowDeletedFromId,
    GetContainsShadowDeletedBlueprintsFromId,
    GetListOfContainsShadowDeletedBlueprints,
    GetTypeIdFromId,
    SearchByName,
    SearchByType,
    GetListOfDuplicates,
    Result,
    SearchResult,
    SearchByNameExact,
    PauseIndexing,
    ResumeIndexing,
    GetListOfContainsRemoveBlueprints
}