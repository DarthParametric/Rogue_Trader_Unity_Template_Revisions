using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadTypeDataExtension
{
    public static string ToJson(this PayloadTypeData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadTypeData
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    public static PayloadTypeData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadTypeData>(json);
    }

    public static PayloadTypeData Create(string? type) 
        => new(type);
    
    public PayloadTypeData() : this(null) { }

    public PayloadTypeData(string? type)
    {
        Type = type;
    }
}