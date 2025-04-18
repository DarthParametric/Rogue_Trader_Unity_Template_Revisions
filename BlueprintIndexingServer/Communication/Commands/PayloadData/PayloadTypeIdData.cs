using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadTypeIdDataExtension
{
    public static string ToJson(this PayloadTypeIdData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadTypeIdData
{
    [JsonPropertyName("type_id")]
    public string? TypeId { get; set; }
    
    public static PayloadTypeIdData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadTypeIdData>(json);
    }

    public static PayloadTypeIdData Create(string? typeId) 
        => new(typeId);
    
    public PayloadTypeIdData() : this(null) { }

    public PayloadTypeIdData(string? typeId)
    {
        TypeId = typeId;
    }
}