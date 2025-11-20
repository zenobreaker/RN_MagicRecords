using System;
using System.Collections.Generic;
using UnityEngine;


public class RewardManager
    : Singleton<RewardManager>
{
    public event Action<List<ItemData>> OnAcceptedRewards;

    private List<ItemData> rewards = new();
    private List<ItemData> viewRewards = new();
    private bool bIsRewardPending = false;
  
    protected override void Awake()
    {
        base.Awake();

        AppManager.Instance.OnAwaked += () =>
        {
            if (IsInitialized) return; 

            ManagerWaiter.WaitForManager<UIManager>((uiManager) =>
            {
                uiManager.OnJoinedLobby += OnJoinedLobby;
                uiManager.OnReturnedStageSelectStage += OnReturnedStageSelectScene;
            });

            ManagerWaiter.WaitForManager<InventoryManager>((Inventory) =>
            {
                OnAcceptedRewards += Inventory.AddItems;
            });
        };
    }

    protected override void SyncDataFromSingleton()
    {
        rewards = Instance.rewards;
    }

    public void GiveStageReward(StageInfo stage)
    {
        if (stage == null) return;

        bool success = stage.bIsCleared;
        if (success)
        {
            var clearRewardId = stage.clearRewardId;
            var clearRewardData = AppManager.Instance?.GetStageClearRewardData(clearRewardId);
            if(clearRewardData != null)
            {
                foreach (var rewardid in clearRewardData.rewardIds)
                {
                    var reward = AppManager.Instance.GetRewardData(rewardid);
                    AddReward(reward);
                }
            }
        }
    }

    public void AddReward(RewardData reward)
    {
        if (reward == null) return; 

        int chance = UnityEngine.Random.Range(0, 101);

        if (chance <= reward.weight)
        {
            int rangeValue = reward.range == 0 ? 0 : UnityEngine.Random.Range(reward.amount, reward.range+1);

            var target = rewards.Find(x => x.id == reward.itemId);
            if (target == null)
            {
                ItemData item = AppManager.Instance.GetItemData(reward.itemId, reward.type);
                if (item != null)
                {
                    item.SetCount(reward.amount);
                    rewards.Add(item);
                }
            }
            else
            {
                if (target.category == ItemCategory.EQUIPMENT)
                    rewards.Add(target);
                else
                {
                    target.ModifyCount(+reward.amount + rangeValue);
                }
            }

            // 보상을 처리하지 않은 상태에서 이 구분으로 온다면 보상 처리한다.
             bIsRewardPending = true;
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

    private void ReceiveRewards()
    {
        viewRewards = rewards;
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

        OpenRewardPopUp();
    }
}
