using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StageInfoJson
{
    public int id;
    public int stageType;
    public int chapter;
    public string groupIds;
    public int clearRewardId;
    public int wave;
}

[System.Serializable]
public class StageInfoJsonAllData
{
    public List<StageInfoJson> stageInfoJson;
}


public class StageDataBase : DataBase
{
    [Header("Stage Data Json")]
    [SerializeField] private TextAsset stageJson;
    // 스테이지 정보 테이블 - key : id,  value : stage info 
    private Dictionary<int, StageInfo> stageInfoTable = new();
    // 스테이지 챕터별 테이블 - key : chapter,  value : 해당 챕터에 등장하는 stage id  
    private Dictionary<int, List<int>> stageChapterTable = new();
    // 보스 스테이지 테이블  - key : boss chatper, value : 해당 챕터에 등장하는 boss stage list 
    private Dictionary<int, List<int>> bossChapterTable = new();

    public override void Initialize()
    {
        InitializeStageData();
    }

    public void InitializeStageData()
    {
        if (stageJson == null) return;

        Debug.Log("Stage Database Init");

        JsonLoader.LoadJsonList<StageInfoJsonAllData, StageInfoJson, StageInfo>
          (
              stageJson,
              root => root.stageInfoJson,

              json =>
              {
                  var stage = new StageInfo
                  {
                      id = json.id,
                      type = (StageType)json.stageType,
                      groupIds = JsonLoader.ParseIntList(json.groupIds),
                      clearRewardId = json.clearRewardId,
                      chapter = json.chapter,
                      wave = json.wave
                  };
                  return stage;
              },

              stage =>
              {
                  // Stage Info 
                  stageInfoTable.TryAdd(stage.id, stage);

                  if (stage.type == StageType.Boss_Combat)
                  {
                      if (bossChapterTable.TryGetValue(stage.chapter, out List<int> bossIds))
                          bossIds.Add(stage.id);
                      else
                      {
                          bossChapterTable.Add(stage.chapter, new List<int>());
                          bossChapterTable[stage.chapter].Add(stage.id);
                      }
                  }

                  // Stage Chapter
                  if (stageChapterTable.TryGetValue(stage.chapter, out List<int> ids))
                      ids.Add(stage.id);
                  else
                  {
                      stageChapterTable.Add(stage.chapter, new List<int>());
                      stageChapterTable[stage.chapter].Add(stage.id);
                  }
              }
          );


        // Complete Message 
        Debug.Log("===================================================");
        Debug.Log($"Complete Message => stageInfoTable : {stageInfoTable.Count}, " +
            $"stageChapterTable : {stageChapterTable.Count}, " +
            $"bossChapterTable : {bossChapterTable.Count}");
    }

    public List<int> GetIdsByChapter(int chapter)
    {
        if (stageChapterTable.TryGetValue(chapter, out List<int> ids))
            return ids;
        else
            return null;
    }

    public int GetRandomStageIDByChater(int chapter)
    {
        if (stageChapterTable.TryGetValue(chapter, out var ids))
        {
            if (ids.Count == 0) return -1;

            int randID = Random.Range(0, ids.Count);
            return ids[randID];
        }

        return -1;
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (stageInfoTable.TryGetValue(stageID, out StageInfo stageInfo))
            return new StageInfo(stageInfo);
        else
            return null;
    }

    public StageInfo GetBossStageInfo(int chapter, int stageID)
    {
        if (bossChapterTable.TryGetValue(chapter, out List<int> ids))
        {
            foreach (var id in ids)
            {
                if (id == stageID)
                    return GetStageInfo(stageID);
            }
        }
        return null;
    }

    public int GetRandomBossStageId(int chapter)
    {
        if (bossChapterTable.TryGetValue(chapter, out List<int> ids))
        {
            if (ids.Count == 0) return -1;
            int randID = Random.Range(0, ids.Count);
            return ids[randID];
        }
        return -1;
    }
}
