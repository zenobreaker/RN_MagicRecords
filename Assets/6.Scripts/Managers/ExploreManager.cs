using System;
using System.Collections.Generic;
using UnityEngine;
public enum RunStatus
{
    NoSave,          // 세이브 없음 (새 게임)
    SetupIncomplete, // 캐릭터/스킬 세팅 완료하지 않고 종료
    MidRun,          // 일반적인 이어하기 상태
    ChapterCleared,  // 챕터 보스는 깼으나 다음 챕터 이동 전 크래시
    FinalRunCleared  // 최종 보스까지 다 깼으나 최종 보상 받기 전 크래시
}


public sealed class ExploreManager : MonoBehaviour
{
    public ExploreState CurrentState { get; private set; } = ExploreState.NONE;

    public ExplorationSetupData CurrentSetupData { get; private set; } = new();

    // 탐사 단계별 이벤트
    public event Action OnExploreStart;     // 탐사 시작 시
    public event Action OnReturnToMain;     // 탐사 메인 
    public event Action<int> OnInStage;     // 특정 스테이지 선택 시
    public event Action OnStageClear;       // 스테이지 클리어 시
    public event Action OnExploreFinish;    // 전체 탐사 완료 시

    // 스테이지 배치자 
    private StageReplacer stageReplacer;
    public StageReplacer StageReplacer => stageReplacer;

    private string chapterBiomeName;
    public RunStatus RunStatus { get; private set; }

    public int Chapter { get; private set; } = 1;
    public int MapNodeID { get; private set; }
    public string BiomeName
    {
        get { return chapterBiomeName; }
        set { chapterBiomeName = value; }
    }
    public bool AllStageClear => bAllCleared;

    private bool bCreate = false;
    private bool bAllCleared = false;

    [SerializeField] private int maxChapter = 1;

    private void Awake()
    {
        stageReplacer = new StageReplacer();
    }


    // 현재 위치한 노드의 클리어 여부를 StageReplacer(진실의 원천)에게 직접 물어보는 프로퍼티
    public bool IsCurrentNodeCleared
    {
        get
        {
            MapNodeInfo currentNodeInfo = GetReplacedNodeInfo(MapNodeID);
            return currentNodeInfo != null && currentNodeInfo.isCleared;
        }
    }

    private void ResetExploreProgress()
    {
        CurrencyManager.Instance.SafeInvoke(v => v.ClearExploreCurrency());

        RewardManager.Instance.SafeInvoke(v => v.ClearPendingRewards());
    }

    public void EnsureInitialized()
    {
        if (bCreate == false)
        {
            Debug.Log("씬에서 직접 시작됨: 자동 Init 실행");
            Init(false);  // 기존의 맵 생성/로드 로직 실행

            RunStatus status = GetRunStatus();

            // 세팅하다 튕긴 경우 맵에 안들어가고 바로 셋업 UI 띄우기
            if (status == RunStatus.SetupIncomplete)
            {
                UIManager.Instance.SafeInvoke(v => v.OpenExplorationSetupPopUp((target) =>
                {
                    target.OpenForContinue(CurrentSetupData);
                }));
            }
            else if (status == RunStatus.FinalRunCleared)
            {
                UIManager.Instance.SafeInvoke(v => v.OpenExploreResultPopUp());
            }
            else if (status == RunStatus.ChapterCleared)
            {
                UIManager.Instance.SafeInvoke(v => v.ShowStageResultUI(true));

            }
        }
    }

    public void ResetData()
    {
        bCreate = false;
        bAllCleared = false;
        Chapter = 1;
        MapNodeID = 0;
    }

    public void StartExplore()
    {
        ResetExploreProgress();
        ResetData();
        Init(true);

        ChangeState(ExploreState.READY);
    }

    public void Init(bool foreceGenerate = false)
    {
        if (foreceGenerate) bCreate = false;

        if (bCreate == false)
        {
            bCreate = true;
            if (stageReplacer == null)
                stageReplacer = new StageReplacer();
            else
                stageReplacer.ClearStage();

            ReplaceLevel(foreceGenerate);
        }
    }

    private void ReplaceLevel(bool forceGenerate)
    {
        ExploreRunSaveData loadData = forceGenerate ? null : SaveManager.LoadExploreRun();
        //MapData loadData  = forceGenerate ? null : SaveManager.LoadMap();

        if (loadData != null)
        {
            RunStatus = loadData.runStatus;
            CurrentSetupData = loadData.setupData;

            // 세팅 중이었다면 맵/스테이지는 없으므로 로드 중단
            if (RunStatus == RunStatus.SetupIncomplete)
                return;

            // 맵과 스테이지 정상 복구 
            MapData mapData = loadData.mapData;
            if (mapData != null)
            {
                Chapter = mapData.chapter <= 0 ? 1 : mapData.chapter;
                MapNodeID = mapData.currentNodeId;
            }

            StageNodeData loadStage = loadData.stageNodeData;
            if (loadStage != null)
            {
                stageReplacer.RestoreStages(Chapter, mapData, loadStage);
                MapNodeInfo startNode = stageReplacer.GetReplacedNodeInfo(0);
                if (startNode != null)
                {
                    startNode.isCleared = true;
                }
            }
        }
        // 데이터가 없으므로 새로 생성 
        else
        {
            Chapter = 1;

            {
                MapNodeID = 0;
            }

            {
                stageReplacer.StartChapter(Chapter);
                MapNodeInfo startNode = stageReplacer.GetReplacedNodeInfo(MapNodeID);
                if (startNode != null)
                {
                    startNode.isCleared = true;
                }
            }

            RunStatus = RunStatus.SetupIncomplete;
            CurrentSetupData = new ExplorationSetupData();
        }

    }

    // UI에서 '다음'버튼을 누를 때 임시저장
    public void SaveSetupProgress(ExplorationSetupData currentSetup)
    {
        CurrentSetupData = currentSetup;
        RunStatus = RunStatus.SetupIncomplete;
        SaveExploreMap();
    }

    // 유저가 세팅창에서 최종 시작 버튼을 눌렀을 때 호출
    public void FinallizeSetupAndGenerateMap(ExplorationSetupData finalSetup)
    {
        CurrentSetupData = finalSetup;
        RunStatus = RunStatus.MidRun;

        MapNodeID = 0;

        stageReplacer ??= new StageReplacer(); 
        stageReplacer.StartChapter(Chapter);
        MapNodeInfo startNode = stageReplacer.GetReplacedNodeInfo(0);
        
        if (startNode != null) 
            startNode.isCleared = true;

        SaveExploreMap();
        ChangeState(ExploreState.READY);
    }

    private RunStatus GetRunStatus()
    {
        //if (!SaveManager.HasSavedMapData())
        if (!SaveManager.HasSavedExploreRun())
            return RunStatus.NoSave;

        // 맵이 생성되지 않은 단계
        if (stageReplacer.IsCreatedNode() == false)
            return RunStatus.SetupIncomplete;

        MapNodeInfo currentNodeInfo = GetReplacedNodeInfo(MapNodeID);
        bool isBossNode = currentNodeInfo != null && currentNodeInfo.type == StageType.Boss_Combat;

        if (isBossNode && currentNodeInfo.isCleared)
        {
            if (Chapter < maxChapter)
                return Chapter < maxChapter ? RunStatus.ChapterCleared : RunStatus.FinalRunCleared;
        }

        return RunStatus.MidRun;
    }

    public void ClearStage(bool isWin)
    {
        if (isWin)
        {
            // 승리 시 현재 노드의 껍데기에 클리어 처리를 해줍니다.
            MapNodeInfo currentNodeInfo = GetReplacedNodeInfo();
            if (currentNodeInfo != null)
            {
                currentNodeInfo.isCleared = true;
            }

            bool bIsFinal = stageReplacer.IsFinalNode(MapNodeID);

            if (bIsFinal)
            {
                // 챕터 처리 
                if (Chapter < maxChapter)
                {
                    Chapter++;
                    bCreate = false;
                    Init(true); // 새 챕터 강제 생성
                    ChangeState(ExploreState.STAGE_CLEAR);
                }
                // 모든 챕터 클리어 탐사 종료 처리 
                else
                {
                    bAllCleared = true;
                    ChangeState(ExploreState.EXPLORE_FINISH);
                }
            }
            else
            {
                ChangeState(ExploreState.STAGE_CLEAR);
            }
        }
        else
        {
            ChangeState(ExploreState.ON_EXPLORE);
        }

        SaveExploreMap();
    }


    public MapNodeInfo GetReplacedNodeInfo()
    {
        return stageReplacer?.GetReplacedNodeInfo(MapNodeID);
    }

    public MapNodeInfo GetReplacedNodeInfo(int id)
    {
        return stageReplacer?.GetReplacedNodeInfo(id);
    }

    public bool CanEnableNode(int targetNodeId, bool bCheat = false)
    {
        if (bCheat) return true;

        // 1. 현재 노드를 아직 못 깼다면? 
        // 오직 "지금 그 노드"만 다시 들어갈 수 있음 (이어하기/재도전)
        if (IsCurrentNodeCleared == false)
            return MapNodeID == targetNodeId;

        // 2. 현재 노드를 깼다면?
        // "현재 노드"와 연결된 "다음 노드들"만 클릭 가능
        return stageReplacer.CanEnableNode(MapNodeID, targetNodeId);
    }

    // 💡 UI 노드들이 자신을 그릴 때 매니저에게 "저 무슨 상태예요?" 하고 물어보는 함수입니다.
    public MapNodeState GetNodeState(int targetNodeId)
    {
        // 1. 플레이어가 서 있는 바로 그곳
        if (MapNodeID == targetNodeId)
        {
            return IsCurrentNodeCleared ? MapNodeState.Cleared : MapNodeState.Current;
        }

        // 2. 이미 지나온 과거 (레벨 비교)
        int currentLevel = stageReplacer.GetNodeLevel(MapNodeID);
        int targetLevel = stageReplacer.GetNodeLevel(targetNodeId);

        if (currentLevel != -1 && targetLevel < currentLevel)
        {
            return MapNodeState.Cleared;
        }

        // 3. 갈 수 있는 곳 (EnableNode 로직 활용)
        if (CanEnableNode(targetNodeId))
        {
            return MapNodeState.Selectable;
        }

        // 4. 아직 못 가는 먼 곳
        return MapNodeState.Locked;
    }

    // 화면에 떠있는 UI 노드들에게 "상태에 맞춰서 색깔 바꿔!" 라고 명령하는 함수
    public void UpdateMapUIState(UIMapReplacer uiMapReplacer)
    {
        if (uiMapReplacer == null) return;

        // UIMapReplacer가 들고 있는 UI 노드 리스트를 가져옵니다.
        List<UIMapNode> spawnedNodes = new List<UIMapNode>();
        uiMapReplacer.GetUIMapNodes(ref spawnedNodes);

        // 모든 UI 노드를 순회하면서 알맞은 옷(상태)을 입혀줍니다.
        foreach (var uiNode in spawnedNodes)
        {
            if (uiNode is UIStageMapNode stageNode)
            {
                // 두뇌(GetNodeState)에게 물어봐서 상태를 얻어온 뒤
                MapNodeState state = GetNodeState(stageNode.Node.id);

                // UI에게 적용!
                stageNode.SetState(state);
            }
        }
    }


    public void EnterStageByNode(MapNode node)
    {
        if (node == null) return;

        MapNodeID = node.id;

        ChangeState(ExploreState.IN_STAGE);

        // 💡 실제 진입은 라우터에게 위임
        MapNodeInfo nodeInfo = GetReplacedNodeInfo(MapNodeID);
        if (nodeInfo != null)
        {
            Debug.Log($"Current Chapter : {Chapter} / Stage {nodeInfo.contentId}");
            NodeRouter.EnterNode(Chapter, nodeInfo);
        }
    }

    //  유저가 보상을 확인하고 다음 챕터 이동을 수락했을 때 실행
    private  void GoToNextChapter()
    {
        if (Chapter >= maxChapter) return;
        if (AllStageClear == false) return; 

        Chapter++;
        bCreate = false;
        Init(true); // 여기서 비로소 2챕터의 새 맵이 깔립니다.

        ChangeState(ExploreState.ON_EXPLORE);
        SaveExploreMap(); // 2챕터가 시작되었다는 정보를 새롭게 저장
    }


    // 전투 이벤트 스테이지로 바로 가게 만드는 로직 
    public void StartEventCombat(EventActionType actionType, EventActionParam actionParam, int actionValue)
    {
        if (actionParam == EventActionParam.STAGE)
        {
            StageInfo combatData = AppManager.Instance.GetStageInfo(actionValue);
            GameManager.Instance.EnterStage(combatData);
        }
    }

    public void StartEventCombat(EventChoice choice)
    {
        if (choice == null) return;

        if (choice.ActionType != EventActionType.STAGE_COMBAT)
            return;

        StartEventCombat(choice.ActionType, choice.ActionParam, choice.ActionValue);
    }

    public void ChangeState(ExploreState newState, int stageID = -1)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        switch (newState)
        {
            case ExploreState.READY:
                HandleExploreStart();
                break;
            case ExploreState.ON_EXPLORE:
                HandleReturnToMain();
                break;
            case ExploreState.IN_STAGE:
                HandleInStage(stageID);
                break;
            case ExploreState.STAGE_CLEAR:
                HandleStageClear();
                break;
            case ExploreState.EXPLORE_FINISH:
                HandleExploreFinish();
                break;
        }
    }


    private void HandleExploreStart()
    {
        Debug.Log($"Explore Start ");
        OnExploreStart?.Invoke();
    }

    private void HandleReturnToMain()
    {
        Debug.Log($"Exlpore return to Main ");
        OnReturnToMain?.Invoke();
    }
    private void HandleInStage(int stageID)
    {
        OnInStage?.Invoke(stageID);
    }
    private void HandleStageClear()
    {
        Debug.Log($"Exlpore Stage Clear");
        OnStageClear?.Invoke();
        GoToNextChapter();
    }

    private void HandleExploreFinish()
    {
        Debug.Log($"Explore Finish");
        OnExploreFinish?.Invoke();
    }

    public void OnReturnedStageSelectScene()
    {
        ChangeState(ExploreState.ON_EXPLORE);
    }

    // 유령 재저장을 막기 위해 메모리 데이터까지 폭파하는 함수 
    public void PurgeCurrentRun()
    {
        Debug.Log("[ExploreManager] 런 종료 완료. 세이브 파일 파기 및 메모리 데이터를 초기화합니다.");
        SaveManager.DeleteExploreRun();

        // 1. 실제 세이브 파일 완전 삭제
        //SaveManager.DeleteMapData();
        // 필요하다면 스테이지 노드 정보 파일도 삭제 (SaveManager에 구현된 경우)
        //SaveManager.DeleteStageNodeData();

        // 2. 메모리에 들고 있던 배치 클래스들을 null로 밀어버려 유령 저장을 원천 차단합니다.
        ResetExploreProgress();
        ResetData();
        stageReplacer = null;

    }


    public void SaveExploreMap()
    {
        if (bCreate == false || stageReplacer == null)
        {
            Debug.LogWarning("[SaveBlock] 이미 종료된 런이거나 초기화 전이므로 유령 저장을 차단합니다.");
            return;
        }

        RunStatus status = GetRunStatus();
        ExploreRunSaveData saveData = new ExploreRunSaveData
        {
            runStatus = status,
            setupData = CurrentSetupData
        };

        if (saveData.runStatus != RunStatus.SetupIncomplete)
        {

            saveData.mapData = stageReplacer.GetMapData(
                Chapter, 
                MapNodeID,
                BiomeName,
                status);

            saveData.stageNodeData = stageReplacer.GetSaveData();
        }

        SaveManager.SaveExploreRun(saveData);
    }
}
