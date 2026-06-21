using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class UIRewardCardPopUp : UIPopUp
{
    [SerializeField] private TMP_Text titleText;

    private List<IReward> rewards;
    private int remainRewardCount; 

    public void SetData(List<IReward> rewards)
    { 
        this.rewards = rewards;
        ShowPopUp(); 
    }

    protected override void DrawPopUp()
    {
        if (rewards == null)
            return;

        InitReplaceContentObject(rewards.Count);

        for (int i = 0; i < rewards.Count; i++)
        {
            var cardObj = content.transform.GetChild(i);

            cardObj.gameObject.SetActive(true);

            if (cardObj.TryGetComponent<UIRewardCard>(out var card))
            {
                card.OnReceived += HandleReceived;
                card.Setup(rewards[i]);
            }
            remainRewardCount++; 
        }

        if (titleText != null)
        {
            titleText.text = "";
        }
    }

    private void HandleReceived(UIRewardCard card)
    {
        if (card == null) return;
        card.gameObject.SetActive(false);
        remainRewardCount--;

        if (remainRewardCount <= 0)
            CloseUI();
    }

    public override void CloseUI()
    {
        base.CloseUI();

        RewardManager.Instance.SafeInvoke(v => v.ClearPendingRewards());
    }
}
