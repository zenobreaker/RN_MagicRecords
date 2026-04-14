using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class RecordManager : MonoBehaviour
{
    [SerializeField] private List<SO_RecordData> records = new List<SO_RecordData>();

    public List<RecordData> CurrentOptions { get; private set; } = new();

    public List<RecordData> SelectedRecords { get; private set; } = new();

    private Dictionary<int, SO_RecordData> recordsDict = new Dictionary<int, SO_RecordData>();
    private int generateCount = 3;
    private int rerollCount;
    public int RerollCount => rerollCount;

    private int max_selectCount = 1;
    private List<RecordData> possesRecords = new List<RecordData>();
    private bool isReceived = false; 

    private bool isDirty = false;

    public void SetReceiveRecordFlag() => isReceived = true;

    public void OnInit()
    {
        foreach (var record in records)
            recordsDict.Add(record.id, record);
        rerollCount = 3;

        RecordSaveData saveData = SaveManager.LoadRecordData();
        if (saveData != null)
            ApplySavedRecords(saveData);
    }

    private void ApplySavedRecords(RecordSaveData saveData)
    {
        if (saveData == null) return; 

        foreach(int id in saveData.recordIDs)
        {
            possesRecords.Add(recordsDict[id].GetRecordData()); 
        }

        isReceived = saveData.isReceived;
    }
    
    public List<RecordData> GetPossesRecord() => possesRecords;

    // 이벤트 노드에서 호출할 때 사용 
    public void GenerateEventRecord()
    {
        isReceived = false;
        GenerateOption(generateCount);
    }

    public void GenerateOption(int count = 3)
    {
        if (isReceived) return; // 이미 받은 상태였다면 

        SelectedRecords = new List<RecordData>();

        // 1. 전체 데이터에서 랜덤 추출 
        List<RecordData> allRecord = AppManager.Instance?.GetAllRecordData();

        // 2. 현재 가지고 있는 레코드들이 있다면 제외 
        if (possesRecords.Count > 0)
        {
            allRecord.RemoveAll(data => possesRecords.Exists(last => last.id == data.id));
        }

        if (CurrentOptions.Count > 0)
        {
            allRecord.RemoveAll(data => CurrentOptions.Exists(last => last.id == data.id));
        }

        // 3. 현재 플레이 중인 직업 정보 
        //TODO : 플레이 중인 것의 job 정보가 필요함 
        int job = 1;

        if (allRecord.Count > 0)
        {
            // 4. 필터링 
            CurrentOptions = allRecord
                .Where(r => r.targetFilter == TargetFilterType.ALL || r.IsTarget(job))
                .OrderBy(x => Random.value)
                .Take(count)
                .ToList();
        }
        // 5. 가질 레코드가 하나도 없다면 빈 메모리 레코드를 쥐어준다. 
        else
        {
            CurrentOptions.Add(AppManager.Instance?.GetEmptyRecord());
        }

        // 5. AppManager를 통해 UI 오픈 이벤트 발행
        AppManager.Instance?.TriggerRecordUI(CurrentOptions);
    }

    // 선택된 레코드 저장 
    public void SelectedRecord(int id)
    {
        if (recordsDict.ContainsKey(id))
            SelectedRecord(recordsDict[id].GetRecordData());
    }

    public void SelectedRecord(RecordData data)
    {
        if (data == null) return;
        if (SelectedRecords.Contains(data) == false)
        {
            if (SelectedRecords.Count >= max_selectCount)
            {
                SelectedRecords.RemoveAt(0);
            }
            SelectedRecords.Add(data);
        }
        else
            SelectedRecords.Remove(data);

    }

    public RecordPassive GetRecordPassive(int id)
    {
        if (recordsDict.ContainsKey(id))
            return (RecordPassive)recordsDict[id].CreateRecord();

        return null;
    }

    public bool IsSelectedRecord(RecordData selectedData)
    {
        return SelectedRecords.Contains(selectedData);
    }

    public void AddRecord(RecordData recordData)
    {
        possesRecords.Add(recordData);
        isReceived = true; 
        isDirty = true;
    }


    public void RerollAllCurrentRecords()
    {
        if (rerollCount <= 0)
        {
            // TODO: 알림 메세지 UI 호출
            return;
        }

        // 1. 후보군 생성 (전체 - 이미 영구 보유 중인 것들)
        // 현재 떠 있는 것(CurrentOptions)은 제외하지 않습니다. 
        // 그래야 리롤 시점에 다시 나올 기회를 얻어 '빈 레코드'가 성급하게 뜨지 않습니다.
        var allRecord = AppManager.Instance?.GetAllRecordData();
        var candidates = allRecord
            .Where(data => !possesRecords.Exists(p => p.id == data.id))
            .ToList();

        // 2. 현재 잠금(Lock)된 데이터들은 후보군에서 즉시 제거하여 중복 생성 방지
        foreach (var opt in CurrentOptions)
        {
            if (opt.isLocked)
            {
                candidates.RemoveAll(c => c.id == opt.id);
            }
        }

        List<RecordData> newOptions = new List<RecordData>();

        // 3. 새로운 옵션 구성
        for (int i = 0; i < generateCount; i++)
        {
            // 현재 인덱스가 잠금 상태라면 그대로 유지
            if (i < CurrentOptions.Count && CurrentOptions[i].isLocked)
            {
                newOptions.Add(CurrentOptions[i]);
                continue;
            }

            // 뽑을 수 있는 레코드가 있다면 랜덤 추출
            if (candidates.Count > 0)
            {
                int randomIndex = Random.Range(0, candidates.Count);
                newOptions.Add(candidates[randomIndex]);
                candidates.RemoveAt(randomIndex); // 이번 셔플 내 중복 방지
            }
            else
            {
                // [핵심] 진짜로 모든 데이터를 다 소진했을 때만 빈 레코드 등장
                var emptyRecord = AppManager.Instance?.GetEmptyRecord();
                if (emptyRecord != null)
                {
                    newOptions.Add(emptyRecord);
                }
            }
        }

        // 성공적으로 리롤이 수행된 경우에만 카운트 차감
        rerollCount--;

        // 4. 데이터 갱신 및 UI 트리거
        CurrentOptions = newOptions;
        AppManager.Instance?.TriggerRecordUI(CurrentOptions);
    }
    public void OnReturendStageSelectScene()
    {

    }

    public void SaveIfDirty()
    {
        if (isDirty == false)
            return;

        RecordSaveData save = new(); 
        foreach(RecordData data in possesRecords)
        {
            save.recordIDs.Add(data.id);
        }

        save.isReceived = isReceived;
        SaveManager.SaveRecordData(save);
        isDirty = true;

    }
}
