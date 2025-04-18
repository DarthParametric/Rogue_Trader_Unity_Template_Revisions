using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadNameDataExtension
{
    public static string ToJson(this PayloadNameData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadNameData
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    public static PayloadNameData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadNameData>(json);
    }
    
    public static PayloadNameData Create(string? name) 
        => new(name);
    
    public PayloadNameData() : this(null) { }

    public PayloadNameData(string? name)
    {
        Name = name;
    }
}