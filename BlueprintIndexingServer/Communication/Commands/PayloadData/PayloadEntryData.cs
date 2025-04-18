using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadEntryDataExtension
{
    public static string ToJson(this PayloadEntryData listData)
    {
        return JsonSerializer.Serialize(listData);
    }
}

public class PayloadEntryData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("is_shadow_deleted")]
    public bool? IsShadowDeleted { get; set; }

    [JsonPropertyName("contains_shadow_deleted_blueprints")]
    public bool? ContainsShadowDeletedBlueprints { get; set; }

    public static PayloadEntryData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadEntryData>(json);
    }
    
    public static PayloadEntryData Create(string? id, string? path, bool? isShadowDeleted, bool? containsShadowDeletedBlueprints) 
        => new(id, path, isShadowDeleted, containsShadowDeletedBlueprints);
    
    public PayloadEntryData() : this(null, null, null, null) { }

    public PayloadEntryData(string? id, string? path, bool? isShadowDeleted, bool? containsShadowDeletedBlueprints)
    {
        Id = id;
        Path = path;
        IsShadowDeleted = isShadowDeleted;
        ContainsShadowDeletedBlueprints = containsShadowDeletedBlueprints;
    }
}