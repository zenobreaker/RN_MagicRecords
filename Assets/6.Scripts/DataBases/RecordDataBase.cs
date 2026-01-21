using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class RecordInfoJson
{
    public int id;
    public string namekeycode;
    public string description;
    public string iconPath;
}

[System.Serializable]
public class RecordInfoAllData
{
    public List<RecordInfoJson> recordInfoJson;
}

[System.Serializable]
public class RecordDataJson
{
    public int id;
    public int recordType;
    public string stat;
    public string targetFilter;
    public int calcType;
    public int value;
    public string triggerEvent;
    public string className;
    public int rarity;
}

[System.Serializable]
public class RecordDataJsonAllData
{
    public List<RecordDataJson> recordDataJson;
}


public class RecordDataBase : DataBase
{

    [SerializeField] private TextAsset recordDataJsonAsset;
    [SerializeField] private Dictionary<int, RecordData> recordDatas = new();
    private List<RecordData> recordDataList;
    public override void Initialize()
    {
        if (jsonAsset == null) return;

        Debug.Log("Rcord Database Init");
        recordDataList = new(); 

        JsonLoader.LoadJsonList<RecordInfoAllData, RecordInfoJson, RecordData>
            (
            jsonAsset,
            root => root.recordInfoJson,

            json=>
            {
                RecordData recordData = new RecordData();
                recordData.id = json.id;
                recordData.description = json.description;
                recordData.recordName = json.namekeycode;
                //recordData.icon =  json.IconPath;
                
                return recordData;
            }, 

            record =>
            {
                recordDatas.Add(record.id, record); 
            }
            );

        JsonLoader.LoadJsonList<RecordDataJsonAllData, RecordDataJson, RecordData>
            (
                recordDataJsonAsset,
                root => root.recordDataJson,

                json =>
                {
                    if (recordDatas.TryGetValue(json.id, out RecordData recordData))
                    {
                        recordData.targetFilter = GetTargetFilterType(json.targetFilter);
                        recordData.type = (RecordType)json.recordType;
                        recordData.rarity = (RecordRarity)json.rarity;
                        recordData.status = GetStatusType(json.stat);
                        recordData.valueType = (ModifierValueType)json.calcType;
                        recordData.effectValue = json.value;
                        recordData.triggerEvent = json.triggerEvent;
                        recordData.className = json.className;

                        return recordData;
                    }
                    return null; 
                },

                record =>
                {
                    recordDatas[record.id] = record;
                    recordDataList.Add(record);
                }
            );

    }

    public RecordData GetRecordData(int recordID)
    {
        return recordDatas.TryGetValue(recordID, out RecordData recordData) ? recordData : null;
    }

    public List<RecordData> GetAllRecordData() => recordDataList;

    private TargetFilterType GetTargetFilterType(string targetFilter)
    {
        if (targetFilter == null) return TargetFilterType.ALL;

        if (targetFilter.Equals("ALL")) return TargetFilterType.ALL;
        else if (targetFilter.Equals("Shooter")) return TargetFilterType.Shooter;

        return TargetFilterType.ALL;
    }

    private StatusType GetStatusType(string statusType)
    {
        switch (statusType)
        {
            case "ATK": return StatusType.ATTACK;
            case "DEF": return StatusType.DEFENSE;
            case "CRIT_RATIO": return StatusType.CRIT_RATIO;
            case "CRIT_DMG": return StatusType.CRIT_DMG;
            case "SPD": return StatusType.MOVESPEED;
            case "ASPD": return StatusType.ATTACKSPEED;
            case "HP": return StatusType.HEALTH;
            case "HP_REGEN": return StatusType.HEALTH_REGEN;
        }

        return StatusType.NONE;
    }
}
