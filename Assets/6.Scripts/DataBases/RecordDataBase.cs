using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// JSON 매핑용 DTO 클래스들
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

[Serializable]
public class RecordStatJson
{
    public string stat;
    public int calcType;
    public float value;
}

[Serializable]
public class RecordSkillJson
{
    public int skillID;
    public string modifier; // 💡 JSON에서 문자열로 넘어오므로 string으로 받아서 파싱
    public string operation; // 💡 JSON에서 문자열로 넘어오므로 string으로 받아서 파싱
    public float value;
}

[Serializable]
public class RecordTriggerJson
{
    public string triggerEvent;
    public string className;
}

[Serializable]
public class RecordDataJson
{
    public int id;
    public List<RecordStatJson> stats = new();
    public int rarity;
    public int recordType;
    public string targetFilter;
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
    private RecordData emptyRecordTemplate;

    // 미완성이었던 딕셔너리 선언 완료 및 초기화
    private Dictionary<RecordRarity, List<RecordData>> recordDataByRarity = new();

    public override void Initialize()
    {
        if (jsonAsset == null) return;

        Debug.Log("Record Database Init");
        recordDataList = new();
        recordDataByRarity.Clear();

        // 1. Info 파싱
        JsonLoader.LoadJsonList<RecordInfoAllData, RecordInfoJson, RecordData>
            (
            jsonAsset,
            root => root.recordInfoJson,
            json =>
            {
                RecordData recordData = new RecordData();
                recordData.id = json.id;
                recordData.description = json.description;
                recordData.recordName = json.namekeycode;
                // recordData.icon = GetSprite(json.iconPath); // Addressable 등 사용 시 연동
                return recordData;
            },
            record =>
            {
                recordDatas.Add(record.id, record);
            }
            );

        // 2. Data 파싱 및 병합
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

                        // 💡 리스트 형태 데이터 파싱 (Null 방어)
                        if (json.stats != null)
                        {
                            recordData.Stats = json.stats.Select(s => new RecordStatData
                            {
                                Status = GetStatusType(s.stat),
                                ValueType = (ModifierValueType)s.calcType,
                                Value = s.value
                            }).ToList();
                        }

                        return recordData;
                    }
                    return null;
                },
                record =>
                {
                    recordDatas[record.id] = record;
                    recordDataList.Add(record);

                    // 파싱이 끝난 레코드를 타입별 딕셔너리에 분류
                    if (!recordDataByRarity.ContainsKey(record.rarity))
                    {
                        recordDataByRarity[record.rarity] = new List<RecordData>();
                    }
                    recordDataByRarity[record.rarity].Add(record);
                }
            );

        CreateEmptyRecordTemplate();
    }

    private void CreateEmptyRecordTemplate()
    {
        emptyRecordTemplate = new RecordData
        {
            id = -1,
            recordName = "name_emptymemory",
            description = "desc_emptyememory",
            rarity = RecordRarity.NORMAL,
            targetFilter = TargetFilterType.ALL,
            type = RecordType.EMPTY,
        };
    }

    public RecordData GetEmptyRecord() => emptyRecordTemplate.GetData();
    public RecordData GetRecordData(int recordID) => recordDatas.TryGetValue(recordID, out RecordData recordData) ? recordData.GetData() : null;
    public List<RecordData> GetAllRecordData() => recordDataList.ToList();

    public List<RecordData> GetRecordDatas(RecordRarity rarity)
    {
        if (recordDataByRarity.TryGetValue(rarity, out List<RecordData> list))
            return list.ToList();
        return new List<RecordData>();
    }

    private TargetFilterType GetTargetFilterType(string targetFilter)
    {
        if (string.IsNullOrEmpty(targetFilter) || targetFilter.Equals("ALL")) return TargetFilterType.ALL;
        else if (targetFilter.Equals("Shooter")) return TargetFilterType.Shooter;
        return TargetFilterType.ALL;
    }

    public StatusType GetStatusType(string statusType)
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