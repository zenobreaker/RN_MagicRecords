using System.Collections.Generic;
using UnityEngine;

public class StageReplacer
{
    private int currentChapter; // 스테이지 ID를 뽑기 위한 챕터 기억용 
    private NodeReplacer nodeReplacer;
    private Dictionary<int, MapNodeInfo> nodeInfoDict = new(); // key : node id value : stage
    private float eventChance;

    public StageReplacer()
    {
        nodeReplacer = new NodeReplacer(); 
    }

    public void StartChapter(int chapter,
        float eventChance = 0.25f,
        float width = 0.0f,
        float height = 0.0f)
    {
        currentChapter = chapter;
        this.eventChance = eventChance;
        nodeReplacer ??= new NodeReplacer();
        nodeReplacer?.ClearMap();
        nodeReplacer?.GenerateNodeMap(width, height);

        AssignStages();
    }

    public void ClearStage()
    {
        currentChapter = 0;
        nodeReplacer.ClearMap();
    }

    public bool IsCreatedNode()
    {
        return nodeReplacer != null && nodeReplacer.GetLevels().Count > 0;
    }


    public void RestoreStages(int chapter, MapData mapData, StageNodeData stageNodeData)
    {
        currentChapter = chapter;
        if(mapData != null)
        {
            nodeReplacer?.RestoreMap(mapData.nodes); 
        }

        nodeInfoDict.Clear();
        nodeInfoDict[0] = new MapNodeInfo { nodeId = 0, type = StageType.None, contentId = 0, mapIndex = 0 };

        foreach (MapNodeInfo savedInfo in stageNodeData.nodeInfos)
        {
            if (savedInfo.contentId == 0 && savedInfo.nodeId == 0) continue;

            // 저장된 껍데기를 깊은 복사(Copy)해서 딕셔너리에 연결!
            nodeInfoDict[savedInfo.nodeId] = savedInfo.Copy();
        }
    }

    public Dictionary<int, MapNodeInfo> GetNodeToInfo() => nodeInfoDict;

    public List<List<MapNode>> GetLevels()
    {
        return nodeReplacer.GetLevels();
    }

    public MapNodeInfo GetNodeInfo(int nodeId)
    {
        if (nodeInfoDict.TryGetValue(nodeId, out var info))
            return info;

        return null;
    }

    public MapNodeInfo GetReplacedNodeInfo(int nodeId)
    {
        if (nodeInfoDict.TryGetValue(nodeId, out var info))
            return info;
        else
            return new MapNodeInfo { nodeId = nodeId, type = StageType.None };
    }

    public bool CanEnableNode(int curr,  int target)
    {
        return nodeReplacer != null && nodeReplacer.CanEnableNode(curr, target);
    }

    public int GetNodeLevel(int nodeId)
    {
        return nodeReplacer.GetNodeLevel(nodeId);
    }

    public float GetTotalHorizontalSpacing()
    {
        return nodeReplacer?.GetTotalHorizontalSpacing() ?? 0f;
    }

    public float GetTotalVerticalSpacing()
    {
        return nodeReplacer?.GetTotalVerticalSpacing() ?? 0f;
    }

    public bool IsFinalNode(int nodeId)
    {
        return nodeReplacer != null && nodeReplacer.IsFinalNode(nodeId);
    }

    public StageNodeData GetSaveData()
    {
        StageNodeData data = new();

        foreach (var pair in nodeInfoDict)
        {
            data.nodeInfos.Add(pair.Value);
        }

        return data;
    }

    public MapData GetMapData(
    int chapter,
    int currentNodeId,
    string biomeName,
    RunStatus runStatus)
    {
        MapData data = new()
        {
            chapter = chapter,
            currentNodeId = currentNodeId,
            biomeName = biomeName,
            runStatus = runStatus
        };

        foreach (var level in nodeReplacer.GetLevels())
        {
            foreach (var node in level)
            {
                data.nodes.Add(node);
            }
        }

        return data;
    }

    private void AssignStages()
    {
        Debug.Assert(DataBaseManager.Instance != null, $"DataBaseManager is Null");

        var levels = nodeReplacer.GetLevels();
        
        for (int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
            {
                nodeInfoDict[0] = new MapNodeInfo
                {
                    nodeId = 0,
                    type = StageType.None,
                    contentId = 0,
                    mapIndex = 0
                }; // 시작 노드
                continue;
            }

            bool isLastLevel = (level == levels.Count - 1);

            foreach (var node in levels[level])
            {
                MapNodeInfo info = new MapNodeInfo();
                info.nodeId = node.id;

                if (isLastLevel)
                {
                    // 보스용 스테이지 풀에서 랜덤추출 
                    info.type = StageType.Boss_Combat;
                    StageInfo bossStage 
                        = DataBaseManager.Instance.
                        SafeInvoke(v=>v.GetRandomBossStageInfo(currentChapter));
                    if (bossStage == null) continue;

                    info.contentId = bossStage.id;
                    info.mapIndex = -1; // 보스 전용 맵
                    info.clearRewardId = bossStage.clearRewardId;
                }
                else
                {
                    // 이벤트 vs 전투 분기
                    if (level > 1 && UnityEngine.Random.value < eventChance)
                    {
                        info.type = StageType.Event;
                        info.contentId = DataBaseManager.Instance.GetRandomEventID(currentChapter); ;
                        info.mapIndex = -1;
                    }
                    else
                    {
                        info.type = StageType.Combat;
                        int randid = DataBaseManager.Instance.GetRandomStageID(currentChapter);
                        StageInfo stage = DataBaseManager.Instance.GetStageInfo(randid);
                        if (stage == null) continue;

                        info.contentId = stage.id;
                        info.mapIndex = stage.mapIndex;
                        info.clearRewardId = stage.clearRewardId;
                    }
                }

                nodeInfoDict[node.id] = info;
            }
        }
    }
}