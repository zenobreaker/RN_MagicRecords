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
    private Dictionary<int, StageInfo> nodeToStage = new(); // key : node id value : stage id 

    public void AssignStages(List<List<MapNode>> levels)
    {
        nodeToStage.Clear(); 

        for(int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
                continue;

            // TODO : 보스 스테이지는 따로 처리해야 할 수 있다.
            foreach(var node in levels[level])
            {
                var stageInfo = AppManager.Instance.CreateRandomStageInfo();
                nodeToStage[node.id] = stageInfo;
            }
        }
    }

    public int GetStageIdByNodeId(int nodeId) => nodeToStage.TryGetValue(nodeId, out var stageInfo) ? stageInfo.id: -1;
    public StageInfo GetReplacedStageInfo(int nodeId) => nodeToStage.TryGetValue(nodeId, out var stageInfo) ? stageInfo : null;
}
