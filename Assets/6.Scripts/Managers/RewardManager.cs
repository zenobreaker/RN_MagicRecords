using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public interface IReward
{
    string Title { get; }
    string Description { get; }
    Sprite Icon { get; }
    int Amount { get; }

    void Receive();
}

public class ItemReward : IReward
{
    private readonly ItemData itemData;
    private int amount;

    public string Title =>
          IsStackable()
              ? $"{itemData.name} x{amount}"
              : itemData.name;

    public string Description => itemData.description;
    public Sprite Icon => itemData.Icon;

    public int Amount => amount;

    public ItemReward(ItemData itemData, int amount)
    {
        this.itemData = itemData;
        this.amount = amount;
    }

    private bool IsStackable()
    {
        return itemData.category != ItemCategory.EQUIPMENT;
    }

    public void Receive()
    {
        ItemData copy = itemData.Copy();
        copy.SetCount(amount);

        InventoryManager.Instance.SafeInvoke(
            v => v.AddItem(copy));
    }
    public void AddAmount(int value)
    {
        amount += value;
    }
}

public class RecordReward : IReward
{
    private readonly RecordRewardMode mode;

    private readonly int recordId;
    private readonly RecordRarity rarity;

    public string Title => "레코드";
    public string Description => "레코드를 획득합니다.";

    public Sprite Icon
    {
        get
        {
            switch (mode)
            {
                case RecordRewardMode.RandomAll:
                    return ResourceManager.Instance.SafeInvoke(v =>
                        v.GetSprite("RewardIcons/random_record"));
                case RecordRewardMode.RandomByRarity:
                    return ResourceManager.Instance.SafeInvoke(v =>
                        v.GetSprite(GetPathByRandom()));
                case RecordRewardMode.FixedRecord:
                    return ResourceManager.Instance.SafeInvoke(v =>
                        v.GetSprite(GetPathByRandom()));
            }

            return null;
        }
    }

    public int Amount => 1;

    public RecordReward(
        RecordRewardMode mode,
        int recordId = 0,
        RecordRarity rarity = RecordRarity.NORMAL)
    {
        this.mode = mode;
        this.recordId = recordId;
        this.rarity = rarity;
    }

    public void Receive()
    {
        RecordData record = null;

        switch (mode)
        {
            case RecordRewardMode.FixedRecord:
                record = AppManager.Instance.SafeInvoke(v => v.GetRecordData(recordId));
                break;

            case RecordRewardMode.RandomByRarity:
                {
                    var list = AppManager.Instance
                        .GetRecordByRarity(rarity);

                    record = list.Random();
                    OpenRecordRewardUI(record);
                }
                break;

            case RecordRewardMode.RandomAll:
                {
                    var list = AppManager.Instance
                        .GetAllRecordData();

                    record = list.Random();
                    OpenRecordRewardUI(record);
                }
                break;
        }

        if (record != null)
        {
            AppManager.Instance.SafeInvoke(v => v.GetRecordManager().SafeInvoke(v => v.AddRecord(record)));
        }
    }

    private void OpenRecordRewardUI(RecordData record)
    {
        List<RecordData> result = new();
        result.Add(record);

        // 얻은 결과 보상 UI 
        UIManager.Instance.SafeInvoke(v => v.OpenRecordSelectPopUp(result, false,
            RecordUIMode.VIEW));
    }

    private string GetPathByRandom()
    {
        string path = "RewardIcons/record_normal";

        switch (rarity)
        {
            case RecordRarity.NORMAL:
                break;
            case RecordRarity.RARE:
                path = "RewardIcons/record_rare";
                break;
            case RecordRarity.UNIQUE:
                path = "RewardIcons/record_unique";
                break;
            case RecordRarity.LEGENDARY:
                path = "RewardIcons/record_legandary";
                break;
            case RecordRarity.MYTH:
                path = "RewardIcons/record_myth";
                break;
        }

        return path;
    }
}
public class RewardManager
: Singleton<RewardManager>
{
    private readonly Dictionary<int, ItemReward> rewardMap = new();
    private readonly List<IReward> rewards = new();

    private bool bIsRewardPending = false;

    protected override void Awake()
    {
        base.Awake();
        if (IsDuplicate) return;
        AppManager.Instance.OnAwaked += () =>
        {
            if (IsInitialized) return;

            ManagerWaiter.WaitForManager<UIManager>((uiManager) =>
            {
                uiManager.OnJoinedLobby += OnJoinedLobby;
                uiManager.OnReturnedStageSelect += OnReturnedStageSelectScene;
            });
        };
    }

    private void AddItemReward(RewardData reward)
    {
        if (reward == null)
            return;

        if (UnityEngine.Random.Range(0, 101) > reward.weight)
            return;

        int amount = reward.amount;

        if (reward.range > 0)
            amount += UnityEngine.Random.Range(0, reward.range + 1);

        ItemData item =
            AppManager.Instance.GetItemData(
                reward.itemId,
                reward.itemCategory);

        if (item == null)
            return;

        if (item.category == ItemCategory.EQUIPMENT)
        {
            ItemData copy = item.Copy();
            copy.SetCount(1);

            ItemReward ir = new ItemReward(copy, 1);
            rewards.Add(ir);

            bIsRewardPending = true;

            return;
        }

        if (rewardMap.TryGetValue(item.id, out ItemReward exist))
        {
            exist.AddAmount(amount);
        }
        else
        {
            ItemData copy = item.Copy();

            copy.SetCount(amount);
            ItemReward ir = new ItemReward(copy, amount);
            rewardMap.Add(copy.id, ir);
            rewards.Add(ir);
        }
        bIsRewardPending = true;
    }

    private void AddRecordReward(RewardData reward)
    {
        bIsRewardPending = true;
        rewards.Add(RewardFactory.Create(reward));
    }

    public void GiveStageReward(int rewardID)
    {
        var clearRewardData = AppManager.Instance.SafeInvoke(v => v.GetStageClearRewardData(rewardID));
        if (clearRewardData != null)
        {
            foreach (var rewardid in clearRewardData.rewardIds)
            {
                var reward = AppManager.Instance.GetRewardData(rewardid);
                AddReward(reward);
            }
        }
    }

    public void AddReward(RewardData reward)
    {
        //TODO : EXP로 오는 보상 처리 로직 추가 
        switch (reward.rewardType)
        {
            case RewardType.ITEM:
                AddItemReward(reward);
                break;
            case RewardType.RECORD:
                AddRecordReward(reward);
                break;
        }
    }

    public void AddReward(ClearRewardData clearData)
    {
        if (clearData == null) return;

        foreach (var id in clearData.rewardIds)
        {
            RewardData reward = AppManager.Instance.GetRewardData(id);
            if (reward == null) continue;
            AddReward(reward);
        }
    }

    // 챕터가 클리어 될 때마다 이 매니저는 각 챕터 보상을 저장한다. 
    public void GiveChapterReward(int clearedChapter)
    {
        if (AppManager.Instance == null) return;

        ClearRewardData clear = AppManager.Instance.GetChapterClearRewardData(clearedChapter);
        if (clear == null) return;

        // 보상 정보로부터 받아낼 보상들을 전부 얻어냄 
        AddReward(clear);
    }

    // 팝업을 로비에만 띄우는게 맞나 탐사포인트 스테이지 선택창에서도 띄우게 해야할거같은데
    private void OpenRewardPopUp()
    {
        if (UIManager.Instance == null)
            return;

        //TODO : IReward로 처리해야함 로비로 가는 팝업 
        //UIManager.Instance.OpenRewardPopUp(rewards);

        bIsRewardPending = false;
    }


    // 이벤트에 의한 처리
    public void OnJoinedLobby()
    {
        if (bIsRewardPending == false) return;
        if (UIManager.Instance == null || UIManager.Instance.IsLobby() == false) return;

        OpenRewardPopUp();
    }

    public void OnReturnedStageSelectScene()
    {
        if (bIsRewardPending == false) return;

        if (rewards.Count == 0) return;

        UIManager.Instance.SafeInvoke(v => v.OpenRewardCardPopUp(rewards));

        bIsRewardPending = false;
    }

    public void ClearPendingRewards()
    {
        rewards.Clear();
        rewardMap.Clear();

        bIsRewardPending = false;
    }
}