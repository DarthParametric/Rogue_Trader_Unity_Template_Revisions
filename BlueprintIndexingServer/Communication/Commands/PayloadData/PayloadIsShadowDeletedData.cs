using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadIsShadowDeletedDataExtension
{
    public static string ToJson(this PayloadIsShadowDeletedData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadIsShadowDeletedData
{
    [JsonPropertyName("is_shadow_deleted")]
    public bool? IsShadowDeleted { get; set; }

    public static PayloadIsShadowDeletedData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadIsShadowDeletedData>(json);
    }

    public static PayloadIsShadowDeletedData Create(bool? isShadowDeleted) 
        => new(isShadowDeleted);
    
    public PayloadIsShadowDeletedData() : this(null) { }

    public PayloadIsShadowDeletedData(bool? isShadowDeleted)
    {
        IsShadowDeleted = isShadowDeleted;
    }
}