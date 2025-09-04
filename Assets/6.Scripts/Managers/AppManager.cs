using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AppManager
    : Singleton<AppManager>
{
    private DataBaseManager databaseManager;
    private SkillManager skillManager; 

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
    private List<int> enableIds; // 갈 수 있는 레벨 
    private bool bAllCleared = false;

    protected override void Awake()
    {
        base.Awake();

        databaseManager = GetComponent<DataBaseManager>();
        skillManager = GetComponent<SkillManager>();

        mapReplacer = new MapReplacer();
        stageReplacer = new StageReplacer();
    }

    protected override void SyncDataFromSingleton()
    {
        // 기존 싱글톤 인스턴스가 자기 자신이 아니면
        if (Instance != this)
        {
            skillManager = Instance.skillManager;
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

            enableIds = new List<int>(Instance.enableIds ?? new List<int>());
            bAllCleared = Instance.bAllCleared;
        }
    }

    private void Start()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnFinishStage += FinishStageProcess;
        GameManager.Instance.OnSuccedStage += SuccessStageProcess;
        GameManager.Instance.OnFailedStage += FailedStageProcess;
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
        enableIds?.Clear();
        chapter = 1;
        bCreate = false;
        mapNodeID = 0;
        prevNodeId = -1;
    }

    public void InitLevel()
    {
        if (bCreate == false)
        {
            bCreate = true;
            ReplaceLevel();
        }

        // 자신이 갈 수 있는 노드 출력하기 
        enableIds = mapReplacer.GetCanEnableNodeIds(mapNodeID);
        foreach (int id in enableIds)
        {
            Debug.Log($"Can going id : {id}");
        }
    }

    public List<List<MapNode>> GetLevels()
    {
        return mapReplacer.GetLevels();
    }

    public bool EnableNode(int id)
    {
        bool bEnable = false;

        // 이전에 실패하ㄴ 노드가 있으면 그 노드만 강제 선택
        if (prevNodeId == mapNodeID)
            bEnable = (id == prevNodeId);
        // 정상적인 흐름 목록에 있는지 판별 
        else 
            bEnable = enableIds != null && enableIds.Contains(id);

        //Cheat 
        if (bCheat)
            bEnable = true;

        return bEnable;
    }

    // 맵들 배치 
    private void ReplaceLevel()
    {
        mapReplacer.Replace();
        mapReplacer.ConnectToNode();
        stageReplacer.AssignStages(mapReplacer.GetLevels());
    }

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

    public void EnterStageByNode(MapNode node, StageInfo stageInfo)
    {
        if (node != null)
        {
            Debug.Log($"Current Select Node ID : {node.id}");
            prevNodeId = mapNodeID;
            mapNodeID = node.id;
        }

        GameManager.Instance.EnterStage(stageInfo);
    }

    private void FinishStageProcess()
    {
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
    #endregion

    #region Skill 
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

    public void SetActiveSkills(int charId, SkillComponent skillComp)
    {
        if(skillManager == null  || skillComp == null) return;
        skillManager.SetActiveSkills(charId, skillComp);
    }

    #endregion

    #region Database
    public EquipmentItem GetEquipmentItem(int itemid)
    {
        return databaseManager?.GetEquipmentItem(itemid);
    }
    #endregion
}
