using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppManager
    : Singleton<AppManager>
{
    public Action OnAwaked;

    private DataBaseManager databaseManager;
    private SkillManager skillManager;
    private SkillTreeManager skillTree;
    private RewardManager rewardManager;

    // 생성한 맵 정보를 가지고 있는 배치자 
    private MapReplacer mapReplacer;
    public MapReplacer MapReplacer { get { return mapReplacer; } }
    private StageReplacer stageReplacer;
    private bool bCreate = false;

    [SerializeField] private bool bCheat;

    private int chapter = 1;
    private int maxChapter = 1;
    private int mapNodeID = 0;  // 현재 고른 mapNode 
    private int prevNodeId = -1; // 이전에 고른 id 
    private bool bAllCleared = false;

    protected override void Awake()
    {
        base.Awake();

        databaseManager = GetComponent<DataBaseManager>();
        skillManager = GetComponent<SkillManager>();
        skillTree = SkillTreeManager.Instance;
        rewardManager = GetComponent<RewardManager>();

        mapReplacer = new MapReplacer();
        stageReplacer = new StageReplacer();

        if (IsInitialized == false)
        {
            skillManager.OnDataChanaged += () => { PlayerManager.Instance.SetDirty(); };
            InventoryManager.Instance.OnInit();
            PlayerManager.Instance.OnInit();
            CurrencyManager.Instance.OnInit((CurrencyInventory)InventoryManager.Instance.GetInvetory(ItemCategory.CURRENCY));
            SceneManager.sceneUnloaded += OnUnloadScene;
        }

        OnAwaked?.Invoke();
        OnAwaked = null;
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

            bCheat = Instance.bCheat;

            mapReplacer = Instance.mapReplacer;
            stageReplacer = Instance.stageReplacer;

            // 전역 상태 가져오기
            bCreate = Instance.bCreate;

            chapter = Instance.chapter;
            maxChapter = Instance.maxChapter;
            prevNodeId = Instance.prevNodeId;
            mapNodeID = Instance.mapNodeID;

            bAllCleared = Instance.bAllCleared;

            OnAwaked = Instance.OnAwaked;
        }
    }

    protected override void Start()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnFinishStage += FinishStageProcess;
        GameManager.Instance.OnSuccedStage += SuccessStageProcess;
        GameManager.Instance.OnFailedStage += FailedStageProcess;

        base.Start();
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnFinishStage -= FinishStageProcess;
        GameManager.Instance.OnSuccedStage -= SuccessStageProcess;
        GameManager.Instance.OnFailedStage -= FailedStageProcess;
    }

    #region Explore 
    private void ResetData()
    {
        chapter = 1;
        bCreate = false;
        mapNodeID = 0;
        prevNodeId = -1;
    }

    private void AcceptReward()
    {
        // 전부 클리어 했다면 최종 보상을 지급한다. 
        if (bAllCleared)
        {
            SetChapterClearReward(chapter);
            return;
        }

        // 특정 스테이지 클리어 보상
        var stage = stageReplacer.GetReplacedStageInfo(mapNodeID);
        if (stage == null)
        {
            Debug.LogWarning($"보상을 받을 스테이지 ID를 찾을 수 없습니다.");
            return;
        }

        rewardManager?.GiveStageReward(stage);
    }

    public void InitLevel()
    {
        if (bCreate == false)
        {
            bCreate = true;
            ReplaceLevel();
        }

#if UNITY_EDITOR
        // 자신이 갈 수 있는 노드 출력하기 
        var enableIds = mapReplacer.GetCanEnableNodeIds(mapNodeID);
        foreach (int id in enableIds)
        {
            Debug.Log($"Can going id : {id}");
        }
#endif
    }

    public bool EnableNode(MapNode node)
    {
        if (node == null) return false;

        return EnableNode(node.id);
    }

    public bool EnableNode(int id)
    {
        bool bEnable = false;

        // 이전에 실패한 노드가 있으면 그 노드만 강제 선택
        if (prevNodeId == mapNodeID)
            bEnable = (id == prevNodeId);
        // 정상적인 흐름 목록에 있는지 판별 
        else
            bEnable = mapReplacer.CanEnableNode(mapNodeID, id);

        //Cheat 
        if (bCheat)
            bEnable = true;

        return bEnable;
    }

    // 맵들 배치 
    private void ReplaceLevel()
    {
        MapData loadMap = SaveManager.LoadMap();
        if (loadMap != null)
        {
            mapReplacer.RestoreMap(loadMap.nodes);
            mapNodeID = loadMap.currentStageId;
        }
        else
        {
            mapReplacer.Replace();
            mapReplacer.ConnectToNode();
        }


        StageNodeData loadStage = SaveManager.LoadStageNode();
        if (loadStage != null)
        {
            stageReplacer.RestoreStages(loadStage);
        }
        else
        {
            stageReplacer.AssignStages(mapReplacer.GetLevels());
        }
    }
    // 스테이지를 만들어 둔 것을 어디에 저장하고 있어야 할 듯 
    public StageInfo GetStageInfo(int stageID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetStageInfo(stageID);
    }

    public int GetRandomStageID()
    {
        if (databaseManager == null) return -1;
        return databaseManager.GetRandomStageID(0);
    }

    public StageInfo CreateRandomStageInfo()
    {
        var stageId = databaseManager.GetRandomStageID(0);
        return GetStageInfo(stageId);
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
        if (node != null)
        {
            Debug.Log($"Current Select Node ID : {node.id}");
            prevNodeId = mapNodeID;
            mapNodeID = node.id;
        }
        var stage = GetStageInfoMatchedMapNode(node);
        GameManager.Instance.EnterStage(stage);
    }

    private void FinishStageProcess()
    {
        // 보상 지급
        AcceptReward();

        // 마지막 챕터까지 클리어 했다면 탐사 진입 전 로비로 이동시킨다. 
        if (bAllCleared)
        {
            SceneManager.LoadScene(0);
            bAllCleared = false;
            ResetData();

            return;
        }

        SceneManager.LoadScene(1);
    }

    public void SuccessStageProcess()
    {
        // 스테이지 클리어 했다면 해당 노드가 끝인지 확인
        bool bIsFinal = mapReplacer.IsFinalNode(mapNodeID);

        // 마지막이라면 챕터를 올린다. 
        if (bIsFinal)
        {
            if (++chapter < maxChapter)
                bCreate = false;
            else
                bAllCleared = true;
        }
    }

    public void FailedStageProcess()
    {
        Debug.Log($"Stage Challege Filed!..");
        mapNodeID = prevNodeId;
    }

    public void EnterTheExplorationProcess()
    {
        ResetData();

        SceneManager.LoadScene(1);
    }

    public StageInfo GetStageInfoMatchedMapNode(MapNode mapNode)
    {
        if (mapNode == null || stageReplacer == null) return null;

        return stageReplacer.GetReplacedStageInfo(mapNode.id);
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

    public void SetActiveSkills(int charId, SkillComponent skillComp)
    {
        if (skillManager == null || skillComp == null) return;
        skillManager.SetActiveSkills(charId, skillComp);
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

    #region Save

    public void OnUnloadScene(Scene scene)
    {
        SaveIfDirty();
    }

    public void SaveIfDirty()
    {
        skillTree?.SaveIfDirty();
        InventoryManager.Instance?.SaveIfDirty();
        PlayerManager.Instance?.SaveIfDirty();
    }

    public void SaveExploreMap()
    {
        // Save Map 
        {
            MapData mapData = new();
            foreach (var level in mapReplacer.GetLevels())
                foreach (var node in level)
                    mapData.nodes.Add(node);

            mapData.currentStageId = mapNodeID;
            SaveManager.SaveMap(mapData);
        }

        // Save Stage
        {
            StageNodeData stageNodeData = new();
            foreach (var pair in stageReplacer.GetNodeToStageId())
                stageNodeData.stages.Add(new MapToStage { mapNodeId = pair.Key, stageId = pair.Value });

            SaveManager.SaveStageNode(stageNodeData);
        }
    }
    #endregion
}
