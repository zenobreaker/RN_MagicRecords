using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ExploreManager : MonoBehaviour
{
    public ExploreState CurrentState { get; private set; } = ExploreState.NONE;

    // 탐사 단계별 이벤트
    public event Action OnExploreStart;     // 탐사 시작 시
    public event Action OnReturnToMain;     // 탐사 메인 
    public event Action<int> OnInStage;     // 특정 스테이지 선택 시
    public event Action OnStageClear;       // 스테이지 클리어 시
    public event Action OnExploreFinish;    // 전체 탐사 완료 시

    // 생성한 맵 정보를 가지고 있는 배치자 
    private MapReplacer mapReplacer;
    public MapReplacer MapReplacer { get { return mapReplacer; } }
    private StageReplacer stageReplacer;

    public int Chapter { get; private set; }
    public int MapNodeID { get; private set; }
    public bool AllStageClear => bAllCleared;

    private int maxChapter = 1;
    private int prevNodeId = -1;
    private bool bClearCurrentNode = false;

    private bool bCreate = false;
    private bool bAllCleared = false;
    private bool bFinished = false; 
    public bool IsFinish => bFinished;

    public void EnsureInitialized()
    {
        if (bCreate == false)
        {
            Debug.Log("씬에서 직접 시작됨: 자동 Init 실행");
            StartExplore(); // 기존의 맵 생성/로드 로직 실행
        }
    }
    public void ResetData()
    {
        bCreate = false; bAllCleared = false;
        Chapter = 1;
        MapNodeID = 0;
        prevNodeId = 0;
    }

    public void StartExplore()
    {
        ResetData();
        Init();

        ChangeState(ExploreState.READY);
    }

    public void Init()
    {
        if (bCreate == false)
        {
            bCreate = true;
            mapReplacer = new MapReplacer();
            stageReplacer = new StageReplacer();
            ReplaceLevel();
        }
    }

    private void ReplaceLevel()
    {
        Chapter = 1;

        MapData loadMap = SaveManager.LoadMap();
        if (loadMap != null)
        {
            mapReplacer.RestoreMap(loadMap.nodes);
            MapNodeID = loadMap.currentNodeId;
            prevNodeId = loadMap.prevNodeId;
            bClearCurrentNode = loadMap.bClear;
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

#if UNITY_EDITOR
        // 자신이 갈 수 있는 노드 출력하기 
        var enableIds = GetCanEnableNodeIds();
        foreach (int id in enableIds)
        {
            Debug.Log($"Can going id : {id}");
        }
#endif
    }

    public StageInfo GetReplacedStageInfo()
    {
        return stageReplacer?.GetReplacedStageInfo(MapNodeID);
    }

    public List<int> GetCanEnableNodeIds()
    {
        return mapReplacer?.GetCanEnableNodeIds(MapNodeID);
    }

    public bool EnableNode(MapNode node, bool bCheat = false)
    {
        if (node == null) return false;

        return EnableNode(node.id, bCheat);
    }

    public bool EnableNode(int id, bool bCheat = false)
    {
        bool bEnable = false;

        // 현재 고른 노드가 있으나 클리어 여부가 충족되지 않은 상태라면 
        // 해당 노드만 고름 
        if(MapNodeID != 0 && bClearCurrentNode == false)
        {
            bEnable = MapNodeID == id; 
        }
        // 현재 노드가 -1 or 0 이고 이전 노드가 사전에 처리된 값이 되어 있다면 
        // 이전 노드로부터 다음에 갈 수 있는 노드를 고른 것인지
        else if((MapNodeID == 0 || MapNodeID == -1) && prevNodeId != -1)
        { 
            bEnable = mapReplacer.CanEnableNode(prevNodeId, id);
        }

        //Cheat 
        if (bCheat)
            bEnable = true;

        return bEnable;
    }

    public void EnterStageByNode(MapNode node)
    {
        prevNodeId = MapNodeID = node.id; 
    }

    public void ClearStage(bool isWin)
    {
        bFinished = false; 

        if (isWin)
        {
            // 스테이지 클리어 했다면 해당 노드가 끝인지 확인
            bool bIsFinal = MapReplacer.IsFinalNode(MapNodeID);
            bClearCurrentNode = true; 

            // 마지막이라면 챕터를 올린다. 
            if (bIsFinal)
            {
                if (++Chapter < maxChapter)
                    bCreate = false;
                else
                    bAllCleared = true;
            }

            bFinished = true; 
            MapNodeID = -1; 
            ChangeState(ExploreState.STAGE_CLEAR);
        }
        else
        {
            bClearCurrentNode = false; 
            MapNodeID = prevNodeId;
        }
    }

    public StageInfo GetReplacedStageInfo(int id)
    {
        return stageReplacer.GetReplacedStageInfo(id);
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
            case ExploreState.FINISH:
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
        bClearCurrentNode = false; 
        OnInStage.Invoke(stageID);
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

    public void SaveExploreMap()
    {
        // Save Map 
        if(mapReplacer != null) 
        {
            MapData mapData = new();
            foreach (var level in mapReplacer.GetLevels())
                foreach (var node in level)
                    mapData.nodes.Add(node);
            
            mapData.prevNodeId = prevNodeId; 
            mapData.currentNodeId = MapNodeID;
            mapData.bClear = bClearCurrentNode;
            SaveManager.SaveMap(mapData);
        }

        // Save Stage
        if(stageReplacer != null) 
        {
            StageNodeData stageNodeData = new();
            foreach (var pair in stageReplacer.GetNodeToStageId())
                stageNodeData.stages.Add(new MapToStage { mapNodeId = pair.Key, stageId = pair.Value });

            SaveManager.SaveStageNode(stageNodeData);
        }
    }
}
