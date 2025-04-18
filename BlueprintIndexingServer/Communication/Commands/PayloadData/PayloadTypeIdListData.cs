using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadTypeIdListDataExtension
{
    public static string ToJson(this PayloadTypeIdListData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadTypeIdListData
{
    [JsonPropertyName("type_id_list")]
    public List<string> TypeIdList { get; set; }
    
    public static PayloadTypeIdListData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadTypeIdListData>(json);
    }

    public static PayloadTypeIdListData CreateEmpty() => new();
    
    public static PayloadTypeIdListData Create(IEnumerable<string> nameList) => new(nameList);
    
    public PayloadTypeIdListData()
    {
        TypeIdList = new List<string>();
    }

    public PayloadTypeIdListData(IEnumerable<string> nameList)
    {
        TypeIdList = new List<string>(nameList);
    }

    public void Add(string name)
    {
        TypeIdList.Add(name);
    }
}