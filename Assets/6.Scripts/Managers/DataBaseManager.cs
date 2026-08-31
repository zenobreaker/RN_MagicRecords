using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class DataBaseManager : Singleton<DataBaseManager>
{
    private StageDataBase stageDataBase;
    private MonsterDataBase monsterDataBase;
    private ItemDataBase itemDataBase;
    private RewardDataBase rewardDataBase;
    private ShopDataBase shopDataBase;
    private EnhanceDataBase enhanceDataBase;
    private RecordDataBase recordDataBase;
    private EventDataBase eventDataBase;

    [SerializeField] private SO_StageIconDatabase stageIconDb;


    protected override void Awake()
    {
        base.Awake();

        if (gameObject.TryComponentInChildren<StageDataBase>(out stageDataBase))
            stageDataBase.Initialize();

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

        if (gameObject.TryComponentInChildren<RecordDataBase>(out recordDataBase))
        {
            recordDataBase.Initialize();
        }

        if (gameObject.TryComponentInChildren<EnhanceDataBase>(out enhanceDataBase))
        {
            enhanceDataBase.Initialize();
        }

        if (gameObject.TryComponentInChildren<EventDataBase>(out eventDataBase))
        {
            eventDataBase.Initialize();
        }
    }


    public GameObject GetTargetBiomeObj(int chapter, int idx)
         => stageDataBase.SafeInvoke(v => v.GetTargetBiomeObj(chapter, idx));
    public GameObject GetRandBiomeObj(int chapter)
        => stageDataBase.SafeInvoke(v => v.GetRandomBiomeObj(chapter));

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

    public int GetRandomBossStageID(int chapter)
    {
        if (stageDataBase == null) return -1;

        return stageDataBase.GetRandomBossStageId(chapter);
    }

    public StageInfo GetBossStageInfo(int chapter, int stageID)
    {
        if (stageDataBase == null) return null;

        return stageDataBase.GetBossStageInfo(chapter, stageID);
    }

    public StageInfo GetRandomBossStageInfo(int chapter)
    {
        if (stageDataBase == null) return null;

        int stageid = GetRandomBossStageID(chapter);

        return stageDataBase.GetBossStageInfo(chapter, stageid);
    }

    public Sprite GetThemeBgSptByBiome(string themeName)
    {
        if (stageDataBase == null) return null;

        return stageDataBase.GetThemeBgSptByBiome(themeName);
    }

    public MonsterData GetMonsterData(int monsterID)
    {
        return monsterDataBase.SafeInvoke(v => v.GetMonsterData(monsterID));
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
        return itemDataBase.SafeInvoke(v => v.GetEquipmentItemData(itemId));
    }

    public IngredientItem GetIngredientItem(int itemId)
    {
        return itemDataBase.SafeInvoke(v => v.GetIngredientItemData(itemId));
    }

    public CurrencyItem GetCurrencyItem(int itemId)
    {
        return itemDataBase.SafeInvoke(v => v.GetCurrencyItemData(itemId));
    }

    public CurrencyItem GetCurrencyItemByType(CurrencyType type)
    {
        return itemDataBase.SafeInvoke(v => v.GetCurrencyItemByType(type));
    }

    public RewardData GetRewardData(int rewardId)
    {
        return rewardDataBase.SafeInvoke(v => v.GetReward(rewardId));
    }

    private ClearRewardData GetClearRewardData(int clearRewardId)
    {
        return rewardDataBase.SafeInvoke(v => v.GetClearReward(clearRewardId));
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
        if (shopDataBase == null)
            return null;
        return shopDataBase.GetShopItemData(itemID);
    }

    public List<ItemData> GetShopItems(ItemCategory category)
    {
        if (shopDataBase == null)
            return null;
        return shopDataBase.GetShopItems(category);
    }

    public EnhanceLevelData GetEnhanceLevelData(int rank, int enhanceLevel)
    {
        if (enhanceDataBase == null) return null;

        return enhanceDataBase.GetEnhanceLevelData(rank, enhanceLevel);
    }

    public EnhanceStatData GetEnhanceStatData(int rank, int level)
    {
        if (enhanceDataBase == null) return null;

        return enhanceDataBase.GetEnhanceStatData(rank, level);
    }

    public List<EnhanceStatData> GetEnhanceStatDatas(int rank)
    {
        if (enhanceDataBase == null) return null;

        return enhanceDataBase.GetEnhanceStatDatas(rank);
    }

    public RecordData GetRecordData(int recordID)
    {
        if (recordDataBase == null) return null;

        return recordDataBase.GetRecordData(recordID);
    }

    public List<RecordData> GetAllRecordData()
    {
        if (recordDataBase == null) return null;

        return recordDataBase.GetAllRecordData();
    }

    public RecordData GetEmptyRecord()
    {
        if (recordDataBase == null) return null;

        return recordDataBase.GetEmptyRecord();
    }

    public List<RecordData> GetRecordDatas(RecordRarity rarity)
    {
        if (recordDataBase == null) return null;

        return recordDataBase.GetRecordDatas(rarity);
    }

    public Sprite GetStageIcon(StageType type)
    {
        return stageIconDb.SafeInvoke(v => v.GetIcon(type));
    }

    public EventInfo GetEventInfo(int eventID)
    {
        return eventDataBase.SafeInvoke(v => v.GetEventInfo(eventID));
    }

    public int GetRandomEventID(int chapter)
    {
        return eventDataBase.GetRandomEventID(chapter);
    }
}
