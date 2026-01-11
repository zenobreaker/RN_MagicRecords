using JetBrains.Annotations;
using System.Collections.Generic;

[System.Serializable]
public class StageInfo
{
    public int id;
    public int chapter;
    public StageType type;

    // 등장할 적 
    public List<int> groupIds = new List<int>();

    // 등장할 보상
    public int clearRewardId;

    public int wave = 0;

    public int mapID;

    public bool bIsCleared = false;
    public bool bIsOpened = false;


    public StageInfo(StageInfo other)
    {
        id = other.id;
        chapter = other.chapter;
        type = other.type; 
        groupIds = new List<int>(other.groupIds);
        clearRewardId = other.clearRewardId;
        wave = other.wave;
        bIsCleared = other.bIsCleared;
        bIsOpened = other.bIsOpened;
    }

    public StageInfo() { }

    public override string ToString()
    {
        return chapter.ToString() + "-" + id.ToString();
    }
}

public class StageReplacer
{
    private Dictionary<int, StageInfo> nodeToStage = new(); // key : node id value : stage
    private Dictionary<int, int> nodeIdToStageId = new();
    public void AssignStages(List<List<MapNode>> levels)
    {
        nodeToStage.Clear();
        nodeIdToStageId.Clear();

        for (int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
                continue;
            bool isLastLevel = (level == levels.Count - 1);

            foreach(var node in levels[level])
            {
                StageInfo stageInfo;

                if (isLastLevel)
                {
                    // 보스용 스테이지 풀에서 랜덤추출 
                    stageInfo = AppManager.Instance.CreateRandomBossStageInfo();
                }
                else
                    stageInfo = AppManager.Instance.CreateRandomStageInfo();

                nodeToStage[node.id] = stageInfo;
                nodeIdToStageId[node.id] = stageInfo.id;
            }
        }
    }

    public void RestoreStages(StageNodeData stageNodeData)
    {
        nodeToStage.Clear();
        nodeIdToStageId.Clear();

        foreach (var stage in stageNodeData.stages)
        {
            var stageInfo = AppManager.Instance.GetStageInfo(stage.stageId);
            nodeToStage[stage.mapNodeId] = stageInfo;
            nodeIdToStageId[stage.mapNodeId] = stage.stageId;
        }
    }

    public Dictionary<int, int> GetNodeToStageId() => nodeIdToStageId;

    public int GetStageIdByNodeId(int nodeId) => nodeToStage.TryGetValue(nodeId, out var stageInfo) ? stageInfo.id: -1;
    public StageInfo GetReplacedStageInfo(int nodeId) => nodeToStage.TryGetValue(nodeId, out var stageInfo) ? stageInfo : null;
}
