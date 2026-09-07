using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppManager
    : Singleton<AppManager>
{
    public Action OnAwaked;
    public event Action OnSelectedRecordCard;

    private DataBaseManager databaseManager;
    private SkillManager skillManager;
    private SkillTreeManager skillTree;
    private RewardManager rewardManager;
    private RecordManager recordManager;
    private ExploreManager exploreManager;

    [SerializeField] private bool bCheat;
    public bool Cheat => bCheat;

    private bool isProcessingReward = false;

    // 패시브 스킬을 처리하는 시스템 클래스
    private PassiveSystem passiveSystem = new();


    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicate)
            return;

        if (Instance != this)
            return;


        // --------------------------------------------------
        // Manager Reference
        // --------------------------------------------------

        databaseManager = GetComponent<DataBaseManager>();

        skillManager = SkillManager.Instance;
        skillTree = SkillTreeManager.Instance;

        rewardManager = GetComponent<RewardManager>();
        recordManager = GetComponent<RecordManager>();
        exploreManager = GetComponent<ExploreManager>();


        // --------------------------------------------------
        // 최초 초기화
        // --------------------------------------------------

        if (IsInitialized == false)
        {
            if (skillManager != null)
            {
                skillManager.OnDataChanged +=
                    OnSkillDataChanged;
            }

            InventoryManager.Instance?.OnInit();

            PlayerManager.Instance?.OnInit();

            CurrencyManager.Instance?.OnInit(
                (CurrencyInventory)
                InventoryManager.Instance?.GetInvetory(
                    ItemCategory.CURRENCY));

            recordManager?.OnInit();

            SceneManager.sceneUnloaded +=
                OnUnloadScene;
        }


        // --------------------------------------------------
        // UI Manager
        // --------------------------------------------------

        if (exploreManager != null)
        {
            ManagerWaiter.WaitForManager<UIManager>(
                uiManager =>
                {
                    uiManager.OnReturnedStageSelect +=
                        exploreManager.OnReturnedStageSelectScene;
                });
        }


        // --------------------------------------------------
        // Passive System
        // --------------------------------------------------

        passiveSystem?.OnInit();


        // --------------------------------------------------
        // Game Manager Events
        // --------------------------------------------------

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBeginStage +=
                OnBeginStage;

            GameManager.Instance.OnUpdated +=
                OnUpdate;

            GameManager.Instance.OnFinishStage +=
                FinishStageProcess;
        }


        // --------------------------------------------------
        // Explore Manager Events
        // --------------------------------------------------

        if (exploreManager != null)
        {
            exploreManager.OnExploreStart +=
                HandleExploreStart;

            exploreManager.OnReturnToMain +=
                HandleReturnToMain;

            exploreManager.OnInStage +=
                HandleInStage;

            exploreManager.OnStageClear +=
                HandleStageClear;

            exploreManager.OnExploreFinish +=
                HanldeExploreFinish;
        }


        // --------------------------------------------------
        // Awake 완료
        // --------------------------------------------------

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
        if (Instance != this)
        {
            skillManager = Instance.skillManager;
            skillTree = Instance.skillTree;
            databaseManager = Instance.databaseManager;
            recordManager = Instance.recordManager;
            exploreManager = Instance.exploreManager;
            rewardManager = Instance.rewardManager;

            bCheat = Instance.bCheat;

            OnAwaked = Instance.OnAwaked;
        }
    }


    private void OnDisable()
    {
        if (Instance != this)
            return;


        // --------------------------------------------------
        // Skill Manager
        // --------------------------------------------------

        if (skillManager != null)
        {
            skillManager.OnDataChanged -=
                OnSkillDataChanged;
        }


        // --------------------------------------------------
        // Game Manager
        // --------------------------------------------------

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBeginStage -=
                OnBeginStage;

            GameManager.Instance.OnUpdated -=
                OnUpdate;

            GameManager.Instance.OnFinishStage -=
                FinishStageProcess;
        }


        // --------------------------------------------------
        // Explore Manager
        // --------------------------------------------------

        if (exploreManager != null)
        {
            exploreManager.OnExploreStart -=
                HandleExploreStart;

            exploreManager.OnReturnToMain -=
                HandleReturnToMain;

            exploreManager.OnInStage -=
                HandleInStage;

            exploreManager.OnStageClear -=
                HandleStageClear;

            exploreManager.OnExploreFinish -=
                HanldeExploreFinish;
        }


        // --------------------------------------------------
        // Scene Manager
        // --------------------------------------------------

        SceneManager.sceneUnloaded -=
            OnUnloadScene;
    }


    private void OnSkillDataChanged()
    {
        PlayerManager.Instance?.SetDirty();
    }

    #endregion


    #region Explore

    public void HandleStageResult(StageResult result)
    {
        if (exploreManager == null)
            return;


        // 1. ExploreManager에게 결과 전달
        exploreManager.ClearStage(
            result.IsSuccess);


        // 2. 결과 UI
        bool isRunCompletelyFinished =
            !result.IsSuccess ||
            exploreManager.AllStageClear;


        if (isRunCompletelyFinished)
        {
            UIManager.Instance?.OpenExploreResultPopUp();
        }
        else
        {
            UIManager.Instance.SafeInvoke(
                v =>
                    v.ShowStageResultUI(
                        result.IsSuccess));
        }


        // 3. 보상 처리
        AcceptReward();


        // 4. 저장
        SaveIfDirty();
    }


    public ExploreManager GetExploreManager()
    {
        return exploreManager;
    }


    private void AcceptReward()
    {
        if (isProcessingReward)
            return;

        isProcessingReward = true;


        if (exploreManager != null &&
            exploreManager.CurrentState !=
            ExploreState.STAGE_CLEAR)
        {
            isProcessingReward = false;
            return;
        }


        // --------------------------------------------------
        // 전체 탐사 클리어
        // --------------------------------------------------

        if (exploreManager.AllStageClear)
        {
            int chapter =
                exploreManager.Chapter;

            SetChapterClearReward(chapter);

            isProcessingReward = false;
            return;
        }


        // --------------------------------------------------
        // 일반 스테이지 클리어
        // --------------------------------------------------

        MapNodeInfo nodeInfo =
            exploreManager.GetReplacedNodeInfo();


        if (nodeInfo == null)
        {
            Debug.LogWarning(
                "보상을 받을 노드 정보를 찾을 수 없습니다.");

            isProcessingReward = false;
            return;
        }


        if (nodeInfo.clearRewardId > 0)
        {
            rewardManager.SafeInvoke(
                v =>
                    v.GiveStageReward(
                        nodeInfo.clearRewardId));
        }


        isProcessingReward = false;
    }


    /// <summary>
    /// 최종 결과창의 [확인 / 로비로] 버튼에 연결.
    /// </summary>
    public void CompleteRunAndReturnToLobby()
    {
        exploreManager.SafeInvoke(
            v =>
                v.PurgeCurrentRun());

        ReturnToLobbyScene();
    }


    public void EnterTheExplorationProcess()
    {
        // 레코드 데이터 초기화
        recordManager.SafeInvoke(
            v =>
                v.ResetRecordFlowData());


        // 탐사 데이터 초기화
        exploreManager.SafeInvoke(
            v =>
                v.StartExplore());


        // 탐사 패시브 초기화
        passiveSystem.ResetExplorePassives();


        // 스킬 런타임 데이터 초기화
        skillManager.SafeInvoke(
            v =>
                v.ResetRunTimeData());


        // 탐사 준비 UI
        UIManager.Instance.SafeInvoke(
            v =>
                v.OpenExplorationSetupPopUp());
    }


    public void ContinueExplorationProcess()
    {
        SceneManager.LoadScene(
            "StageSelectScene");
    }


    public bool HasSavedExploration()
    {
        return SaveManager.HasSavedMapData();
    }


    public bool CanEnableNode(MapNode node)
    {
        if (node == null ||
            exploreManager == null)
            return false;

        return exploreManager.CanEnableNode(
            node.id,
            bCheat);
    }


    public StageInfo GetStageInfo(int stageID)
    {
        if (databaseManager == null)
            return null;

        return databaseManager.GetStageInfo(
            stageID);
    }


    public StageInfo GetBossStageInfo(
        int chapter,
        int stageID)
    {
        if (databaseManager == null)
            return null;

        return databaseManager.GetBossStageInfo(
            chapter,
            stageID);
    }


    public int GetRandomStageId(int chapter)
    {
        if (databaseManager == null)
            return -1;

        return databaseManager.GetRandomStageID(
            chapter);
    }


    public int GetRandomBossStageID(int chapter)
    {
        if (databaseManager == null)
            return -1;

        return databaseManager.GetRandomBossStageID(
            chapter);
    }


    public StageInfo CreateRandomBossStage(int chapter)
    {
        int stageID =
            GetRandomBossStageID(chapter);

        if (stageID < 0)
            return null;

        return GetBossStageInfo(
            chapter,
            stageID);
    }


    public StageInfo CreateRandomStage(int chapter)
    {
        int stageID =
            GetRandomStageId(chapter);

        if (stageID < 0)
            return null;

        return GetStageInfo(stageID);
    }


    public MonsterData GetMonsterData(int monsterID)
    {
        if (databaseManager == null)
            return null;

        return databaseManager.GetMonsterData(
            monsterID);
    }


    public MonsterGroupData GetGroupData(int groupID)
    {
        if (databaseManager == null)
            return null;

        return databaseManager.GetMonsterGroupData(
            groupID);
    }


    public MonsterStatData GetMonsterStatData(
        int monsterID)
    {
        if (databaseManager == null)
            return null;

        return databaseManager.GetMonsterStatData(
            monsterID);
    }


    public void EnterStageByNode(MapNode node)
    {
        if (node == null ||
            exploreManager == null)
            return;

        Debug.Log(
            $"Current Select Node ID : {node.id}");

        exploreManager.EnterStageByNode(node);
    }


    private void HandleExploreStart()
    {
    }


    private void HandleReturnToMain()
    {
        if (recordManager == null)
            return;

        recordManager.GenerateChapterStartRecords();
    }


    private void HandleInStage(int stageID)
    {
    }


    private void HandleStageClear()
    {
        if (exploreManager.AllStageClear == false)
        {
            recordManager.SetReceiveRecordFlag();
        }
    }


    private void HanldeExploreFinish()
    {
    }


    private void FinishStageProcess()
    {
        SaveIfDirty();
    }


    public MapNodeInfo GetNodeInfoMatchedMapNode(
        MapNode mapNode)
    {
        if (mapNode == null ||
            exploreManager == null)
            return null;

        return exploreManager.GetReplacedNodeInfo(
            mapNode.id);
    }

    #endregion


    #region Skill

    public void EquipSavedClassActiveSkill(
        int classID,
        List<int> skillIDs)
    {
        if (skillManager == null ||
            skillTree == null)
            return;

        int slot = 0;

        foreach (int skillID in skillIDs)
        {
            SkillRuntimeData runtimeData =
                skillTree.GetSkillRuntimeData(
                    classID,
                    skillID);

            EquipActiveSkill(
                classID,
                slot,
                runtimeData);

            slot++;
        }
    }


    public void EquipActiveSkill(
        int charId,
        int slot,
        SkillRuntimeData skill)
    {
        if (skillManager == null)
            return;

        skillManager.EquipActiveSkill(
            charId,
            slot,
            skill);
    }


    public void UnequipActiveSkill(
        int charId,
        int slot)
    {
        if (skillManager == null)
            return;

        skillManager.EquipActiveSkill(
            charId,
            slot,
            null);
    }


    public List<SkillRuntimeData>
        GetEquippedActiveSkillListByCharID(
            int charId)
    {
        if (skillManager == null)
            return null;

        return skillManager.GetActiveSkillList(
            charId);
    }


    public List<int>
        GetEquippedActiveSkillIDListByCharID(
            int charID)
    {
        if (skillManager == null)
            return null;

        return skillManager.GetActiveSkillIDList(
            charID);
    }


    public void SetActiveSkills(
        int jobID,
        SkillComponent skillComp)
    {
        if (skillManager == null ||
            skillComp == null)
            return;

        skillManager.SetActiveSkills(
            jobID,
            skillComp);
    }


    public PassiveSystem GetPassiveSystem()
    {
        return passiveSystem;
    }


    public void AddPassiveSkill(
        int jobID,
        PassiveSkill passiveSkill)
    {
        if (passiveSystem == null)
            return;

        passiveSystem.Add(
            jobID,
            passiveSkill);
    }


    public void RemovePassiveSkill(
        int jobID,
        PassiveSkill passiveSkill)
    {
        if (passiveSystem == null)
            return;

        passiveSystem.Remove(
            jobID,
            passiveSkill);
    }


    public void OnApplyStaticEffct(
        int jobID,
        Character owner)
    {
        passiveSystem?.OnApplyStaticEffect(
            jobID,
            owner);
    }


    public void OnAcquire(
        int jobID,
        Character owner)
    {
        passiveSystem?.OnAcquire(
            jobID,
            owner);
    }


    public void OnLose(
        int jobID,
        Character owner)
    {
        passiveSystem?.OnLose(
            jobID,
            owner);
    }


    public void OnUpdate(float dt)
    {
        passiveSystem?.OnUpdate(dt);
    }


    public void OnChangedLevelPassiveSkill(
        int jobID,
        SkillRuntimeData data)
    {
        passiveSystem?.OnChangedLevel(
            jobID,
            data);
    }

    #endregion


    #region Database

    public ItemData GetItemData(
        int itemId,
        ItemCategory category)
    {
        if (category == ItemCategory.EQUIPMENT)
            return GetEquipmentItem(itemId);

        if (category == ItemCategory.INGREDIANT)
            return GetIngredientItem(itemId);

        return GetCurrencyItem(itemId);
    }


    public DataBaseManager GetDataBaseManager()
    {
        return databaseManager;
    }


    public EquipmentItem GetEquipmentItem(int itemid)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetEquipmentItem(itemid));
    }


    public IngredientItem GetIngredientItem(int itemId)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetIngredientItem(itemId));
    }


    public CurrencyItem GetCurrencyItem(int itemId)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetCurrencyItem(itemId));
    }


    public CurrencyItem GetCurrencyItemByType(
        CurrencyType type)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetCurrencyItemByType(type));
    }


    public ShopItem GetShopItem(int itemId)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetShopItem(itemId));
    }


    public List<ItemData> GetShopItems(
        ItemCategory category)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetShopItems(category));
    }


    public EnhanceLevelData GetEnhanceLevelData(
        ItemRank rank,
        int enhanceLevel)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetEnhanceLevelData(
                    (int)rank,
                    enhanceLevel));
    }


    public EnhanceStatData GetEnhanceStatData(
        ItemRank rank,
        int enhanceLevel)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetEnhanceStatData(
                    (int)rank,
                    enhanceLevel));
    }


    public List<EnhanceStatData> GetEnhanceStatDatas(
        ItemRank rank)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetEnhanceStatDatas(
                    (int)rank));
    }


    public List<RecordData> GetRecordByRarity(
        RecordRarity rarity)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetRecordDatas(rarity));
    }


    public RecordData GetRecordData(int recordID)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetRecordData(recordID));
    }


    public List<RecordData> GetAllRecordData()
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetAllRecordData());
    }


    public RecordData GetEmptyRecord()
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetEmptyRecord());
    }


    public EventInfo GetEventInfo(int eventID)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetEventInfo(eventID));
    }

    #endregion


    #region Reward

    public RewardData GetRewardData(int rewardId)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetRewardData(rewardId));
    }


    public ClearRewardData GetStageClearRewardData(
        int stageid)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetStageClearReward(stageid));
    }


    public ClearRewardData GetChapterClearRewardData(
        int clearedChapter)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetChapterClearReward(
                    clearedChapter));
    }


    public void SetChapterClearReward(
        int clearedChapter)
    {
        rewardManager.SafeInvoke(
            v =>
                v.GiveChapterReward(
                    clearedChapter));
    }

    #endregion


    #region Record Data

    public void TriggerRecordUI(
        List<RecordData> records,
        bool canReroll = true,
        RecordUIMode mode = RecordUIMode.DRAFT)
    {
        PauseManager.RequestPause();

        UIManager.Instance.SafeInvoke(
            v =>
                v.OpenRecordSelectPopUp(
                    records,
                    canReroll,
                    mode));
    }


    public void OnRecordSelected(
        RecordData selected)
    {
        recordManager.SafeInvoke(
            v =>
                v.SelectedRecord(selected));

        OnSelectedRecordCard?.Invoke();
    }


    public void GenerateRecord_Test(
        int recordCount,
        bool canReroll = true)
    {
        recordManager.SafeInvoke(
            v =>
                v.GenerateRewardRecords(
                    recordCount,
                    canReroll));
    }


    public RecordManager GetRecordManager()
    {
        return recordManager;
    }

    #endregion


    #region Scene Navigation (UI Event)

    public void MoveToNextNodeScene()
    {
        SceneManager.LoadScene(
            "StageSelectScene");
    }


    public void ReturnToLobbyScene()
    {
        SceneManager.LoadScene(
            "Lobby");
    }

    #endregion


    #region Save

    private void OnBeginStage()
    {
    }


    public void OnUnloadScene(Scene scene)
    {
        SaveIfDirty();
    }


    public void SaveIfDirty()
    {
        skillTree.SafeInvoke(
            v =>
                v.SaveIfDirty());

        InventoryManager.Instance.SafeInvoke(
            v =>
                v.SaveIfDirty());

        PlayerManager.Instance.SafeInvoke(
            v =>
                v.SaveIfDirty());

        exploreManager.SafeInvoke(
            v =>
                v.SaveExploreMap());

        recordManager.SafeInvoke(
            v =>
                v.SaveIfDirty());
    }


    public void SaveExploreMap()
    {
        exploreManager.SafeInvoke(
            v =>
                v.SaveExploreMap());
    }

    #endregion


    public Sprite GetStageIcon(StageType type)
    {
        return databaseManager.SafeInvoke(
            v =>
                v.GetStageIcon(type));
    }
}