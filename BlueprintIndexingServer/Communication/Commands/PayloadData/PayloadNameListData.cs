using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadNameListDataExtension
{
    public static string ToJson(this PayloadNameListData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadNameListData
{
    [JsonPropertyName("name_list")]
    public List<string> NameList { get; set; }
    
    public static PayloadNameListData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadNameListData>(json);
    }

    public static PayloadNameListData CreateEmpty() => new();
    
    public static PayloadNameListData Create(IEnumerable<string> nameList) => new(nameList);
    
    public PayloadNameListData()
    {
        NameList = new List<string>();
    }

    public PayloadNameListData(IEnumerable<string> nameList)
    {
        NameList = new List<string>(nameList);
    }

    public void Add(string name)
    {
        NameList.Add(name);
    }
}