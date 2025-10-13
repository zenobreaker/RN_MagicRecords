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
    public List<int> rewardIds = new List<int>();

    public int wave = 0;

    public int mapID;

    public bool bIsCleared = false;
    public bool bIsOpened = false;

    public override string ToString()
    {
        return chapter.ToString() + "-" + id.ToString();
    }
}


public class StageReplacer
{
    private Dictionary<int, int> nodeToStage = new(); // key : node id value : stage id 

    public void AssignStages(List<List<MapNode>> levels)
    {
        nodeToStage.Clear(); 

        for(int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
                continue;
            else if (level == levels.Count - 1)
            {
                MapNode node = levels[level][0];
                if(node != null)
                {
                    //TODO : 임시 배치 - 보스스테이지를 배치해야함
                    node.stageID = AppManager.Instance.GetRandomStageID();
                    continue;;
                }
            }

            foreach(var node in levels[level])
            { 
                node.stageID = AppManager.Instance.GetRandomStageID();
                nodeToStage[node.id] = node.stageID;
            }
        }
    }

    public int GetStageIdByNodeId(int nodeId) => nodeToStage.TryGetValue(nodeId, out int stageId) ? stageId : -1;
}
