using System.Collections.Generic;
using UnityEngine;

public class AppManager 
    : Singleton<AppManager>
{
    DataBaseManager databaseManager; 

    // 생성한 맵 정보를 가지고 있는 배치자 
    private MapReplacer mapReplacer;
    public MapReplacer MapReplacer { get { return mapReplacer; } }
    private StageReplacer stageReplacer;
    private bool bCreate = false; 

    protected override void Awake()
    {
        base.Awake();
        databaseManager = GetComponent<DataBaseManager>();

        mapReplacer = new MapReplacer();
        stageReplacer = new StageReplacer();
    }

    public void InitLevel()
    {
        if(bCreate  == false)
        {
            bCreate = true;
            ReplaceLevel();
        }
    }

    public List<List<MapNode>> GetLevels()
    {
        return mapReplacer.GetLevels();
    }

    // 맵들 배치 
    private void ReplaceLevel()
    {
        mapReplacer.Replace();
        mapReplacer.ConnectToNode();
        stageReplacer.AssignStages(mapReplacer.GetLevels());
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetStageInfo(stageID);
    }

    public int GetRandomStageID()
    {
        if (databaseManager == null) return -1;
        return databaseManager.GetRandomStageID(0); 
    }

    public MonsterGroupData GetGroupData(int groupID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetMonsterGroupData(groupID);
    }

    public MonsterStatData GetMonsterStatData(int monsterID)
    {
        if (databaseManager == null) return null;
        return databaseManager.GetMonsterStatData(monsterID);
    }

}
