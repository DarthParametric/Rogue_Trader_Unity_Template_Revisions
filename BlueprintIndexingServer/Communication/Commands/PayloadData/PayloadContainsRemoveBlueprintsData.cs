using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadContainsRemoveBlueprintsDataExtension
{
    public static string ToJson(this PayloadContainsRemoveBlueprintsData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadContainsRemoveBlueprintsData
{
    [JsonPropertyName("contains_remove_blueprints_list")]
    public Dictionary<string, HashSet<string>> ContainsRemoveBlueprintsList { get; set; }
    
    public static PayloadContainsRemoveBlueprintsData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadContainsRemoveBlueprintsData>(json);
    }

    public static PayloadContainsRemoveBlueprintsData CreateEmpty() => new();
    
    public PayloadContainsRemoveBlueprintsData()
    {
        ContainsRemoveBlueprintsList = new Dictionary<string, HashSet<string>>();
    }
    
    public void Add(string removeBlueprintGuid, string blueprintGuid)
    {
        if (!ContainsRemoveBlueprintsList.ContainsKey(removeBlueprintGuid))
        {
            ContainsRemoveBlueprintsList.Add(removeBlueprintGuid, new HashSet<string>());
        }

        if (ContainsRemoveBlueprintsList[removeBlueprintGuid].Contains(blueprintGuid))
        {
            return;
        }

        ContainsRemoveBlueprintsList[removeBlueprintGuid].Add(blueprintGuid);
    }
}