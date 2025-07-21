using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum StageType
{
    None, 
    Combat, 
    Event, 
    Shop,
    Boss_Combat, 
}

[System.Serializable]
public class Stage 
{
    public int id;

    public StageType type; 

    // 등장할 적 
    public List<int> enemyIds = new List<int>();

    // 등장할 보상
    public List<int> rewardIds = new List<int>();
}


public class StageReplacer
{
    public Stage GetStage(int id)
    {
        Stage temp = new Stage();
        temp.id = id;
        //TODO : 스테이지 타입 정하는 로직이 필요함.

        // 스테이지를 고르는 로직이 필요한데 
        // 몬스터 같은 경우를 생각하면 어느 테이블에 몬스터가 배치된 테이블을 
        // 미리 만들어놓고 그 테이블의 id값을 가져다가 붙여넣으면 될 듯 

        CreateEnemyList(ref temp.enemyIds);
        CreateRewardList(ref temp.rewardIds);

        return temp; 
    }


    public void CreateEnemyList(ref List<int> enemyIds)
    {
        //TODO : 스테이지 테마별 등장 몬스터 정보가 필요함
        enemyIds.Clear();
        enemyIds.Add(0);
    }

    public void CreateRewardList(ref List<int> rewardIds)
    {
        rewardIds.Clear();
        rewardIds.Add(0);
    }
}
