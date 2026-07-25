using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
    [Header("Addressables Settings")]
    [Tooltip("어드레서블에서 긁어올 라벨 이름")]
    [SerializeField] private string recordLabel = "RecordData";

    [SerializeField] private TextAsset recordDataJsonAsset;
    [SerializeField] private Dictionary<int, RecordData> recordDatas = new();
    private List<RecordData> recordDataList;
    private RecordData emptyRecordTemplate;

    // 미완성이었던 딕셔너리 선언 완료 및 초기화
    private Dictionary<RecordRarity, List<RecordData>> recordDataByRarity = new();
    private Dictionary<RecordType, List<int>> recordIDByType = new(); 

    public override void Initialize()
    {
        if (jsonAsset == null) return;

        recordDataList = new();
        recordDataByRarity.Clear();

        Debug.Log("Record Database Init - Addressables Start");

        CreateEmptyRecordTemplate();

        // 비동기 로드 시작
        LoadRecordsFromAddressablesAsync().Forget();
    }

    private async UniTaskVoid LoadRecordsFromAddressablesAsync()
    {
        recordDatas.Clear();
        recordDataList.Clear();
        recordDataByRarity.Clear();

        try
        {
            // 1. 해당 라벨(RecordData)이 달린 모든 에셋을 메모리에 로드합니다.
            // (에셋이 많아도 렉이 걸리지 않도록 await로 비동기 대기)
            IList<SO_RecordData> loadedRecords = await Addressables.LoadAssetsAsync<SO_RecordData>(recordLabel, null);

            // 2. 로드된 에셋들을 순회하며 딕셔너리에 세팅
            foreach (var recordSO in loadedRecords)
            {
                if (recordSO == null) continue;

                // SO 내부의 실제 데이터 추출
                RecordData recordData = recordSO.GetRecordData();

                // 리스트와 딕셔너리에 쏙쏙 집어넣기
                recordDatas[recordData.id] = recordData;
                recordDataList.Add(recordData);

                if (!recordDataByRarity.ContainsKey(recordData.rarity))
                {
                    recordDataByRarity[recordData.rarity] = new List<RecordData>();
                }
                recordDataByRarity[recordData.rarity].Add(recordData);

                if (!recordIDByType.ContainsKey(recordData.type))
                    recordIDByType[recordData.type] = new List<int>();
                recordIDByType[recordData.type].Add(recordData.id); 
            }

            Debug.Log($"[RecordDataBase] {loadedRecords.Count}개의 레코드 데이터 로드 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RecordDataBase] 어드레서블 로드 실패: {e.Message}");
        }
    }
    private void OldJson()
    {

        Debug.Log("Record Database Init");
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