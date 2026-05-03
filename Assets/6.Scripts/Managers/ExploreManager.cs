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

    private string chapterBiomeName;

    public int Chapter { get; private set; }
    public int MapNodeID { get; private set; }
    public string BiomeName
    {
        get { return chapterBiomeName; }
        set { chapterBiomeName = value; }
    }
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

        bClearCurrentNode = false;
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

            MapNodeID = 0;
            prevNodeId = 0;
            bClearCurrentNode = true;
        }


        StageNodeData loadStage = SaveManager.LoadStageNode();
        stageReplacer.StartChapter(Chapter);
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
        if (bCheat) return true;

        if (MapNodeID == 0)
        {
            return mapReplacer.CanEnableNode(0, id);
        }

        // 1. 현재 노드를 아직 못 깼다면? 
        // 오직 "지금 그 노드"만 다시 들어갈 수 있음 (이어하기/재도전)
        if (bClearCurrentNode == false)
        {
            return MapNodeID == id;
        }

        // 2. 현재 노드를 깼다면?
        // "현재 노드"와 연결된 "다음 노드들"만 클릭 가능
        return mapReplacer.CanEnableNode(MapNodeID, id);
    }

    // 💡 UI 노드들이 자신을 그릴 때 매니저에게 "저 무슨 상태예요?" 하고 물어보는 함수입니다.
    public MapNodeState GetNodeState(int targetNodeId)
    {
        // 1. 플레이어가 서 있는 바로 그곳
        if (MapNodeID == targetNodeId)
        {
            return bClearCurrentNode ? MapNodeState.Cleared : MapNodeState.Current;
        }

        // 2. 이미 지나온 과거 (레벨 비교)
        int currentLevel = mapReplacer.GetNodeLevel(MapNodeID);
        int targetLevel = mapReplacer.GetNodeLevel(targetNodeId);

        if (currentLevel != -1 && targetLevel < currentLevel)
        {
            return MapNodeState.Cleared;
        }

        // 3. 갈 수 있는 곳 (EnableNode 로직 활용)
        if (EnableNode(targetNodeId))
        {
            return MapNodeState.Selectable;
        }

        // 4. 아직 못 가는 먼 곳
        return MapNodeState.Locked;
    }
    // =======================================================
    // 💡 2. 화면에 떠있는 UI 노드들에게 "상태에 맞춰서 색깔 바꿔!" 라고 명령하는 함수
    // =======================================================
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
        MapNodeID = node.id;
        bClearCurrentNode = false;
    }

    public void ClearStage(bool isWin)
    {
        bFinished = false;

        if (isWin)
        {
            // 스테이지 클리어 했다면 해당 노드가 끝인지 확인
            bool bIsFinal = MapReplacer.IsFinalNode(MapNodeID);
            bClearCurrentNode = true;

            prevNodeId = MapNodeID;

            // 마지막이라면 챕터를 올린다. 
            if (bIsFinal)
            {
                if (++Chapter < maxChapter)
                    bCreate = false;
                else
                    bAllCleared = true;
            }

            bFinished = true;
            ChangeState(ExploreState.STAGE_CLEAR);
        }
        else
        {
            bClearCurrentNode = false;
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
        if (mapReplacer != null)
        {
            MapData mapData = new();
            foreach (var level in mapReplacer.GetLevels())
                foreach (var node in level)
                    mapData.nodes.Add(node);

            mapData.prevNodeId = prevNodeId;
            mapData.currentNodeId = MapNodeID;
            mapData.bClear = bClearCurrentNode;

            mapData.biomeName = BiomeName;

            SaveManager.SaveMap(mapData);
        }

        // Save Stage
        if (stageReplacer != null)
        {
            StageNodeData stageNodeData = new();
            foreach (var pair in stageReplacer.GetNodeToStage())
            {
                int nodeId = pair.Key;
                StageInfo stageInfo = pair.Value;

                stageNodeData.stages.Add(new MapToStage
                {
                    mapNodeId = nodeId,
                    stageId = stageInfo.id,
                    biomeName = stageInfo.biome,
                    mapIndex = stageInfo.mapIndex
                });
            }

            SaveManager.SaveStageNode(stageNodeData);
        }
    }
}
