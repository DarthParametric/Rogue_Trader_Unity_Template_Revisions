using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadIdDataExtension
{
    public static string ToJson(this PayloadIdData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadIdData
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    public static PayloadIdData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadIdData>(json);
    }

    public static PayloadIdData Create(string? id) 
        => new(id);
    
    public PayloadIdData() : this(null) { }

    public PayloadIdData(string? id)
    {
        Id = id;
    }
}