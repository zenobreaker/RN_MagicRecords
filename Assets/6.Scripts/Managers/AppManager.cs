using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;
using UnityEngine.SceneManagement;

public class AppManager
    : Singleton<AppManager>
{
    public Action OnAwaked;
    public event Action OnSelectedRecordCard;
    public event Action<RecordData> OnRecordSelectedComplete;

    private DataBaseManager databaseManager;
    private SkillManager skillManager;
    private SkillTreeManager skillTree;
    private RewardManager rewardManager;
    private RecordManager recordManager;
    private ExploreManager exploreManager;

    [SerializeField] private bool bCheat;
    public bool Cheat => bCheat;

    private bool isProcessingReward = false; // 중복 실행 방지 플래그
    // 패시브 스킬을 처리하는 시스템 클래스 
    private PassiveSystem passiveSystem = new();

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        databaseManager = GetComponent<DataBaseManager>();
        skillManager = GetComponent<SkillManager>();
        skillTree = SkillTreeManager.Instance;
        rewardManager = GetComponent<RewardManager>();
        recordManager = GetComponent<RecordManager>();
        exploreManager = GetComponent<ExploreManager>();

        if (IsInitialized == false)
        {
            skillManager.OnDataChanaged += () => { PlayerManager.Instance.SetDirty(); };
            InventoryManager.Instance?.OnInit();
            PlayerManager.Instance?.OnInit();
            CurrencyManager.Instance?.OnInit((CurrencyInventory)InventoryManager.Instance?.GetInvetory(ItemCategory.CURRENCY));
            recordManager.OnInit();
            SceneManager.sceneUnloaded += OnUnloadScene;
        }

        if (exploreManager != null)
        {
            ManagerWaiter.WaitForManager<UIManager>((uiManager) =>
            {
                uiManager.OnReturnedStageSelectStage += exploreManager.OnReturnedStageSelectScene;
            });
        }

        passiveSystem?.OnInit();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBeginStage += OnBeginStage;
            GameManager.Instance.OnUpdated += OnUpdate;

            GameManager.Instance.OnFinishStage += FinishStageProcess;
            GameManager.Instance.OnSuccedStage += SuccessStageProcess;
            GameManager.Instance.OnFailedStage += FailedStageProcess;
        }

        if (exploreManager != null)
        {
            exploreManager.OnExploreStart += HandleExploreStart;
            exploreManager.OnReturnToMain += HandleReturnToMain;
            exploreManager.OnInStage += HandleInStage;
            exploreManager.OnStageClear += HandleStageClear;
            exploreManager.OnExploreFinish += HanldeExploreFinish;
        }

        OnAwaked?.Invoke();
        OnAwaked = null;

        PauseManager.Reset();
    }

    private void OnApplicationQuit()
    {
        SaveIfDirty();
    }

    protected override void SyncDataFromSingleton()
    {
        // 기존 싱글톤 인스턴스가 자기 자신이 아니면
        if (Instance != this)
        {
            skillManager = Instance.skillManager;
            skillTree = Instance.skillTree;
            databaseManager = Instance.databaseManager;
            recordManager = Instance.recordManager;
            exploreManager = Instance.exploreManager;

            bCheat = Instance.bCheat;

            OnAwaked = Instance.OnAwaked;
        }
    }

    private void OnDisable()
    {
        if (Instance != null) return;

        if (GameManager.Instance == null) return;

        GameManager.Instance.OnBeginStage -= OnBeginStage;
        GameManager.Instance.OnUpdated -= OnUpdate;

        GameManager.Instance.OnFinishStage -= FinishStageProcess;
        GameManager.Instance.OnSuccedStage -= SuccessStageProcess;
        GameManager.Instance.OnFailedStage -= FailedStageProcess;

        if (exploreManager != null)
        {
            exploreManager.OnExploreStart -= HandleExploreStart;
            exploreManager.OnReturnToMain -= HandleReturnToMain;
            exploreManager.OnInStage -= HandleInStage;
            exploreManager.OnStageClear -= HandleStageClear;
            exploreManager.OnExploreFinish -= HanldeExploreFinish;
        }
    }

    #region Explore 

    public ExploreManager GetExploreManager() { return exploreManager; }

    public MapReplacer GetMapReplacer()
    {
        return exploreManager == null ? null : exploreManager.MapReplacer;
    }
    private void ResetData()
    {
        exploreManager?.ResetData();
    }

    private void AcceptReward()
    {
        if (isProcessingReward) return;
        isProcessingReward = true;

        if (exploreManager != null && exploreManager.CurrentState != ExploreState.STAGE_CLEAR)
        {
            isProcessingReward = false;
            return;
        }

        if (exploreManager.AllStageClear)
        {
            int chapter = exploreManager.Chapter;
            SetChapterClearReward(chapter);
            isProcessingReward = false;
            return;
        }

        // 💡 StageInfo 대신 MapNodeInfo를 가져옵니다.
        MapNodeInfo nodeInfo = exploreManager?.GetReplacedNodeInfo();

        if (nodeInfo == null)
        {
            Debug.LogWarning("보상을 받을 노드 정보를 찾을 수 없습니다.");
            isProcessingReward = false;
            return;
        }

        // 💡 껍데기에 적힌 보상 ID만 보고 바로 지급합니다!
        // (RewardManager의 GiveStageReward 함수가 int rewardId를 받도록 수정해야 합니다)
        if (nodeInfo.clearRewardId > 0)
        {
            rewardManager?.GiveStageReward(nodeInfo.clearRewardId);
        }

        isProcessingReward = false;
    }


    public void EnterTheExplorationProcess()
    {
        // 탐사 데이터 초기화 
        exploreManager?.StartExplore();

        SceneManager.LoadScene(1);
    }

    public bool EnableNode(MapNode node)
    {
        if (node == null || exploreManager == null) return false;

        return exploreManager.EnableNode(node.id, bCheat);
    }

    public string GetRandomBiome(int chapter)
    {
        if (databaseManager == null) return ""; 
        string biomeName =  databaseManager.GetRandomBiome(chapter);

        exploreManager.BiomeName = biomeName;

        return biomeName;
    }

    public SO_Biome GetBiomeData(string biomeName)
    {
        return BiomeManager.Instance.GetBiomeData(biomeName);
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetStageInfo(stageID);
    }

    public StageInfo GetBossStageInfo(int chapter, int stageID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetBossStageInfo(chapter, stageID);
    }

    public int GetRandomStageId(int chapter)
    {
        if (exploreManager == null) return -1;

        return databaseManager.GetRandomStageID(chapter); 
    }

    public int GetRandomBossStageID(int chapter)
    {
        if (exploreManager == null) return 0;

        int stageId = databaseManager.GetRandomBossStageID(chapter);
        return stageId;
    }

    public MonsterData GetMonsterData(int monsterID)
    {
        return databaseManager?.GetMonsterData(monsterID);
    }

    public MonsterGroupData GetGroupData(int groupID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetMonsterGroupData(groupID);
    }

    public MonsterStatData GetMonsterStatData(int monsterID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetMonsterStatData(monsterID);
    }

    public void EnterStageByNode(MapNode node)
    {
        if (node != null && exploreManager != null)
        {
            Debug.Log($"Current Select Node ID : {node.id}");
            exploreManager.EnterStageByNode(node);
        }

        if (recordManager != null)
            recordManager.ResetReceiveFlag();

        var nodeInfo = GetNodeInfoMatchedMapNode(node);
        NodeRouter.EnterNode(nodeInfo);
    }

    public int GetRandomExploreEvent(int chapter)
    {
       return  databaseManager.GetRandomEventID(chapter);
    }

    private void HandleExploreStart()
    {

    }

    private void HandleReturnToMain()
    {
        // 탐사 씬 메인으로 오는 경우에도 호출 
        GenerateRecord(3);
    }

    private void HandleInStage(int stageID)
    {

    }

    private void HandleStageClear()
    {
        // 스테이지 마지막인지 검사해서 아니라면 
        // 레코드를 얻을 수 있는 플래그 켜야함
        if(exploreManager.AllStageClear == false)
        {
            recordManager.SetReceiveRecordFlag();
        }
    }

    private void HanldeExploreFinish()
    {

    }

    private void FinishStageProcess()
    {
        AcceptReward();

        SaveIfDirty();
    }
    private void SuccessStageProcess()
    {
        if (exploreManager != null)
        {
            exploreManager.ClearStage(true);
        }
    }

    private void FailedStageProcess()
    {
        Debug.Log($"Stage Challege Failed!..");
        exploreManager.ClearStage(false);
    }


    public MapNodeInfo GetNodeInfoMatchedMapNode(MapNode mapNode)
    {
        if (mapNode == null || exploreManager == null) return null;

        return exploreManager.GetReplacedNodeInfo(mapNode.id);
    }

    #endregion

    #region Skill 

    public void EquipSavedClassActiveSkill(int classID, List<int> skillIDs)
    {
        if (skillManager == null || skillTree == null) return;

        int slot = 0;
        foreach (int skillID in skillIDs)
        {
            SkillRuntimeData runtimeData = skillTree.GetSkillRuntimeData(classID, skillID);
            EquipActiveSkill(classID, slot, runtimeData);
            slot++;
        }
    }

    public void EquipActiveSkill(int charId, int slot, SkillRuntimeData skill)
    {
        if (skillManager == null) return;

        skillManager.EquipActiveSkill(charId, slot, skill);
    }

    public void UnequipActiveSkill(int charId, int slot)
    {
        if (skillManager == null) return;

        skillManager.EquipActiveSkill(charId, slot, null);
    }

    public List<SkillRuntimeData> GetEquippedActiveSkillListByCharID(int charId)
    {
        if (skillManager == null) return null;

        return skillManager.GetActiveSkillList(charId);
    }

    public List<int> GetEquippedActiveSkillIDListByCharID(int charID)
    {
        if (skillManager == null) return null;
        return skillManager.GetActiveSkillIDList(charID);
    }

    public void SetActiveSkills(int jobID, SkillComponent skillComp)
    {
        if (skillManager == null || skillComp == null) return;
        skillManager.SetActiveSkills(jobID, skillComp);
    }

    public void AddPassiveSkill(int jobID, PassiveSkill passiveSkill)
    {
        if (passiveSystem == null) return;
        passiveSystem.Add(jobID, passiveSkill);
    }

    public void RemovePassiveSkill(int jobID, PassiveSkill passiveSkill)
    {
        if (passiveSystem == null) return;
        passiveSystem.Remove(jobID, passiveSkill);
    }

    public void OnApplyStaticEffct(int jobID, GameObject ownerObj)
    {
        passiveSystem?.OnApplyStaticEffect(jobID, ownerObj);
    }

    public void OnAcquire(int jobID, GameObject ownerObj)
    {
        passiveSystem?.OnAcquire(jobID, ownerObj);
    }

    public void OnLose(int jobID, GameObject ownerObj)
    {
        passiveSystem?.OnLose(jobID, ownerObj);
    }

    public void OnUpdate(float dt)
    {
        passiveSystem?.OnUpdate(dt);
    }

    public void OnChangedLevelPassiveSkill(int jobID, SkillRuntimeData data)
    {
        passiveSystem?.OnChangedLevel(jobID, data);
    }

    #endregion

    #region Database
    public ItemData GetItemData(int itemId, ItemCategory category)
    {
        if (category == ItemCategory.EQUIPMENT)
            return GetEquipmentItem(itemId);
        else if (category == ItemCategory.INGREDIANT)
            return GetIngredientItem(itemId);
        else
            return GetCurrencyItem(itemId);
    }

    public EquipmentItem GetEquipmentItem(int itemid)
    {
        return databaseManager?.GetEquipmentItem(itemid);
    }

    public IngredientItem GetIngredientItem(int itemId)
    {
        return databaseManager?.GetIngredientItem(itemId);
    }

    public CurrencyItem GetCurrencyItem(int itemId)
    {
        return databaseManager?.GetCurrencyItem(itemId);
    }

    public CurrencyItem GetCurrencyItemByType(CurrencyType type)
    {
        return databaseManager?.GetCurrencyItemByType(type);
    }

    public ShopItem GetShopItem(int itemId)
    {
        return databaseManager?.GetShopItem(itemId);
    }

    public List<ItemData> GetShopItems(ItemCategory category)
    {
        return databaseManager?.GetShopItems(category);
    }

    public EnhanceLevelData GetEnhanceLevelData(ItemRank rank, int enhanceLevel)
    {
        return databaseManager?.GetEnhanceLevelData((int)rank, enhanceLevel);
    }

    public EnhanceStatData GetEnhanceStatData(ItemRank rank, int enhanceLevel)
    {
        return databaseManager?.GetEnhanceStatData((int)rank, enhanceLevel);
    }

    public List<EnhanceStatData> GetEnhanceStatDatas(ItemRank rank)
    {
        return databaseManager?.GetEnhanceStatDatas((int)rank);
    }

    public RecordData GetRecordData(int recordID)
    {
        return databaseManager?.GetRecordData(recordID);
    }

    public List<RecordData> GetAllRecordData()
    {
        return databaseManager?.GetAllRecordData();
    }

    public RecordData GetEmptyRecord()
    { return databaseManager?.GetEmptyRecord(); }

    public EventInfo GetEventInfo(int eventID)
    {
        return databaseManager?.GetEventInfo(eventID); 
    }

    #endregion

    #region Reward
    public RewardData GetRewardData(int rewardId)
    {
        return databaseManager?.GetRewardData(rewardId);
    }

    public ClearRewardData GetStageClearRewardData(int stageid)
    {
        return databaseManager?.GetStageClearReward(stageid);
    }

    public ClearRewardData GetChapterClearRewardData(int clearedChapter)
    {
        // 챕터가 클리어 되면 해당 챕터에 맞는 id로 변환되어 반환함 
        return databaseManager?.GetChapterClearReward(clearedChapter);
    }

    public void SetChapterClearReward(int clearedChapter)
    {
        rewardManager?.GiveChapterReward(clearedChapter);
    }
    #endregion

    #region Record Data 
    public void TriggerRecordUI(List<RecordData> records, bool canReroll = true, RecordUIMode mode = RecordUIMode.DRAFT)
    {
        PauseManager.RequestPause();
        UIManager.Instance.OpenRecordSelectPopUp(records, canReroll, mode);
    }

    public void OnRecordSelected(RecordData selected)
    {
        // 선택된 레코드 알림 
        recordManager?.SelectedRecord(selected);
        OnSelectedRecordCard?.Invoke();
    }

    public bool OnCompleteSelctRecords()
    {
        List<RecordData> selectedRecords = recordManager?.SelectedRecords;
        if (selectedRecords.Count <= 0)
            return false;
        
        foreach (RecordData data in selectedRecords)
        {
            // 선택된 레코드를 패시브 시스템에 등록
            var rp = recordManager?.GetRecordPassive(data.id);
            // 선택된 레코드를 소지품에 추가 
            recordManager?.AddRecord(data);
            AddPassiveSkill(9999, rp);
            OnRecordSelectedComplete?.Invoke(data);
        }

        PauseManager.RequestResume();
        return true;
    }

    public bool OnCompleteArchiveRecord()
    {
        List<RecordData> selectedRecords = recordManager?.SelectedRecords;
        if (selectedRecords == null || selectedRecords.Count <= 0) return false;

        foreach (RecordData data in selectedRecords)
        {
            recordManager.SetTranferRecord(data);
            Debug.Log($"[{data.recordName}] 레코드가 아카이브에 저장되었습니다!");
        }

        // 처리가 끝났으니 선택 리스트 비워주기
        recordManager.SelectedRecords.Clear();
        PauseManager.RequestResume();
        return true;
    }

    public void GenerateRecord(int recordCount, bool canReroll = true)
    {
        recordManager?.GenerateRecords(recordCount, canReroll);
    }

    public void RerollAllRecords()
    {
        recordManager?.RerollAllCurrentRecords();
    }

    public int GetRerollCount()
    {
        return recordManager.RerollCount;
    }

    public bool IsSelectRecordData(RecordData selectedData)
    {
        return recordManager == null ? false : recordManager.IsSelectedRecord(selectedData);
    }

    public RecordManager GetRecordManager() { return recordManager; }

    // TODO : 레코드 매니저에게 매개변수 없이 전달하면 내부에서 알아서 처리하게
    // 레코드 등장 개수가 특정 이벤트에 의해서 적어지거나 하는 가변적일 수 있음 
    // 레코드 매니저, 리워드 매니저 등 팝업을 띄우는 대상은 UI매니저의 인큐 함수를 
    // 초기에 등록시켜서 하면 좋을 듯

    #endregion

    #region Scene Navigation (UI Event)
    // 일반 스테이지 결과창에서 [다음으로] 버튼 클릭 시
    public void MoveToNextNodeScene()
    {
        // 탐사 맵 씬으로 이동
        SceneManager.LoadScene(1);
    }

    // 총 결산창에서 [로비로 돌아가기] 버튼 클릭 시
    public void ReturnToLobbyScene()
    {
        
        // 탐사 데이터 완전 초기화
        //ResetData();
        // 로비 씬으로 이동
        SceneManager.LoadScene(0);
    }
    #endregion

    #region Save

    private void OnBeginStage()
    {
        //TODO : 선택한 휠러의 직업들을 가져와 세팅해야 한다. 
    }

    public void OnUnloadScene(Scene scene)
    {
        SaveIfDirty();
    }

    public void SaveIfDirty()
    {
        skillTree?.SaveIfDirty();
        InventoryManager.Instance?.SaveIfDirty();
        PlayerManager.Instance?.SaveIfDirty();
        exploreManager?.SaveExploreMap();
        recordManager?.SaveIfDirty();
    }

    public void SaveExploreMap()
    {
        exploreManager?.SaveExploreMap();
    }
    #endregion

    public Sprite GetStageIcon(StageType type)
    {
        return databaseManager?.GetStageIcon(type);
    }
}
