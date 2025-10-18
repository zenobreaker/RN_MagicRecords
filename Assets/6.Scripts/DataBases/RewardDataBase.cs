using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RewardJsonData
{
    public int rewardID;
    public int type;
    public int itemID;
    public int amount;
    public int weight;
    public int range; 
}

[System.Serializable]
public class RewardJsonAllData
{
    public List<RewardJsonData> rewardJsonData;
}

public class RewardData
{
    public int id;
    public ItemCategory type;
    public int itemId;
    public int amount; 
    public int weight;
    public int range;
}

[System.Serializable]
public class ClearRewardJsonData
{
    public int id;
    public string rewardIDList;
    public string viewItemIDList; 
}

[System.Serializable]
public class ClearRewardAllData
{
    public List<ClearRewardJsonData> clearRewardJsonData;
}

public class ClearRewardData
{
    public int id;
    public List<int> rewardIds = new();
    public List<int> viewItemIDList = new();
}


public class RewardDataBase : DataBase
{
    [SerializeField] private TextAsset clearRewardAsset;

    [SerializeField] private Dictionary<int, RewardData> rewards = new();
    [SerializeField] private Dictionary<int, ClearRewardData> clearRewards = new();

    public override void Initialize()
    {
        if (jsonAsset == null)
            return;

        Debug.Log("Reward Database Init");

        JsonLoader.LoadJsonList<RewardJsonAllData, RewardJsonData, RewardData>
        (
            jsonAsset,
            root => root.rewardJsonData,

            json =>
            {
                var rewardData = new RewardData();
                rewardData.id = json.rewardID;
                rewardData.type = (ItemCategory)json.type;
                rewardData.itemId = json.itemID;
                rewardData.amount = json.amount;
                rewardData.weight = json.weight;
                return rewardData;
            },

            reward =>
            {
                rewards.TryAdd(reward.id, reward);
            }
        );

        Debug.Log("===================================================");
        Debug.Log($"Complete Message => rewards : {rewards.Count}");

        JsonLoader.LoadJsonList<ClearRewardAllData, ClearRewardJsonData, ClearRewardData>
            (
                clearRewardAsset,
                root => root.clearRewardJsonData,

                json =>
                {
                    var clearRewardData = new ClearRewardData();
                    clearRewardData.id = json.id;
                    foreach (string id in json.rewardIDList.Split(','))
                    {
                        if(!string.IsNullOrEmpty(id))
                            clearRewardData.rewardIds.Add(int.Parse(id));
                    }

                    foreach(string id in json.viewItemIDList.Split(','))
                    {
                        if (!string.IsNullOrEmpty(id))
                            clearRewardData.viewItemIDList.Add(int.Parse(id));
                    }

                    return clearRewardData;
                },

                clearReward=>
                {
                    clearRewards.TryAdd(clearReward.id, clearReward);
                }
            );

        Debug.Log("===================================================");
        Debug.Log($"Complete Message => clear Rewards : {clearRewards.Count}");

    }

    public RewardData GetReward(int rewardId)
    {
        return rewards.TryGetValue(rewardId, out var rewardData) ? rewardData : null;
    }

    public ClearRewardData GetClearReward(int clearRewardId)
    {
        return clearRewards.TryGetValue(clearRewardId, out var clearReward) ? clearReward : null;
    }
}
