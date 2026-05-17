using System.Collections.Generic;
using System.Linq;
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
    private RecordData emptyRecordTemplate;

    // 💡 1. 미완성이었던 딕셔너리 선언 완료 및 초기화
    private Dictionary<RecordRarity, List<RecordData>> recordDataByRarity = new();

    public override void Initialize()
    {
        if (jsonAsset == null) return;

        Debug.Log("Record Database Init");
        recordDataList = new();
        recordDataByRarity.Clear(); // 💡 재초기화를 대비해 클리어

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

                    // 💡 2. 파싱이 끝난 레코드를 타입별 딕셔너리에 분류해서 넣기
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
            effectValue = 0,
            triggerEvent = "",
            className = string.Empty,
        };
    }

    public RecordData GetEmptyRecord()
    {
        return emptyRecordTemplate.GetData();
    }

    public RecordData GetRecordData(int recordID)
    {
        return recordDatas.TryGetValue(recordID, out RecordData recordData) ? recordData.GetData() : null;
    }

    public List<RecordData> GetAllRecordData() => recordDataList.ToList();

    // 💡 3. 특정 타입의 레코드 리스트를 반환하는 함수 구현
    public List<RecordData> GetRecordDatas(RecordRarity rarity)
    {
        if (recordDataByRarity.TryGetValue(rarity, out List<RecordData> list))
        {
            // 외부에서 이 리스트를 수정하더라도 원본 DB가 훼손되지 않도록 ToList()로 복사본 반환
            return list.ToList();
        }

        // 해당 타입이 아예 없을 경우 에러 대신 빈 리스트 반환
        return new List<RecordData>();
    }

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