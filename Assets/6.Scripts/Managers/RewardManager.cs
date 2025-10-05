using System;
using System.Collections.Generic;
using UnityEngine;


public class RewardManager
    : Singleton<RewardManager>
{
    public event Action OnAcceptedRewards;

    private List<ItemData> rewards = new();
    private int credit = 0;
    private bool bIsRewardPending = false; 
    protected override void SyncDataFromSingleton()
    {
        rewards = Instance.rewards;
        credit = Instance.credit; 
    }

    public void GiveStageReward(StageInfo stage, bool success)
    {
        if (success)
        {
            foreach (var reward in stage.rewardIds)
            {

            }
        }
    }

    public void AddReward(RewardData reward)
    {
        int chance = UnityEngine.Random.Range(0, 101);

        if (chance <= reward.weight)
        {
            if (reward.type == RewardType.CREDIT)
            {
                credit += reward.amount;
                return;
            }

            var target = rewards.Find(x => x.id == reward.itemId);
            if (target == null)
            {
                ItemCategory category = reward.type == RewardType.EQUIPMENT ? ItemCategory.EQUIPMENT : ItemCategory.INGREDIANT;
                ItemData item = AppManager.Instance.GetItemData(reward.itemId, category);
                rewards.Add(item);
            }
            else
            {
                if (target.category == ItemCategory.EQUIPMENT)
                    rewards.Add(target);
                else
                    target.itemCount += reward.amount;
            }

            // 보상을 처리하지 않은 상태에서 이 구분으로 온다면 보상 처리한다.
             bIsRewardPending = true;
            if (bIsRewardPending)
                OpenRewardPopUp();
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
        // 나는 재능이 없는 걸까나 
        ClearRewardData clear = AppManager.Instance.GetChapterClearRewardData(clearedChapter);
        if (clear == null) return;

        // 보상 정보로부터 받아낼 보상들을 전부 얻어냄 
        AddReward(clear);
    }

    private void ReceiveRewads() => rewards.Clear(); 

    private void OpenRewardPopUp()
    {
        if (UIManager.Instance == null || UIManager.Instance.IsLobby() == false) 
            return;
        
        UIManager.Instance.OpenRewardPopUp(rewards);
        ReceiveRewads();

        bIsRewardPending = false;
    }

    // 이벤트에 의한 처리
    public void OnJoinedLobby()
    {
        if (bIsRewardPending == false) return;
        if (UIManager.Instance == null) return;

        OpenRewardPopUp();
    }
}
