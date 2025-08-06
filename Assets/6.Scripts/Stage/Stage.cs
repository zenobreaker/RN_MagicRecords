using System.Collections.Generic;

[System.Serializable]
public enum NodeType
{
    None,
    Combat,
    Event,
    Shop,
    Boss_Combat,
}

[System.Serializable]
public class StageInfo
{
    public int id;

    public int chapter;

    public NodeType type;

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
    public StageInfo GetStage(int id)
    {
        StageInfo temp = new StageInfo();
        temp.id = id;
        //TODO : 스테이지 타입 정하는 로직이 필요함.

        // 스테이지를 고르는 로직이 필요한데 
        // 몬스터 같은 경우를 생각하면 어느 테이블에 몬스터가 배치된 테이블을 
        // 미리 만들어놓고 그 테이블의 id값을 가져다가 붙여넣으면 될 듯 

        CreateEnemyList(ref temp.groupIds);
        CreateRewardList(ref temp.rewardIds);

        return temp;
    }


    public void CreateEnemyList(ref List<int> groupIds)
    {
        //TODO : 스테이지 테마별 등장 몬스터 정보가 필요함
        groupIds.Clear();
        groupIds.Add(0);
    }

    public void CreateRewardList(ref List<int> rewardIds)
    {
        rewardIds.Clear();
        rewardIds.Add(0);
    }

    public void AssignStages(List<List<MapNode>> levels)
    {
        for(int level = 0; level < levels.Count; level++)
        {
            if (level == 0)
                continue;
            else if (level == levels.Count - 1)
                continue;

            for(int n = 0; n < levels[level].Count;n++)
            {
                MapNode node = levels[level][n]; 
                if(node != null)
                {
                    node.stageID = AppManager.Instance.GetRandomStageID();
                }
            }
        }
    }
}
