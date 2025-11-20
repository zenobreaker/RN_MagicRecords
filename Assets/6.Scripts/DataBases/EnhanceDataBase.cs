using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class EnhanceJsonData 
{
    public int id;
    public int rank;
    public int min;
    public int max;
    public int usegold;
    public string requires;
    public string trycost; 
}

[System.Serializable]   
public class EnhanceJosnAllData
{
    public List<EnhanceJsonData> enhanceLevel;
}

[System.Serializable]
public class EnhanceStatJsonData
{
    public int id;
    public int rank;
    public int level;
    public int flat; 
    public float percent;
}

[System.Serializable]
public class EnhanceStatJsonAllData
{
    public List<EnhanceStatJsonData> enhanceStatRule;
}

public class EnhanceStatData
{
    public int id;
    public int rank;
    public int level;
    public int flat;
    public float percent;
}

public class EnhanceLevelData
{
    public int id; 
    public int rank;
    public int min;
    public int max;
    public bool usegold;
    public List<int> requires;
    public List<int> costs; 
}

public class EnhanceDataBase : DataBase
{
    [SerializeField] private TextAsset enhanceStatJson;

    private Dictionary<int, EnhanceLevelData> enhanceLevelDatas = new(); // key : id 
    private Dictionary<int, List<int>> enhanceIDsByRank = new(); // key : rank value : id

    private Dictionary<int, List<EnhanceStatData>> enhanceStatDatasByRank = new(); // key : rank value : enhanceStatData list

    public override void Initialize()
    {
        InitializeEnhanceData();
        InitializeEnhnaceRuleData();
    }

    private void InitializeEnhnaceRuleData()
    {
        JsonLoader.LoadJsonList<EnhanceStatJsonAllData, EnhanceStatJsonData, EnhanceStatData>
            (
                enhanceStatJson,
                root => root.enhanceStatRule,
                enhanceStatJsonData =>
                {
                    EnhanceStatData enhanceStatInfo = new EnhanceStatData();
                    enhanceStatInfo.id = enhanceStatJsonData.id;
                    enhanceStatInfo.rank = enhanceStatJsonData.rank;
                    enhanceStatInfo.level = enhanceStatJsonData.level;
                    enhanceStatInfo.flat = enhanceStatJsonData.flat;
                    enhanceStatInfo.percent = enhanceStatJsonData.percent;
                    return enhanceStatInfo;
                },
                enhanceStatInfo =>
                {
                    enhanceStatDatasByRank.TryGetValue(enhanceStatInfo.rank, out List<EnhanceStatData> statList);
                    if (statList == null)
                    {
                        statList = new List<EnhanceStatData>();
                        enhanceStatDatasByRank[enhanceStatInfo.rank] = statList;
                    }
                    statList.Add(enhanceStatInfo);
                }
            );

        Debug.Log("===================================================");
    }

    private void InitializeEnhanceData()
    {
        Debug.Log("Enhance Database Init");

        JsonLoader.LoadJsonList<EnhanceJosnAllData, EnhanceJsonData, EnhanceLevelData>
          (
            jsonAsset,
            root => root.enhanceLevel,

            enhanceJsonData => 
            {
                EnhanceLevelData enhanceInfo = new EnhanceLevelData();
                enhanceInfo.id = enhanceJsonData.id;
                enhanceInfo.rank = enhanceJsonData.rank;
                enhanceInfo.min = enhanceJsonData.min;
                enhanceInfo.max = enhanceJsonData.max;
                enhanceInfo.usegold = enhanceJsonData.usegold > 0;
                // Parse requires
                enhanceInfo.requires = new List<int>();
                if (!string.IsNullOrEmpty(enhanceJsonData.requires))
                {
                    string[] requiresStr = enhanceJsonData.requires.Split(',');
                    foreach (var req in requiresStr)
                    {
                        if (int.TryParse(req, out int reqId))
                        {
                            enhanceInfo.requires.Add(reqId);
                        }
                    }
                }
                // Parse costs
                enhanceInfo.costs = new List<int>();
                if (!string.IsNullOrEmpty(enhanceJsonData.trycost))
                {
                    string[] costsStr = enhanceJsonData.trycost.Split(',');
                    foreach (var cost in costsStr)
                    {
                        if (int.TryParse(cost, out int costValue))
                        {
                            enhanceInfo.costs.Add(costValue);
                        }
                    }
                }
                return enhanceInfo;
            },

            enhanceInfo => 
            {
                enhanceIDsByRank.TryGetValue(enhanceInfo.rank, out List<int> ids);
                if (ids == null)
                {
                    ids = new List<int>();
                    enhanceIDsByRank[enhanceInfo.rank] = ids;
                }
                ids.Add(enhanceInfo.id);

                enhanceLevelDatas.TryAdd(enhanceInfo.id, enhanceInfo);
            }
        );

        // Complete Message 
        Debug.Log("===================================================");
        Debug.Log($"Complete Message => enhanceLevelDatas : {enhanceLevelDatas.Count}");
    }

   
    public EnhanceLevelData GetEnhanceLevelData(int rank, int enhanceLevel)
    {

        if (enhanceIDsByRank.TryGetValue(rank, out List<int> ids) == false)
            return null; 

        foreach(var id in ids)
        {
            if (enhanceLevelDatas[id].min <= enhanceLevel && enhanceLevelDatas[id].max >= enhanceLevel)
            {
                return enhanceLevelDatas[id];
            }
        }

        return null;
    }

    public EnhanceStatData GetEnhanceStatData(int rank, int level)
    {
        enhanceStatDatasByRank.TryGetValue(rank, out List<EnhanceStatData> statList);

        if (statList == null)
            return null;

        foreach(var stat in statList)
        {
            if(stat.level == level)
            {
                return stat;
            }
        }

        return null;
    }

    public List<EnhanceStatData> GetEnhanceStatDatas(int rank)
    {
        enhanceStatDatasByRank.TryGetValue(rank, out List<EnhanceStatData> statList);
        if (statList == null)
            return null;
        return statList; 
    }
}
