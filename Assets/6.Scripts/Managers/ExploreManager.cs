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


/// <summary>
//StartExplore()
// ↓
//ResetExploreProgress()
// ↓
//ResetData()
// ↓
//Init(true)
// ↓
//ReplaceLevel(true)
// ↓
//Chapter 1 생성
// ↓
//SetupIncomplete
/// </summary>

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
            Debug.Log("========== Explore Resume ==========");
            Init(false);  // 기존의 맵 생성/로드 로직 실행
            Debug.Log(
      $"[ExploreResume] " +
      $"RunStatus = {RunStatus}, " +
      $"Chapter = {Chapter}, " +
      $"MapNodeID = {MapNodeID}, " +
      $"bCreate = {bCreate}, " +
      $"stageCreated = {stageReplacer?.IsCreatedNode()}"
  );

            switch (RunStatus)
            {
                case RunStatus.SetupIncomplete:
                    Debug.Log("[ExploreResume] -> Setup UI");
                    UIManager.Instance.SafeInvoke(v =>
                    {
                        v.OpenExplorationSetupPopUp((target) =>
                        {
                            target.OpenForContinue(CurrentSetupData);
                        });
                    });

                    break;

                case RunStatus.FinalRunCleared:
                    Debug.Log("[ExploreResume] -> FinalRunCleared");
                    UIManager.Instance.SafeInvoke(v =>
                    {
                        v.OpenExploreResultPopUp();
                    });

                    break;

                case RunStatus.ChapterCleared:

                    Debug.Log(
                        "[ExploreManager] ChapterCleared 런 복구 완료"
                    );

                    break;

                case RunStatus.MidRun:

                    Debug.Log(
                        $"[ExploreManager] MidRun 이어하기 - Chapter {Chapter}, Node {MapNodeID}"
                    );

                    break;
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

    private bool TryAdvanceChapter()
    {
        // 마지막 챕터라면 다음 챕터가 없음
        if (Chapter >= maxChapter)
            return false;

        Chapter++;

        MapNodeID = 0;
        bCreate = false;

        // 다음 챕터의 맵 생성
        Init(true);

        return true;
    }

    public void Init(bool forceGenerate = false)
    {
        if (forceGenerate)
            bCreate = false;

        if (bCreate)
            return;


        bCreate = true;

        if (stageReplacer == null)
            stageReplacer = new StageReplacer();
        else
            stageReplacer.ClearStage();

        ReplaceLevel(forceGenerate);
    }

    private void ReplaceLevel(bool forceGenerate)
    {
        ExploreRunSaveData loadData = forceGenerate ? null : SaveManager.LoadExploreRun();
        //MapData loadData  = forceGenerate ? null : SaveManager.LoadMap();

        // 1. 저장 데이터가 존재하는 경우
        if (loadData != null)
        {
            RunStatus = loadData.runStatus;
            CurrentSetupData = loadData.setupData;

            // 세팅 중이었다면 맵/스테이지는 없으므로 로드 중단
            if (RunStatus == RunStatus.SetupIncomplete)
                return;

            // 챕터 보스 클리어 직후 저장된 상태
            if (RunStatus == RunStatus.ChapterCleared)
            {
                Debug.Log(" " +
                    $"[ExploreManager] Chapter {Chapter} 클리어 상태로 저장된 런"
                    + $"다음 챕터로 진행");

                // 맵과 스테이지 정상 복구 
                MapData saveMap = loadData.mapData;
                if (saveMap != null)
                {
                    Chapter = saveMap.chapter <= 0 ? 1 : saveMap.chapter;
                    BiomeName = saveMap.biomeName;
                }

                // 다음 챕터로 이동
                if (Chapter < maxChapter)
                {
                    Chapter++;
                    MapNodeID = 0;

                    stageReplacer.StartChapter(Chapter);

                    MapNodeInfo startNode =
                        stageReplacer.GetReplacedNodeInfo(MapNodeID);

                    if (startNode != null)
                        startNode.isCleared = true;

                    RunStatus = RunStatus.MidRun;

                    SaveExploreMap();

                    return;
                }

                // 혹시 마지막 챕터라면 
                RunStatus = RunStatus.FinalRunCleared;
                return;
            }

            // 일반적인 이어하기
            MapData mapData = loadData.mapData;

            if (mapData != null)
            {
                Chapter = mapData.chapter <= 0 ? 1 : Chapter;

                MapNodeID = mapData.currentNodeId;
                BiomeName = mapData.biomeName;
            }

            StageNodeData loadStage = loadData.stageNodeData;

            if (loadData != null)
            {
                StageReplacer.RestoreStages(
                    Chapter,
                    mapData,
                    loadStage
                    );

                MapNodeInfo startNode =
                    stageReplacer.GetReplacedNodeInfo(0);

                if (startNode != null)
                    startNode.isCleared = true;
            }

            return;
        }

        // 2. 저장 데이터가 없는 경우 

        // forceGenerate가 아닌데 세이브가 없다면 새 탐사 
        if (forceGenerate == false)
        {
            Chapter = 1;
            MapNodeID = 0;
        }

        stageReplacer.StartChapter(Chapter);

        MapNodeInfo starteNode =
            stageReplacer.GetReplacedNodeInfo(MapNodeID);

        if (starteNode != null)
            starteNode.isCleared = true;

        // 새 맵이 생성되었지만 아직 세팅 전
        RunStatus = RunStatus.SetupIncomplete;

        CurrentSetupData = new ExplorationSetupData();
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
        if (!SaveManager.HasSavedExploreRun())
            return RunStatus.NoSave;

        if (stageReplacer == null || stageReplacer.IsCreatedNode() == false)
            return RunStatus.SetupIncomplete;

        MapNodeInfo currentNodeInfo =
            GetReplacedNodeInfo(MapNodeID);

        bool isBossNode =
            currentNodeInfo != null &&
            currentNodeInfo.type == StageType.Boss_Combat;

        if (isBossNode && currentNodeInfo.isCleared)
        {
            if (Chapter < maxChapter)
                return RunStatus.ChapterCleared;

            return RunStatus.FinalRunCleared;
        }

        return RunStatus.MidRun;
    }

    public void ClearStage(bool isWin)
    {
        if (isWin == false)
        {
            ChangeState(ExploreState.ON_EXPLORE);
            SaveExploreMap();
            return;
        }

        // 현재 노드 클리어
        MapNodeInfo currentNodeInfo = GetReplacedNodeInfo();

        if (currentNodeInfo != null)
            currentNodeInfo.isCleared = true;

        bool bIsFianl = stageReplacer.IsFinalNode(MapNodeID);

        // 챕터 마지막 보스 
        if (bIsFianl)
        {
            // 아직 챕터가 남음
            if (Chapter < maxChapter)
            {
                RunStatus = RunStatus.ChapterCleared;

                SaveExploreMap();

                // 실제 다음 챕터 생성
                if (TryAdvanceChapter())
                {
                    RunStatus = RunStatus.MidRun;

                    SaveExploreMap();

                    ChangeState(ExploreState.STAGE_CLEAR);
                }

                return;
            }

            // 최종 챕터 보스
            bAllCleared = true;
            RunStatus = RunStatus.FinalRunCleared;

            ChangeState(ExploreState.EXPLORE_FINISH);
            SaveExploreMap();

            return;
        }

        // 일반 스테이지 클리어
        ChangeState(ExploreState.STAGE_CLEAR);

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

    // UI에서 '다음'버튼을 누를 때 임시저장
    public void SaveSetupProgress(ExplorationSetupData currentSetup)
    {
        CurrentSetupData = currentSetup;
        RunStatus = RunStatus.SetupIncomplete;
        SaveExploreMap();
    }

    public void SaveExploreMap()
    {
        if (bCreate == false || stageReplacer == null)
        {
            Debug.LogWarning("[SaveBlock] 이미 종료된 런이거나 초기화 전이므로 유령 저장을 차단합니다.");
            return;
        }

        ExploreRunSaveData saveData = new ExploreRunSaveData
        {
            runStatus = RunStatus,
            setupData = CurrentSetupData
        };

        if (RunStatus != RunStatus.SetupIncomplete)
        {
            saveData.mapData = stageReplacer.GetMapData(
                Chapter,
                MapNodeID,
                BiomeName,
                RunStatus
            );

            saveData.stageNodeData = stageReplacer.GetSaveData();
        }

        SaveManager.SaveExploreRun(saveData);
    }
}
