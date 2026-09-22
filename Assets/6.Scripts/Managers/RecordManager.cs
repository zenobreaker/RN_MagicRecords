using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class RecordManager : MonoBehaviour
{
    [SerializeField] private List<SO_RecordData> records = new List<SO_RecordData>();

    public List<RecordData> CurrentOptions { get; private set; } = new();

    public List<RecordData> SelectedRecords { get; private set; } = new();
    public event System.Action OnCostPaidSuccess; // 코스트 지불 성공 알림 이벤트 

    private Dictionary<int, SO_RecordData> recordsDict = new();
    private int generateCount = 3;
    private int rerollCount;
    public int RerollCount => rerollCount;
    private int maxRerollCount = 3;

    private int max_selectCount = 1;
    private RecordInventory recordInventory = new();
    private RecordInventory transferInventory = new();
    private bool isReceived = false;
    private bool filterStartingSkills;

    private bool isDirty = false;

    public void SetReceiveRecordFlag() => isReceived = true;

    public void ResetRecordFlowData()
    {
        isReceived = false;
        filterStartingSkills = false;
        rerollCount = maxRerollCount;
        recordInventory.ClearAll();
        SelectedRecords.Clear();
        CurrentOptions.Clear();
    }


    public void OnInit()
    {
        ResetRecordFlowData();

        foreach (var record in records)
            recordsDict.Add(record.id, record);

        // 인벤토리에 변동이 생길 때마다 자동으로 세이브 플레그 ON 
        recordInventory.OnInventoryChanged += (inv) => { isDirty = true; };
        transferInventory.OnInventoryChanged += (inv) => { isDirty = true; };

        RecordSaveListData saveData = SaveManager.LoadRecordData();
        if (saveData != null)
            ApplySavedRecords(saveData);
    }

    private void ApplySavedRecords(RecordSaveListData saveData)
    {
        if (saveData == null) return;

        // 혹시 모를 찌꺼기 데이터 초기화
        recordInventory.ClearAll();
        transferInventory.ClearAll();


        // 소지 중인 레코드 복구 
        foreach (RecordSaveData savedInfo in saveData.recordIDs)
        {
            recordInventory.AddRecord(RestoreRecord(savedInfo));
        }

        // 다음 회차로 인계된 레코드 복구
        foreach (RecordSaveData transferInfo in saveData.transferedrecordIDs)
        {
            transferInventory.AddRecord(RestoreRecord(transferInfo));
        }

        isReceived = saveData.isReceived;
    }

    private RecordData RestoreRecord(RecordSaveData saved)
    {
        if (saved == null) return null;
        var record = saved.recordID == RecordDataBase.EmptyRecordId ? GetEmptyRecord() :
            recordsDict.TryGetValue(saved.recordID, out var template) ? template.GetRecordData() : null;
        if (record != null) record.uniqueID = saved.uniqueID;
        return record;
    }

    public List<RecordData> GetPossesRecord() => recordInventory.Records.ToList();

    // Inventory restoration happens during AppManager.Awake, before PassiveSystem.OnInit.
    // Rebuild effects afterwards so resumed records keep their gameplay effects too.
    public void RestoreOwnedPassives()
    {
        var passives = AppManager.Instance?.GetPassiveSystem();
        if (passives == null) return;
        passives.ResetExplorePassives();
        foreach (var record in recordInventory.Records)
            if (record.id != RecordDataBase.EmptyRecordId)
                passives.Add(Constants.GLOBAL_RECORD_JOB_ID, GetRecordPassive(record.id));
    }
    public List<RecordData> GetTransferedRecordIDs() => transferInventory.Records.ToList();

    // 특정 레코드를 다음 회차에 사용할 수 있도록 보내는 함수 
    public void SetTranferRecord(RecordData target)
    {
        if (target == null) return;

        var find = recordInventory.GetRecord(target.uniqueID);
        if (find == null) return; // 없는 대상은 실패 TODO: 토스트 문자 띄우기 

        recordInventory.RemoveRecord(find);
        transferInventory.AddRecord(find);
        SaveIfDirty();
    }


    private List<RecordData> GetAllEnrichedRecordData()
    {
        if (AppManager.Instance == null) return null;

        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null) return null;

        List<RecordData> rawRecords = db.GetAllRecordData();
        List<RecordData> enrichedRecords = new List<RecordData>();

        if (rawRecords != null)
        {
            foreach (var raw in rawRecords)
            {
                // DB에서 가져온 ID를 기반으로, SO가 가진 아이콘/정보를 씌운 객체를 생성
                if (recordsDict.TryGetValue(raw.id, out SO_RecordData soData))
                {
                    enrichedRecords.Add(soData.GetRecordData());
                }
            }
        }
        return enrichedRecords;
    }


    // 💡 1. 탐사 시작 시 호출 (중복 방지 플래그 검사 O)
    public void GenerateChapterStartRecords(int count = 3, bool canReroll = true)
    {
        // 이미 이번 스테이지에서 기본 보상을 받았다면 무시
        if (isReceived || AppManager.Instance == null) return;
        var explore = AppManager.Instance.GetExploreManager();
        if (explore == null || !explore.InitialRecordRewardPending || explore.Chapter != 1 || explore.MapNodeID != 0) return;

        GenerateDraftRecords(count, canReroll, true);
    }

    // 💡 2. 이벤트 보상 시 호출 (중복 방지 플래그 검사 X - 무조건 지급)
    public void GenerateRewardRecords(int count = 3, bool canReroll = true)
    {
        if (AppManager.Instance == null) return;

        // 플래그를 무시하고 즉시 레코드 선택창을 띄웁니다.
        GenerateDraftRecords(count, canReroll);
    }

    // 💡 3. 실제 레코드를 뽑고 UI를 띄우는 핵심 내부 로직 (은닉화)
    private void GenerateDraftRecords(int count, bool canReroll, bool startingSkillsOnly = false)
    {
        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null) return;

        filterStartingSkills = startingSkillsOnly;
        generateCount = Mathf.Max(1, count);
        SelectedRecords = new List<RecordData>();

        // 1. 전체 데이터에서 랜덤 추출 
        List<RecordData> allRecord = GetAllEnrichedRecordData();

        // 2. 현재 가지고 있는 레코드들이 있다면 제외 
        if (recordInventory.Records.Count > 0)
        {
            allRecord.RemoveAll(data => recordInventory.Records.Any(last => last.id == data.id));
        }

        CurrentOptions = FilterDraftCandidates(allRecord, startingSkillsOnly)
            .OrderBy(_ => Random.value).Take(generateCount).ToList();
        while (CurrentOptions.Count < generateCount)
        {
            var empty = db.GetEmptyRecord();
            if (empty == null) break;
            CurrentOptions.Add(empty);
        }

        // 5. AppManager를 통해 UI 오픈 이벤트 발행
        PauseManager.RequestPause();
        UIManager.Instance.OpenRecordSelectPopUp(CurrentOptions, canReroll, RecordUIMode.DRAFT);
        if (startingSkillsOnly) AppManager.Instance.GetExploreManager()?.ConsumeInitialRecordReward();
    }

    private List<RecordData> FilterDraftCandidates(IEnumerable<RecordData> source, bool startingOnly)
    {
        var setup = AppManager.Instance?.GetExploreManager()?.CurrentSetupData;
        int job = setup?.SelectedClassId ?? 1;
        var equipped = new HashSet<int>(AppManager.Instance?.GetEquippedActiveSkillIDListByCharID(
            setup?.SelectedCharacterId ?? 1) ?? new List<int>());
        return (source ?? Enumerable.Empty<RecordData>()).Where(r => r != null &&
            (r.targetFilter == TargetFilterType.ALL || r.IsTarget(job)) &&
            (!startingOnly || IsStartingRecordEligible(r, equipped))).ToList();
    }

    public bool IsStartingRecordEligible(RecordData record, ISet<int> equipped)
    {
        if (record == null || equipped == null) return false;
        var targets = new HashSet<int>((record.Skills ?? new List<RecordSkillData>())
            .Where(s => s != null && s.SkillID > 0).Select(s => s.SkillID));
        if (!recordsDict.TryGetValue(record.id, out var so) || so == null)
            return targets.Count == 0 || targets.Overlaps(equipped);
        var passive = SkillTreeManager.Instance?.GetSkillRuntimeData(
            so.targetFilter == TargetFilterType.ALL ? 0 : (int)so.targetFilter,
            so.linkedPassiveSkillID)?.template as SO_PassiveSkillData;
        if (passive?.Modules != null)
            foreach (var module in passive.Modules)
                if (module != null && module.TargetSkillID > 0) targets.Add(module.TargetSkillID);
        // Stats and general passives have no particular skill target.
        // An unresolved skill modifier must not bypass the equipment filter.
        if (targets.Count == 0 && passive == null && so.linkedPassiveSkillID > 0 &&
            (record.type == RecordType.AUGMENT || record.type == RecordType.MODIFY)) return false;
        return targets.Count == 0 || targets.Overlaps(equipped);
    }

    public RecordData GetEmptyRecord()
    {
        Debug.Assert(AppManager.Instance != null);

        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null) return null;

        return db.GetEmptyRecord();
    }

    public List<RecordData> GetNormalRecordDatas()
    {
        return GetUnpossessedRecordDatas(RecordRarity.NORMAL);
    }
    public List<RecordData> GetRareRecordDatas()
    {
        return GetUnpossessedRecordDatas(RecordRarity.RARE);
    }
    public List<RecordData> GetUniqueRecordDatas()
    {
        return GetUnpossessedRecordDatas(RecordRarity.UNIQUE);
    }
    public List<RecordData> GetLengdaryRecordDatas()
    {
        return GetUnpossessedRecordDatas(RecordRarity.LEGENDARY);
    }
    public List<RecordData> GetMythRecordDatas()
    {
        return GetUnpossessedRecordDatas(RecordRarity.MYTH);
    }

    private List<RecordData> GetRecordDatas(RecordRarity rarity)
    {
        Debug.Assert(AppManager.Instance != null);

        List<RecordData> records;
        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null) return null;

        return records = db.GetRecordDatas(rarity);
    }


    // 💡 특정 등급의 레코드 중, '아직 획득하지 않은(미보유)' 레코드만 반환하는 함수
    private List<RecordData> GetUnpossessedRecordDatas(RecordRarity rarity)
    {
        // 1. 데이터베이스에서 해당 등급의 모든 레코드를 가져옵니다.
        List<RecordData> allRecordsOfRarity = GetRecordDatas(rarity);

        // 예외 처리: 해당 등급의 레코드가 아예 없다면 빈 리스트 반환
        if (allRecordsOfRarity == null || allRecordsOfRarity.Count == 0)
        {
            return new List<RecordData>();
        }

        // 2. LINQ를 사용하여 내가 가진 레코드(recordInventory)와 ID를 대조해 걸러냅니다.
        List<RecordData> unpossessedRecords = allRecordsOfRarity
            .Where(data => !recordInventory.Records.Any(owned => owned.id == data.id))
            .ToList();

        return unpossessedRecords;
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
            Debug.Log($"Record Select : {data.recordName}");
        }
        else
            SelectedRecords.Remove(data);

    }

    public PassiveSkill GetRecordPassive(int id)
    {
        if (recordsDict.ContainsKey(id))
            return recordsDict[id].CreatePassiveSkill();

        return null;
    }

    public bool IsSelectedRecord(RecordData selectedData)
    {
        return SelectedRecords.Contains(selectedData);
    }

    public void AddRecord(RecordData recordData)
        => GrantRecord(recordData);

    // Return the actual item so result UI and passive registration cannot use a duplicate's original data.
    public RecordData GrantRecord(RecordData recordData)
    {
        var granted = recordInventory.AddRecord(recordData);
        if (granted == null) return null;
        isReceived = true;
        isDirty = true;
        SaveIfDirty();
        return granted;
    }

    // Same SO enrichment, job eligibility and unowned rule as the existing draft.
    public List<RecordData> GetShopCandidates()
    {
        return FilterDraftCandidates(GetAllEnrichedRecordData(), false)
            .Where(r => r.id > 0 && !recordInventory.Records.Any(owned => owned.id == r.id))
            .GroupBy(r => r.id).Select(g => g.First()).ToList();
    }

    public RecordData GetShopRecord(int id) => recordsDict.TryGetValue(id, out var record) && record != null
        ? record.GetRecordData() : null;

    public void RemoveTransferedRecord(RecordData target)
    {
        transferInventory.RemoveRecord(target);
        SaveIfDirty();
    }

    public List<RecordData> RerollAllCurrentRecords()
    {
        DataBaseManager db = DataBaseManager.Instance;
        if (db == null) return null;

        // 1. 후보군 생성 (전체 - 이미 영구 보유 중인 것들)
        // 현재 떠 있는 것(CurrentOptions)은 제외하지 않습니다. 
        // 그래야 리롤 시점에 다시 나올 기회를 얻어 '빈 레코드'가 성급하게 뜨지 않습니다.
        var allRecord = GetAllEnrichedRecordData();
        var candidates = FilterDraftCandidates(allRecord, filterStartingSkills)
            .Where(data => !recordInventory.Records.Any(p => p.id == data.id))
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
                var emptyRecord = db.GetEmptyRecord();
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

        return newOptions;
    }

    //public void RerollAllCurrentRecords()
    //{
    //    if (rerollCount <= 0)
    //    {
    //        // 알림 메세지 UI 호출
    //        UIManager.Instance.SafeInvoke(v => v.ShowToast(
    //            $"리롤 횟수가 부족합니다!!"));
    //        return;
    //    }

    //    DataBaseManager db = DataBaseManager.Instance;
    //    if (db == null) return;

    //    // 1. 후보군 생성 (전체 - 이미 영구 보유 중인 것들)
    //    // 현재 떠 있는 것(CurrentOptions)은 제외하지 않습니다. 
    //    // 그래야 리롤 시점에 다시 나올 기회를 얻어 '빈 레코드'가 성급하게 뜨지 않습니다.
    //    var allRecord = GetAllEnrichedRecordData();
    //    var candidates = allRecord
    //        .Where(data => !recordInventory.Records.Any(p => p.id == data.id))
    //        .ToList();

    //    // 2. 현재 잠금(Lock)된 데이터들은 후보군에서 즉시 제거하여 중복 생성 방지
    //    foreach (var opt in CurrentOptions)
    //    {
    //        if (opt.isLocked)
    //        {
    //            candidates.RemoveAll(c => c.id == opt.id);
    //        }
    //    }

    //    List<RecordData> newOptions = new List<RecordData>();

    //    // 3. 새로운 옵션 구성
    //    for (int i = 0; i < generateCount; i++)
    //    {
    //        // 현재 인덱스가 잠금 상태라면 그대로 유지
    //        if (i < CurrentOptions.Count && CurrentOptions[i].isLocked)
    //        {
    //            newOptions.Add(CurrentOptions[i]);
    //            continue;
    //        }

    //        // 뽑을 수 있는 레코드가 있다면 랜덤 추출
    //        if (candidates.Count > 0)
    //        {
    //            int randomIndex = Random.Range(0, candidates.Count);
    //            newOptions.Add(candidates[randomIndex]);
    //            candidates.RemoveAt(randomIndex); // 이번 셔플 내 중복 방지
    //        }
    //        else
    //        {
    //            // [핵심] 진짜로 모든 데이터를 다 소진했을 때만 빈 레코드 등장
    //            var emptyRecord = db.GetEmptyRecord();
    //            if (emptyRecord != null)
    //            {
    //                newOptions.Add(emptyRecord);
    //            }
    //        }
    //    }

    //    // 성공적으로 리롤이 수행된 경우에만 카운트 차감
    //    rerollCount--;

    //    // 4. 데이터 갱신 및 UI 트리거
    //    CurrentOptions = newOptions;

    //    UIManager.Instance.SafeInvoke(v => v.RefreshRecordSelectPopUp(CurrentOptions)); 
    //}


    // 레코드를 비용(Cost)으로 지불하고 완료하는 함수
    public bool OnCompleteCostDiscard()
    {
        if (SelectedRecords == null || SelectedRecords.Count <= 0) return false;

        foreach (RecordData data in SelectedRecords)
        {
            // 1. 인벤토리에서 해당 레코드 삭제 (소비)
            recordInventory.RemoveRecord(data);

            // (필요하다면 패시브 시스템에서도 제거하는 로직 추가)
             var ps = AppManager.Instance.GetPassiveSystem();
             ps?.Remove(9999, GetRecordPassive(data.id));

            Debug.Log($"[{data.recordName}] 레코드를 비용으로 소모했습니다.");
        }

        SelectedRecords.Clear();
        SaveIfDirty();
        PauseManager.RequestResume();

        return true;
    }

    public void TriggerCostPaidEvent()
    {
        OnCostPaidSuccess?.Invoke();
    }


    public bool OnCompleteSelctRecords()
    {
        List<RecordData> selectedRecords = SelectedRecords;
        if (selectedRecords.Count <= 0)
            return false;

        var ps = AppManager.Instance.GetPassiveSystem();
        if (ps == null) return false;

        foreach (RecordData data in selectedRecords)
        {
            var granted = GrantRecord(data);
            if (granted != null && granted.id != RecordDataBase.EmptyRecordId)
                ps.Add(Constants.GLOBAL_RECORD_JOB_ID, GetRecordPassive(granted.id));
        }

        SelectedRecords.Clear();
        PauseManager.RequestResume();
        return true;
    }

    public bool OnCompleteArchiveRecord()
    {
        List<RecordData> selectedRecords = SelectedRecords;
        if (selectedRecords == null || selectedRecords.Count <= 0) return false;

        foreach (RecordData data in selectedRecords)
        {
            SetTranferRecord(data);
            Debug.Log($"[{data.recordName}] 레코드가 아카이브에 저장되었습니다!");
        }

        // 처리가 끝났으니 선택 리스트 비워주기
        SelectedRecords.Clear();
        PauseManager.RequestResume();
        return true;
    }

    // 이전 회차에서 가져온 레코드를 수령
    public bool OnCompleteInheritReward()
    {
        List<RecordData> selectedRecords = SelectedRecords;
        if (selectedRecords == null || selectedRecords.Count <= 0) return false;

        var ps = AppManager.Instance.GetPassiveSystem();
        if (ps == null) return false;

        foreach (RecordData data in selectedRecords)
        {
            var granted = GrantRecord(data);
            if (granted == null) continue;
            if (granted.id != RecordDataBase.EmptyRecordId)
                ps.Add(Constants.GLOBAL_RECORD_JOB_ID, GetRecordPassive(granted.id));
            RemoveTransferedRecord(data);
        }


        SelectedRecords.Clear();
        PauseManager.RequestResume();
        return true;
    }

    public void SaveIfDirty()
    {
        if (isDirty == false) return;

        RecordSaveListData save = new RecordSaveListData();

        // 1. 소지 중인 레코드 저장
        foreach (RecordData data in recordInventory.Records)
        {
            save.recordIDs.Add(new RecordSaveData()
            {
                recordID = data.id,
                uniqueID = data.uniqueID
            });
        }

        // 2. 인계된 레코드 저장
        foreach (RecordData data in transferInventory.Records)
        {
            save.transferedrecordIDs.Add(new RecordSaveData()
            {
                recordID = data.id,
                uniqueID = data.uniqueID
            });
        }

        save.isReceived = isReceived;
        SaveManager.SaveRecordData(save);
        isDirty = false;
    }
}
