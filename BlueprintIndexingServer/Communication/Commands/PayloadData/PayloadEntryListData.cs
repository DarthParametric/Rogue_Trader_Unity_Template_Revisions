using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Owlcat.Blueprints.Server.Communication.Commands.PayloadData;

public static class PayloadEntryListDataExtension
{
    public static string ToJson(this PayloadEntryListData listData)
    {
        return JsonSerializer.Serialize(listData);
    }
}

public class PayloadEntryListData
{
    [JsonPropertyName("entry_data_list")] 
    public List<PayloadEntryData> EntryDataList { get; set; }
    
    public static PayloadEntryListData? FromJson(string json)
    {
        return JsonSerializer.Deserialize<PayloadEntryListData>(json);
    }

    public static PayloadEntryListData CreateEmpty() => new();
    public static PayloadEntryListData Create(IEnumerable<PayloadEntryData> entryDataLists) => new(entryDataLists);
    
    public PayloadEntryListData()
    {
        EntryDataList = new List<PayloadEntryData>();
    }
    
    public PayloadEntryListData(IEnumerable<PayloadEntryData> entryDataList)
    {
        EntryDataList = new List<PayloadEntryData>(entryDataList);
    }

    public void Add(PayloadEntryData payloadEntryData)
    {
        EntryDataList.Add(payloadEntryData);
    }
}