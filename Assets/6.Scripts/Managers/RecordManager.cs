using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class RecordManager : MonoBehaviour
{
    [SerializeField] private List<SO_RecordData> records = new List<SO_RecordData>();

    public List<RecordData> CurrentOptions
    {
        get;
        private set;
    } = new();

    public List<RecordData> SelectedRecords
    {
        get;
        private set;
    } = new();
    public event System.Action OnCostPaidSuccess; // 코스트 지불 성공 알림 이벤트

    private Dictionary<int, SO_RecordData> recordsDict = new();
    private readonly Dictionary<string, PassiveSkill> recordPassives = new();
    private int generateCount = 3;
    private int rerollCount;
    public int RerollCount
    {
        get
        {
            return rerollCount;
        }
    }
    private int maxRerollCount = 3;

    private int max_selectCount = 1;
    private RecordInventory recordInventory = new();
    private RecordInventory transferInventory = new();
    private bool isReceived = false;
    private bool filterStartingSkills;

    private bool isDirty = false;
    private const float HighestRarityBonusPerMiss = 0.002f;
    private const int MaximumRarityMissCount = 500;
    private int highestRarityMissCount;

    public float HighestRarityBonus => highestRarityMissCount * HighestRarityBonusPerMiss;

    /// <summary>보정 누적 횟수는 저장 가능 범위로 제한하고 변경 시 저장 대상으로 표시합니다.</summary>
    private void SetHighestRarityMissCount(int value)
    {
        int next = Mathf.Clamp(value, 0, MaximumRarityMissCount);
        if (highestRarityMissCount == next)
        {
            return;
        }
        highestRarityMissCount = next;
        isDirty = true;
    }

    public void SetReceiveRecordFlag()
    {
        isReceived = true;
    }

    public void ResetRecordFlowData()
    {
        foreach (var passive in recordPassives.Values)
        {
            AppManager.Instance?.GetPassiveSystem()?.Remove(Constants.GLOBAL_RECORD_JOB_ID, passive);
        }

        recordPassives.Clear();
        isReceived = false;
        filterStartingSkills = false;
        SetHighestRarityMissCount(0);
        rerollCount = maxRerollCount;
        recordInventory.ClearAll();
        SelectedRecords.Clear();
        CurrentOptions.Clear();
    }


    public void OnInit()
    {
        ResetRecordFlowData();

        recordsDict.Clear();
        foreach (var record in records)
        {
            if (record == null)
            {
                Debug.LogWarning("[RecordManager] 레코드 목록의 비어 있거나 삭제된 에셋을 건너뜁니다.", this);
                continue;
            }
            if (recordsDict.ContainsKey(record.id))
            {
                Debug.LogWarning($"[RecordManager] 중복 레코드 ID {record.id}를 건너뜁니다.", this);
                continue;
            }
            recordsDict.Add(record.id, record);
        }

        // 인벤토리에 변동이 생길 때마다 자동으로 세이브 플레그 ON
        recordInventory.OnInventoryChanged += (inv) =>
        {
            isDirty = true;

        };
        transferInventory.OnInventoryChanged += (inv) =>
        {
            isDirty = true;

        };

        RecordSaveListData saveData = SaveManager.LoadRecordData();
        if (saveData != null)
        {
            ApplySavedRecords(saveData);
        }
    }

    private void ApplySavedRecords(RecordSaveListData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        // 혹시 모를 찌꺼기 데이터 초기화
        recordInventory.ClearAll();
        transferInventory.ClearAll();


        // 소지 중인 레코드 복구
        foreach (RecordSaveData savedInfo in saveData.recordIDs)
        {
            var restored = RestoreRecord(savedInfo, saveData.catalogVersion);
            if (IsPassiveLevelReward(restored) == false)
            {
                recordInventory.AddRecord(restored);
            }
        }

        // 다음 회차로 인계된 레코드 복구
        foreach (RecordSaveData transferInfo in saveData.transferedrecordIDs)
        {
            transferInventory.AddRecord(RestoreRecord(transferInfo, saveData.catalogVersion));
        }

        isReceived = saveData.isReceived;
        highestRarityMissCount = Mathf.Clamp(saveData.highestRarityMissCount, 0, MaximumRarityMissCount);
    }

    private RecordData RestoreRecord(RecordSaveData saved, int catalogVersion = 0)
    {
        if (saved == null)
        {
            return null;
        }

        // 체이서 저장 카드는 대체된 익스큐터로 복원합니다.
        int recordId = saved.recordID == 100101 ? 100103 : saved.recordID;
        // 번호 정리 전 저장 데이터만 변환하여 재저장 후 중복 이동을 방지합니다.
        if (catalogVersion < 1)
        {
            if (recordId == 6)
            {
                recordId = 2108;
            }
            else if (recordId >= 7 && recordId <= 10)
            {
                recordId -= 1;
            }
            else if (recordId == 11)
            {
                recordId = 2100;
            }
            else if (recordId >= 12 && recordId <= 49)
            {
                recordId -= 2;
            }
        }
        var record = recordId == RecordDataBase.EmptyRecordId ? GetEmptyRecord() :
                    recordsDict.TryGetValue(recordId, out var template) ? template.GetRecordData() : null;
        if (record != null)
        {
            record.uniqueID = saved.uniqueID;
            record.ownedLevels = Mathf.Max(0, saved.ownedLevels);
        }

        return record;
    }

    public List<RecordData> GetPossesRecord()
    {
        return recordInventory.Records.ToList();
    }

    // Inventory restoration happens during AppManager.Awake, before PassiveSystem.OnInit.
    // Rebuild effects afterwards so resumed records keep their gameplay effects too.
    public void RestoreOwnedPassives()
    {
        var passives = AppManager.Instance?.GetPassiveSystem();
        if (passives == null)
        {
            return;
        }

        passives.ResetRecordPassives();
        recordPassives.Clear();
        foreach (var record in recordInventory.Records)
        {
            if (record.id != RecordDataBase.EmptyRecordId && IsRuntimePassiveRecord(record) == false)
            {
                RegisterRecordPassive(record);
            }
        }
    }
    public List<RecordData> GetTransferedRecordIDs()
    {
        return transferInventory.Records.ToList();
    }

    // 특정 레코드를 다음 회차에 사용할 수 있도록 보내는 함수
    public void SetTranferRecord(RecordData target)
    {
        if (target == null)
        {
            return;
        }

        var find = recordInventory.GetRecord(target.uniqueID);
        if (find == null)
        {
            return; // 없는 대상은 실패 TODO: 토스트 문자 띄우기
        }

        RemoveOwnedRecord(find);
        transferInventory.AddRecord(find);
        SaveIfDirty();
    }


    private List<RecordData> GetAllEnrichedRecordData()
    {
        if (AppManager.Instance == null)
        {
            return null;
        }

        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null)
        {
            return null;
        }

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
        // 에셋으로 추가한 기본 패시브 강화 레코드도 JSON 재생성 없이 추첨에 포함합니다.
        foreach (var template in recordsDict.Values)
        {
            if (template != null && template.isImplemented && template.type == RecordType.PASSIVE && enrichedRecords.Any(r => r.id == template.id) == false)
            {
                enrichedRecords.Add(template.GetRecordData());
            }
        }

        return enrichedRecords;
    }


    // 💡 1. 탐사 시작 시 호출 (중복 방지 플래그 검사 O)
    public void GenerateChapterStartRecords(int count = 3, bool canReroll = true)
    {
        // 이미 이번 스테이지에서 기본 보상을 받았다면 무시
        if (isReceived || AppManager.Instance == null)
        {
            return;
        }

        var explore = AppManager.Instance.GetExploreManager();
        if (explore == null || explore.InitialRecordRewardPending == false || explore.Chapter != 1 || explore.MapNodeID != 0)
        {
            return;
        }

        GenerateDraftRecords(count, canReroll, true);
    }

    // 💡 2. 이벤트 보상 시 호출 (중복 방지 플래그 검사 X - 무조건 지급)
    public void GenerateRewardRecords(int count = 3, bool canReroll = true)
    {
        if (AppManager.Instance == null)
        {
            return;
        }

        // 플래그를 무시하고 즉시 레코드 선택창을 띄웁니다.
        GenerateDraftRecords(count, canReroll);
    }

    // 💡 3. 실제 레코드를 뽑고 UI를 띄우는 핵심 내부 로직 (은닉화)
    private void GenerateDraftRecords(int count, bool canReroll, bool startingSkillsOnly = false)
    {
        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null)
        {
            return;
        }

        filterStartingSkills = startingSkillsOnly;
        generateCount = Mathf.Max(1, count);
        SelectedRecords = new List<RecordData>();

        // 1. 전체 데이터에서 랜덤 추출
        List<RecordData> allRecord = GetAllEnrichedRecordData();

        // 2. 현재 가지고 있는 레코드들이 있다면 제외
        if (recordInventory.Records.Count > 0)
        {
            allRecord.RemoveAll(data => (IsRewardCandidate(data) == false));
        }

        var candidates = FilterDraftCandidates(allRecord, startingSkillsOnly);
        CurrentOptions = new List<RecordData>();
        for (int i = 0; i < generateCount && candidates.Count > 0; i++)
        {
            var selected = PickRandomRewardRecord(candidates);
            if (selected == null)
            {
                break;
            }
            CurrentOptions.Add(selected);
            candidates.RemoveAll(candidate => candidate.id == selected.id);
        }
        if (CurrentOptions.Count == 0)
        {
            var empty = db.GetEmptyRecord();
            if (empty != null)
            {
                CurrentOptions.Add(empty);
            }
        }

        SaveIfDirty();
        // 5. AppManager를 통해 UI 오픈 이벤트 발행
        PauseManager.RequestPause();
        UIManager.Instance.OpenRecordSelectPopUp(CurrentOptions, canReroll, RecordUIMode.DRAFT);
        if (startingSkillsOnly)
        {
            AppManager.Instance.GetExploreManager()?.ConsumeInitialRecordReward();
        }
    }

    private List<RecordData> FilterDraftCandidates(IEnumerable<RecordData> source, bool startingOnly)
    {
        var setup = AppManager.Instance?.GetExploreManager()?.CurrentSetupData;
        int job = setup?.SelectedClassId ?? 1;
        var equipped = new HashSet<int>(AppManager.Instance?.GetEquippedActiveSkillIDListByCharID(
            setup?.SelectedCharacterId ?? 1) ?? new List<int>());
        return (source ?? Enumerable.Empty<RecordData>()).Where(r => r != null &&
            (r.targetFilter == TargetFilterType.ALL || r.IsTarget(job)) &&
            IsRewardCandidate(r) &&
            (startingOnly == false || IsStartingRecordEligible(r, equipped))).ToList();
    }

    public bool IsStartingRecordEligible(RecordData record, ISet<int> equipped)
    {
        if (record == null || equipped == null)
        {
            return false;
        }
        if (record.targetFilter == TargetFilterType.ALL && record.type != RecordType.PASSIVE)
        {
            return false;
        }

        var targets = new HashSet<int>((record.Skills ?? new List<RecordSkillData>())
                    .Where(s => s != null && s.SkillID > 0).Select(s => s.SkillID));
        if (recordsDict.TryGetValue(record.id, out var so) == false || so == null)
        {
            return targets.Count > 0 ? targets.Overlaps(equipped) : record.type == RecordType.PASSIVE;
        }

        var passive = AppManager.Instance?.GetExploreManager()?.ResolvePassiveTemplate(
            so.linkedPassiveSkillID, so.targetFilter == TargetFilterType.ALL ? 0 : (int)so.targetFilter);
        if (passive?.Modules != null)
        {
            foreach (var module in passive.Modules)
            {
                if (module != null && module.TargetSkillID > 0)
                {
                    targets.Add(module.TargetSkillID);
                }
            }
        }
        // 특정 액티브 스킬을 대상으로 하는 레코드는 타입과 무관하게 장착 여부를 검사합니다.
        return targets.Count > 0 ? targets.Overlaps(equipped) : record.type == RecordType.PASSIVE;
    }

    /// <summary>등급별 가중치에 최고 등급 미등장 보정을 적용하고 해당 등급 안에서 균등 추첨합니다.</summary>
    public RecordData PickRandomRewardRecord(List<RecordData> candidates)
    {
        var selected = PickWeightedRecord(candidates, highestRarityMissCount, out bool hasHighest, out var highestRarity);
        SetHighestRarityMissCount(hasHighest && selected != null && selected.rarity != highestRarity
            ? highestRarityMissCount + 1 : 0);
        return selected;
    }

    /// <summary>전달받은 보정으로 추첨만 하고 탐사 보상 및 상점의 상태를 변경하지 않습니다.</summary>
    internal static RecordData PickWeightedRecord(List<RecordData> candidates, int misses, out bool hasHighest,
        out RecordRarity highestRarity)
    {
        var rarityTable = SO_GameSettings.Instance.RecordRarityTable;
        var groups = (candidates ?? new List<RecordData>())
            .Where(record => record != null && rarityTable.GetWeight(record.rarity) > 0f)
            .GroupBy(record => record.rarity)
            .OrderBy(group => group.Key)
            .ToList();
        bool configured = rarityTable.TryGetHighestRarity(out var highest);
        highestRarity = highest;
        hasHighest = configured && groups.Any(group => group.Key == highest);
        float totalWeight = groups.Sum(group => rarityTable.GetWeight(group.Key));
        if (totalWeight <= 0f)
        {
            return null;
        }

        float highestBaseChance = hasHighest ? rarityTable.GetWeight(highestRarity) / totalWeight : 0f;
        float highestChance = hasHighest ? Mathf.Clamp01(highestBaseChance + Mathf.Clamp(misses, 0, MaximumRarityMissCount) * HighestRarityBonusPerMiss) : 0f;
        float lowerScale = hasHighest && highestBaseChance < 1f
            ? (1f - highestChance) / (1f - highestBaseChance) : 1f;
        float roll = Random.value;
        RecordRarity selectedRarity = groups[groups.Count - 1].Key;
        foreach (var group in groups)
        {
            float chance = group.Key == highestRarity && hasHighest
                ? highestChance : rarityTable.GetWeight(group.Key) / totalWeight * lowerScale;
            if (chance <= 0f)
            {
                continue;
            }
            selectedRarity = group.Key;
            roll -= chance;
            if (roll < 0f)
            {
                break;
            }
        }

        var recordsOfRarity = groups.First(group => group.Key == selectedRarity).ToList();
        return recordsOfRarity[Random.Range(0, recordsOfRarity.Count)];
    }

    public RecordData GetEmptyRecord()
    {
        Debug.Assert(AppManager.Instance != null);

        DataBaseManager db = AppManager.Instance.GetDataBaseManager();
        if (db == null)
        {
            return null;
        }

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
        return GetRewardCandidates().Where(r => r.rarity == rarity).ToList();
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
            .Where(IsRewardCandidate)
            .ToList();

        return unpossessedRecords;
    }

    public void SelectedRecord(RecordData data)
    {
        if (data == null)
        {
            return;
        }

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
        {
            SelectedRecords.Remove(data);
        }
    }

    public PassiveSkill GetRecordPassive(int id)
    {
        if (recordsDict.ContainsKey(id))
        {
            return recordsDict[id].CreatePassiveSkill();
        }

        return null;
    }

    public bool IsSelectedRecord(RecordData selectedData)
    {
        return SelectedRecords.Contains(selectedData);
    }

    public void AddRecord(RecordData recordData)
    {
        GrantRecord(recordData);
    }

    // AUGMENT/MODIFY도 PassiveSkill로 구현되므로 연결 데이터만으로 강화형을 판정하지 않습니다.
    private bool IsRuntimePassiveRecord(RecordData record)
    {
        return record != null && record.type == RecordType.PASSIVE &&
        recordsDict.TryGetValue(record.id, out var template) && template != null && template.type == RecordType.PASSIVE &&
        template.linkedPassiveSkillID > 0;
    }

    /// <summary>기본 패시브 강화 보상은 레코드 인벤토리에 별도 카드를 만들지 않습니다.</summary>
    public bool IsPassiveLevelReward(RecordData record)
    {
        return IsRuntimePassiveRecord(record) && recordsDict[record.id].levelsPassiveOnly;
    }

    private string PassiveUpgradeIdentity(RecordData record, int job)
    {
        return $"passive:{job}:{recordsDict[record.id].linkedPassiveSkillID}";
    }

    /// <summary>실물 카드와 현재 보유한 패시브 중 다음 레벨이 가능한 대상을 반환합니다.</summary>
    public List<RecordData> GetUpgradeableRecords()
    {
        var result = recordInventory.Records.Where(record => TryGetUpgradeLevel(record, out _, out _)).ToList();
        var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
        var setup = explore.SafeInvoke(value => value.CurrentSetupData);
        if (setup == null)
        {
            return result;
        }
        foreach (var definition in recordsDict.Values)
        {
            if (definition == null || definition.isImplemented == false || definition.levelsPassiveOnly == false ||
                (definition.targetFilter != TargetFilterType.ALL && (int)definition.targetFilter != setup.SelectedClassId))
            {
                continue;
            }
            var record = definition.GetRecordData();
            record.uniqueID = PassiveUpgradeIdentity(record, setup.SelectedClassId);
            if (TryGetUpgradeLevel(record, out int level, out _))
            {
                record.ownedLevels = level;
                result.Add(record);
            }
        }
        return result;
    }

    private SO_PassiveSkillData ResolveRecordPassiveTemplate(RecordData record)
    {
        if (IsRuntimePassiveRecord(record) == false)
        {
            return null;
        }

        var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
        return explore.SafeInvoke(manager => manager.ResolvePassiveTemplate(recordsDict[record.id].linkedPassiveSkillID));
    }

    public bool IsRewardCandidate(RecordData record)
    {
        if (record == null || record.id == RecordDataBase.EmptyRecordId ||
            HasConflictingRecord(record))
        {
            return false;
        }

        if (recordsDict.TryGetValue(record.id, out var definition) && definition.isImplemented == false)
        {
            return false;
        }

        if (IsRuntimePassiveRecord(record))
        {
            var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
            return explore.SafeInvoke(manager => manager.CanUpgradePassive(ResolveRecordPassiveTemplate(record)));
        }
        return recordInventory.Records.Any(owned => owned.id == record.id) == false;
    }

    private bool HasConflictingRecord(RecordData record)
    {
        if (recordsDict.TryGetValue(record.id, out var candidate) == false || candidate == null ||
            string.IsNullOrEmpty(candidate.mutualExclusionGroup))
        {
            return false;
        }

        return recordInventory.Records.Any(owned => owned.id != record.id &&
                    recordsDict.TryGetValue(owned.id, out var template) && template != null &&
                    template.mutualExclusionGroup == candidate.mutualExclusionGroup);
    }

    private void RegisterRecordPassive(RecordData record)
    {
        var passive = GetRecordPassive(record.id);
        if (passive == null)
        {
            return;
        }

        if (record.ownedLevels > 1)
        {
            passive.OnChangedLevel(record.ownedLevels);
            passive.SetLevel(record.ownedLevels);
        }
        recordPassives[record.uniqueID] = passive;
        AppManager.Instance?.GetPassiveSystem()?.Add(Constants.GLOBAL_RECORD_JOB_ID, passive);
    }

    /// <summary>실제로 소유한 카드의 효과가 다음 레벨을 지원할 때만 강화 대상으로 반환합니다.</summary>
    public bool TryGetUpgradeLevel(RecordData record, out int level, out int maximum)
    {
        level = 0;
        maximum = 0;
        if (record == null || string.IsNullOrEmpty(record.uniqueID) ||
            recordsDict.TryGetValue(record.id, out var definition) == false || definition == null)
        {
            return false;
        }
        var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
        if (explore == null || explore.CurrentSetupData == null)
        {
            return false;
        }
        if (IsPassiveLevelReward(record))
        {
            if (record.uniqueID != PassiveUpgradeIdentity(record, explore.CurrentSetupData.SelectedClassId))
            {
                return false;
            }
        }
        else if (recordInventory.GetRecord(record.uniqueID) != record)
        {
            return false;
        }
        if (IsRuntimePassiveRecord(record))
        {
            var template = ResolveRecordPassiveTemplate(record);
            var active = AppManager.Instance.SafeInvoke(app => app.GetPassiveSystem())?.GetRunPassive(explore.CurrentSetupData.SelectedClassId, definition.linkedPassiveSkillID);
            level = active?.SkillLevel ?? 0;
            maximum = template.SafeInvoke(data => data.maxLevel);
        }
        else if (definition.linkedPassiveSkillID > 0 && recordPassives.TryGetValue(record.uniqueID, out var effect) &&
            effect.SkillID == definition.linkedPassiveSkillID)
        {
            int job = definition.targetFilter == TargetFilterType.ALL ? 0 : (int)definition.targetFilter;
            var template = explore.ResolvePassiveTemplate(definition.linkedPassiveSkillID, job);
            maximum = template.SafeInvoke(data => data.maxLevel);
            level = Mathf.Max(1, record.ownedLevels);
        }
        return maximum > 1 && level > 0 && level < maximum;
    }

    public bool TryUpgradeOwned(RecordData record, int expectedLevel)
    {
        if (TryGetUpgradeLevel(record, out int current, out _) == false || current != expectedLevel)
        {
            return false;
        }
        if (IsRuntimePassiveRecord(record))
        {
            return GrantRecord(record) != null;
        }
        var effect = recordPassives[record.uniqueID];
        record.ownedLevels = current + 1;
        effect.SetLevel(record.ownedLevels);
        effect.OnChangedLevel(record.ownedLevels);
        isDirty = true;
        return true;
    }

    /// <summary>이전 저장에는 카드별 기여도가 없어 기존 탐사 레벨에서 직업 기본분을 제외합니다.</summary>
    private int GrantedLevels(RecordData owned, ExploreManager explore, int passiveId)
    {
        if (owned == null)
        {
            return 0;
        }
        if (owned.ownedLevels > 0)
        {
            return owned.ownedLevels;
        }
        int job = explore.CurrentSetupData.SelectedClassId;
        var effect = AppManager.Instance.GetPassiveSystem()?.GetRunPassive(job, passiveId);
        return Mathf.Max(0, (effect?.SkillLevel ?? 0) - (explore.IsJobStartingPassive(job, passiveId) ? 1 : 0));
    }

    public bool TrySellOwned(RecordData record)
    {
        if (record == null || string.IsNullOrEmpty(record.uniqueID) || recordInventory.GetRecord(record.uniqueID) != record)
        {
            return false;
        }
        if (IsRuntimePassiveRecord(record))
        {
            var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
            if (explore == null || explore.CurrentSetupData == null)
            {
                return false;
            }
            int job = explore.CurrentSetupData.SelectedClassId;
            int id = recordsDict[record.id].linkedPassiveSkillID;
            var system = AppManager.Instance.GetPassiveSystem();
            if (system == null || system.RemoveRecordLevels(job, id, GrantedLevels(record, explore, id),
                explore.IsJobStartingPassive(job, id) ? 1 : 0) == false)
            {
                return false;
            }
        }
        return RemoveOwnedRecord(record);
    }

    public bool RemoveOwnedRecord(RecordData record)
    {
        var owned = record == null ? null : recordInventory.GetRecord(record.uniqueID);
        if (owned == null)
        {
            return false;
        }

        recordInventory.RemoveRecord(owned);
        if (recordPassives.TryGetValue(owned.uniqueID, out var passive))
        {
            AppManager.Instance?.GetPassiveSystem()?.Remove(Constants.GLOBAL_RECORD_JOB_ID, passive);
            recordPassives.Remove(owned.uniqueID);
        }
        SaveIfDirty();
        return true;
    }

    // Return the actual item so result UI and passive registration cannot use a duplicate's original data.
    public RecordData GrantRecord(RecordData recordData)
    {
        if (recordData == null || HasConflictingRecord(recordData))
        {
            return null;
        }

        if (IsRuntimePassiveRecord(recordData))
        {
            var explore = AppManager.Instance.SafeInvoke(app => app.GetExploreManager());
            var template = ResolveRecordPassiveTemplate(recordData);
            if (explore == null || template == null)
            {
                return null;
            }
            if (IsPassiveLevelReward(recordData))
            {
                if (explore.GrantPassiveRecord(template) == false)
                {
                    return null;
                }
                recordData.ownedLevels = AppManager.Instance.SafeInvoke(app => app.GetPassiveSystem())?.GetRunPassive(explore.CurrentSetupData.SelectedClassId, template.id)?.SkillLevel ?? 0;
                isReceived = true;
                isDirty = true;
                SaveIfDirty();
                explore.SaveExploreMap();
                return recordData;
            }
            var owned = recordInventory.Records.FirstOrDefault(r => r.id == recordData.id);
            int previous = GrantedLevels(owned, explore, template.id);
            if (explore.GrantPassiveRecord(template) == false)
            {
                return null;
            }
            // 강화 레코드는 한 장만 보관하며 중복 획득은 빈 레코드 대신 레벨로 반영합니다.
            var result = owned ?? recordInventory.AddRecord(recordData);
            result.ownedLevels = previous + 1;
            isReceived = true;
            isDirty = true;
            SaveIfDirty();
            explore.SaveExploreMap();
            return result;
        }
        var granted = recordInventory.AddRecord(recordData);
        if (granted == null)
        {
            return null;
        }

        isReceived = true;
        isDirty = true;
        if (granted.id != RecordDataBase.EmptyRecordId)
        {
            RegisterRecordPassive(granted);
        }

        SaveIfDirty();
        return granted;
    }

    // Same SO enrichment, job eligibility and unowned rule as the existing draft.
    public List<RecordData> GetShopCandidates()
    {
        var catalog = recordsDict.Values.Where(template => template != null && template.isImplemented && template.id > 0)
            .Select(template => template.GetRecordData());
        return FilterDraftCandidates(catalog, false)
            .GroupBy(r => r.id).Select(g => g.First()).ToList();
    }

    public List<RecordData> GetRewardCandidates()
    {
        return FilterDraftCandidates(GetAllEnrichedRecordData(), false);
    }

    /// <summary>상점 추첨 조건과 관계없이 등록된 실제 획득 대상의 ID를 반환합니다.</summary>
    public IEnumerable<int> GetImplementedRecordIds()
    {
        return recordsDict.Values
            .Where(record => record != null && record.isImplemented && record.id > 0)
            .Select(record => record.id);
    }

    public RecordData GetShopRecord(int id)
    {
        return recordsDict.TryGetValue(id, out var record) && record != null
        ? record.GetRecordData() : null;
    }

    public void RemoveTransferedRecord(RecordData target)
    {
        transferInventory.RemoveRecord(target);
        SaveIfDirty();
    }

    public List<RecordData> RerollAllCurrentRecords()
    {
        DataBaseManager db = DataBaseManager.Instance;
        if (db == null)
        {
            return null;
        }

        // 1. 후보군 생성 (전체 - 이미 영구 보유 중인 것들)
        // 현재 떠 있는 것(CurrentOptions)은 제외하지 않습니다.
        // 그래야 리롤 시점에 다시 나올 기회를 얻어 '빈 레코드'가 성급하게 뜨지 않습니다.
        var allRecord = GetAllEnrichedRecordData();
        var candidates = FilterDraftCandidates(allRecord, filterStartingSkills)
            .Where(IsRewardCandidate)
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
            if (i < CurrentOptions.Count && CurrentOptions[i].isLocked && IsRewardCandidate(CurrentOptions[i]))
            {
                newOptions.Add(CurrentOptions[i]);
                continue;
            }

            // 뽑을 수 있는 레코드가 있다면 랜덤 추출
            if (candidates.Count > 0)
            {
                var selected = PickRandomRewardRecord(candidates);
                if (selected == null)
                {
                    continue;
                }
                newOptions.Add(selected);
                candidates.RemoveAll(candidate => candidate.id == selected.id);
            }
            else
            {
                // Do not pad a partially available draft with dummy records.
                continue;
            }
        }

        if (newOptions.Count == 0)
        {
            var emptyRecord = db.GetEmptyRecord();
            if (emptyRecord != null)
            {
                newOptions.Add(emptyRecord);
            }
        }

        // 성공적으로 리롤이 수행된 경우에만 카운트 차감
        rerollCount--;

        // 4. 데이터 갱신 및 UI 트리거
        CurrentOptions = newOptions;
        SaveIfDirty();

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
        if (SelectedRecords == null || SelectedRecords.Count <= 0)
        {
            return false;
        }

        foreach (RecordData data in SelectedRecords)
        {
            // 1. 인벤토리에서 해당 레코드 삭제 (소비)
            RemoveOwnedRecord(data);

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
        {
            return false;
        }

        var ps = AppManager.Instance.GetPassiveSystem();
        if (ps == null)
        {
            return false;
        }

        foreach (RecordData data in selectedRecords)
        {
            GrantRecord(data);
        }

        SelectedRecords.Clear();
        filterStartingSkills = false;
        PauseManager.RequestResume();
        return true;
    }

    public bool OnCompleteArchiveRecord()
    {
        List<RecordData> selectedRecords = SelectedRecords;
        if (selectedRecords == null || selectedRecords.Count <= 0)
        {
            return false;
        }

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
        if (selectedRecords == null || selectedRecords.Count <= 0)
        {
            return false;
        }

        var ps = AppManager.Instance.GetPassiveSystem();
        if (ps == null)
        {
            return false;
        }

        foreach (RecordData data in selectedRecords)
        {
            var granted = GrantRecord(data);
            if (granted == null)
            {
                continue;
            }

            RemoveTransferedRecord(data);
        }


        SelectedRecords.Clear();
        PauseManager.RequestResume();
        return true;
    }

    public void SaveIfDirty()
    {
        if (isDirty == false)
        {
            return;
        }

        RecordSaveListData save = new RecordSaveListData();
        save.catalogVersion = 1;

        // 1. 소지 중인 레코드 저장
        foreach (RecordData data in recordInventory.Records)
        {
            save.recordIDs.Add(new RecordSaveData()
            {
                ownedLevels = data.ownedLevels,
                recordID = data.id,
                uniqueID = data.uniqueID
            });
        }

        // 2. 인계된 레코드 저장
        foreach (RecordData data in transferInventory.Records)
        {
            save.transferedrecordIDs.Add(new RecordSaveData()
            {
                ownedLevels = data.ownedLevels,
                recordID = data.id,
                uniqueID = data.uniqueID
            });
        }

        save.isReceived = isReceived;
        save.highestRarityMissCount = highestRarityMissCount;
        SaveManager.SaveRecordData(save);
        isDirty = false;
    }
}
