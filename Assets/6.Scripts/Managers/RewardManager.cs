using System;
using System.Collections.Generic;
using UnityEngine;

public interface IReward
{
    string Title { get; }
    string Description { get; }
    Sprite Icon { get; }

    void Receive();
}

public class ItemReward : IReward
{
    private readonly ItemData itemData;

    public string Title => itemData.name;
    public string Description => itemData.description;
    public Sprite Icon => itemData.icon;

    public ItemReward(ItemData itemData)
    {
        this.itemData = itemData;
    }

    public void Receive()
    {
        InventoryManager.Instance.SafeInvoke(v => v.AddItem(itemData));
    }
}

public class RecordReward : IReward
{
    private readonly RecordRewardMode mode;

    private readonly int recordId;
    private readonly RecordRarity rarity;

    public string Title => "레코드";
    public string Description => "레코드를 획득합니다.";

    private Sprite recordIcon;
    public Sprite Icon => recordIcon;
    public RecordReward(
        RecordRewardMode mode,
        int recordId = 0,
        RecordRarity rarity = RecordRarity.NORMAL)
    {
        this.mode = mode;
        this.recordId = recordId;
        this.rarity = rarity;

        RecordData record = AppManager.Instance.GetRecordData(recordId);
        recordIcon = record.icon;
    }

    public void Receive()
    {
        RecordData record = null;

        switch (mode)
        {
            case RecordRewardMode.FixedRecord:
                record = AppManager.Instance.GetRecordData(recordId);
                break;

            case RecordRewardMode.RandomByRarity:
                {
                    var list = AppManager.Instance
                        .GetRecordByRarity(rarity);

                    record = list.Random();
                }
                break;

            case RecordRewardMode.RandomAll:
                {
                    var list = AppManager.Instance
                        .GetAllRecordData();

                    record = list.Random();
                }
                break;
        }

        if (record != null)
        {
            AppManager.Instance.SafeInvoke(v=>v.GetRecordManager().SafeInvoke(v=>v.AddRecord(record)));
        }
    }
}

public class RewardManager
    : Singleton<RewardManager>
{
    public event Action<List<ItemData>> OnAcceptedRewards;

    private readonly Dictionary<int, ItemData> rewardMap = new();
    private readonly List<ItemData> rewards = new();

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

            ManagerWaiter.WaitForManager<InventoryManager>((Inventory) =>
            {
                OnAcceptedRewards += Inventory.AddItems;
            });
        };

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

            rewards.Add(copy);

            bIsRewardPending = true;

            return;
        }

        if (rewardMap.TryGetValue(item.id, out ItemData exist))
        {
            exist.ModifyCount(amount);
        }
        else
        {
            ItemData copy = item.Copy();

            copy.SetCount(amount);

            rewardMap.Add(copy.id, copy);
            rewards.Add(copy);
        }

        bIsRewardPending = true;
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

    private void ReceiveRewards()
    {
        //viewRewards = rewards;
        OnAcceptedRewards?.Invoke(rewards);
        rewards.Clear();
    }

    // 팝업을 로비에만 띄우는게 맞나 탐사포인트 스테이지 선택창에서도 띄우게 해야할거같은데
    private void OpenRewardPopUp()
    {
        if (UIManager.Instance == null)
            return;

        UIManager.Instance.OpenRewardPopUp(rewards);
        ReceiveRewards();

        bIsRewardPending = false;
    }
    private List<IReward> ConvertToRewards()
    {
        List<IReward> result = new();

        foreach (var item in rewards)
        {
            result.Add(new ItemReward(item));
        }

        return result;
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

        List<IReward> result = ConvertToRewards();
        if(result.Count == 0) return;

        UIManager.Instance.SafeInvoke(v=>v.OpenRewardCardPopUp(result));

        bIsRewardPending = false;
    }
}
