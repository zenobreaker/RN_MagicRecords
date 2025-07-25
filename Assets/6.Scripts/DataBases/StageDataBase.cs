using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StageInfoJson
{
    public int id;
    public int stageType;
    public int chapter;
    public string groupIds;
    public string rewardIDs;
    public int wave;
}

[System.Serializable]
public class StageInfoJsonAllData
{
    public List<StageInfoJson> stageInfoJson;
}


public class StageDataBase : MonoBehaviour
{
    [Header("Stage Data Json")]
    [SerializeField] private TextAsset stageJson;
    [SerializeField] private Dictionary<int, StageInfo> stageInfoTable = new();

    [SerializeField] private Dictionary<int, List<int>> stageChapterTable = new(); 

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
                      type = (NodeType)json.stageType,
                      groupIds = JsonLoader.ParseIntList(json.groupIds),
                      rewardIds =  JsonLoader.ParseIntList(json.rewardIDs),
                      wave = json.wave
                  };
                  return stage;
              },

              stage =>
              {
                  // Stage Info 
                  stageInfoTable.TryAdd(stage.id, stage);

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
            $"stageChapterTable : {stageChapterTable.Count} ");
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
            return stageInfo;
        else
            return null;
    }
}
