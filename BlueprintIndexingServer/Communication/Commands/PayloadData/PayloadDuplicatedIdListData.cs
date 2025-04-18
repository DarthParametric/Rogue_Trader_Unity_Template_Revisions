using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadDuplicatedIdListDataExtension
{
    public static string ToJson(this PayloadDuplicatedIdListData data)
    {
        return JsonSerializer.Serialize(data);
    }
}

public class PayloadDuplicatedIdListData
{
    [JsonPropertyName("duplicated_Id_list")]
    public List<string> DuplicatedIdList { get; set; }
    
    public static PayloadDuplicatedIdListData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadDuplicatedIdListData>(json);
    }

    public static PayloadDuplicatedIdListData CreateEmpty() => new();
    
    public static PayloadDuplicatedIdListData Create(IEnumerable<string> duplicatedIdList) => new(duplicatedIdList);
    
    public PayloadDuplicatedIdListData()
    {
        DuplicatedIdList = new List<string>();
    }

    public PayloadDuplicatedIdListData(IEnumerable<string> nameList)
    {
        DuplicatedIdList = new List<string>(nameList);
    }

    public void Add(string name)
    {
        DuplicatedIdList.Add(name);
    }
}