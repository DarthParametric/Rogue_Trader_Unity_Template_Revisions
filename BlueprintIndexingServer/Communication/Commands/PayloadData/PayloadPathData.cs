using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadPathDataExtension
{
    public static string ToJson(this PayloadPathData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadPathData
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    public static PayloadPathData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadPathData>(json);
    }
    
    public static PayloadPathData Create(string? path)
    {
        return new PayloadPathData(path);
    }
    
    public PayloadPathData() : this(null) { }

    public PayloadPathData(string? path)
    {
        Path = path;
    }
}