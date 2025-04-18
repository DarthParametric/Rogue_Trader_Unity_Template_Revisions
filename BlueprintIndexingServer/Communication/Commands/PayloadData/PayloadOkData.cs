using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadOkDataExtension
{
    public static string ToJson(this PayloadOkData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadOkData
{
    [JsonPropertyName("ok")] 
    public bool Ok => true;
    
    public static PayloadOkData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadOkData>(json);
    }

    public static PayloadOkData Create() 
        => new();
    
    public PayloadOkData() {}
}