using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DataBaseManager : MonoBehaviour
{
    private StageDataBase stageDataBase;
    private MonsterDataBase monsterDataBase;
    private ItemDataBase itemDataBase;
    private RewardDataBase rewardDataBase;
    private ShopDataBase shopDataBase;
    private EnhanceDataBase enhanceDataBase;


    private void Awake()
    {
        if (gameObject.TryComponentInChildren<StageDataBase>(out stageDataBase))
            stageDataBase.InitializeStageData();

        if (gameObject.TryComponentInChildren<MonsterDataBase>(out monsterDataBase))
            monsterDataBase.InitializeData();

        if (gameObject.TryComponentInChildren<ItemDataBase>(out itemDataBase))
            itemDataBase.Initialize();

        if (gameObject.TryComponentInChildren<RewardDataBase>(out rewardDataBase))
            rewardDataBase.Initialize();

        if (gameObject.TryComponentInChildren<ShopDataBase>(out shopDataBase))
        {
            shopDataBase.Initialize();
            shopDataBase.Initialize_Lookup(itemDataBase);
        }

        if (gameObject.TryComponentInChildren<EnhanceDataBase>(out enhanceDataBase))
        {
            enhanceDataBase.Initialize();
        }
    }

    public int GetRandomStageID(int chapter)
    {
        if (stageDataBase == null) return -1;

        return stageDataBase.GetRandomStageIDByChater(chapter);
    }

    public StageInfo GetStageInfo(int stageID)
    {
        if (stageDataBase == null) return null;

        return stageDataBase.GetStageInfo(stageID);
    }

    public MonsterGroupData GetMonsterGroupData(int groupID)
    {
        if (monsterDataBase == null) return null;

        return monsterDataBase.GetMonsterGroupData(groupID);
    }

    public MonsterStatData GetMonsterStatData(int monsterID)
    {
        if (monsterDataBase == null) return null;
        return monsterDataBase.GetMonsterStatData(monsterID);
    }

    public EquipmentItem GetEquipmentItem(int itemId)
    {
        return itemDataBase?.GetEquipmentItemData(itemId);
    }

    public IngredientItem GetIngredientItem(int itemId)
    {
        return itemDataBase?.GetIngredientItemData(itemId);
    }

    public CurrencyItem GetCurrencyItem(int itemId)
    {
        return itemDataBase?.GetCurrencyItemData(itemId);
    }

    public CurrencyItem GetCurrencyItemByType(CurrencyType type)
    {
        return itemDataBase?.GetCurrencyItemByType(type);
    }

    public RewardData GetRewardData(int rewardId)
    {
        return rewardDataBase?.GetReward(rewardId);
    }

    private ClearRewardData GetClearRewardData(int clearRewardId)
    {
        return rewardDataBase?.GetClearReward(clearRewardId);
    }

    public ClearRewardData GetStageClearReward(int clearedStageId)
    {
        return GetClearRewardData(clearedStageId);
    }

    public ClearRewardData GetChapterClearReward(int clearedChapter)
    {
        // 해당 값으로 하드 코딩하여 처리한다.
        int clearId = 0;
        if (clearedChapter == 1)
        {
            clearId = 1000;
        }
        else if (clearedChapter == 2)
        {
            clearId = 2000;
        }
        else if (clearedChapter == 3)
        {
            clearId = 3000;
        }

        return GetClearRewardData(clearId);
    }

    public ShopItem GetShopItem(int itemID)
    {
        return shopDataBase?.GetShopItemData(itemID);
    }

    public List<ItemData> GetShopItems(ItemCategory category)
    {
        return shopDataBase?.GetShopItems(category);
    }

    public EnhanceLevelData GetEnhanceLevelData(int rank, int enhanceLevel)
    {
        return enhanceDataBase?.GetEnhanceLevelData(rank, enhanceLevel);
    }

    public EnhanceStatData GetEnhanceStatData(int rank, int level)
    {
        return enhanceDataBase?.GetEnhanceStatData(rank, level);
    }

    public List<EnhanceStatData> GetEnhanceStatDatas(int rank)
    {
        return enhanceDataBase?.GetEnhanceStatDatas(rank);
    }
}
