using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadContainsShadowDeletedBlueprintsDataExtension
{
    public static string ToJson(this PayloadContainsShadowDeletedBlueprintsData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadContainsShadowDeletedBlueprintsData
{
    [JsonPropertyName("contains_shadow_deleted_blueprints")]
    public bool? ContainsShadowDeletedBlueprints { get; set; }

    public static PayloadContainsShadowDeletedBlueprintsData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadContainsShadowDeletedBlueprintsData>(json);
    }

    public static PayloadContainsShadowDeletedBlueprintsData Create(bool? containsShadowDeletedBlueprints) 
        => new(containsShadowDeletedBlueprints);
    
    public PayloadContainsShadowDeletedBlueprintsData() : this(null) { }

    public PayloadContainsShadowDeletedBlueprintsData(bool? containsShadowDeletedBlueprints)
    {
        ContainsShadowDeletedBlueprints = containsShadowDeletedBlueprints;
    }
}