using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RecordManager : MonoBehaviour
{
    //TODO : 스크립터블 오브젝트 넣는 변수 
    [SerializeField] private List<SO_RecordData> records = new List<SO_RecordData>();

    public List<RecordData> CurrentOptions { get; private set; } = new();

    private Dictionary<int, SO_RecordData> recordsDict = new Dictionary<int, SO_RecordData>();
    private int generateCount = 3;

    public void Start()
    {
        foreach (var record in records)
            recordsDict.Add(record.id, record);
    }

    public void GenerateOption(int count = 3)
    {
        // 1. 전체 데이터에서 랜덤 추출 
        List<RecordData> allRecord = AppManager.Instance?.GetAllRecordData();

        // 2. 현재 플레이 중인 직업 정보 
        //TODO : 플레이 중인 것의 job 정보가 필요함 
        int job = 1;

        // 3. 필터링 
        CurrentOptions = allRecord
            .Where(r => r.targetFilter == TargetFilterType.ALL || r.IsTarget(job))
            .OrderBy(x => Random.value)
            .Take(count)
            .ToList();

        // 4. AppManager를 통해 UI 오픈 이벤트 발행
        AppManager.Instance?.TriggerRecordUI(CurrentOptions);
    }

    public RecordPassive GetRecordPassive(int id)
    {
        if (recordsDict.ContainsKey(id))
            return (RecordPassive)recordsDict[id].CreateRecord();

        return null;
    }

    public void RerollAllCurrentRecords()
    {
        // 1. 전체 데이터에서 랜덤 추출 
        List<RecordData> allRecord = AppManager.Instance?.GetAllRecordData();

        // 2. 중복 방지 
        if (CurrentOptions.Count > 0)
        {
            allRecord.RemoveAll(data => CurrentOptions.Exists(last => last.id == data.id));
        }

        // 셔플 및 추출 
        List<RecordData> newOptions = new();
        for (int i = 0; i < generateCount && allRecord.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, allRecord.Count);
            newOptions.Add(allRecord[randomIndex]);
            allRecord.RemoveAt(randomIndex); 
        }

        // 현재 옵션 저장
        CurrentOptions = newOptions;

        AppManager.Instance?.TriggerRecordUI(CurrentOptions);
    }

    public void OnReturendStageSelectScene()
    {

    }
}
