using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class UIRewardCardPopUp : UIPopUp
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text continueText;

    private List<IReward> rewards;
    private int remainRewardCount; 
    private Action onChapterReceived;
    private int chapter;

    public void SetData(List<IReward> rewards)
    {
        chapter = 0;
        onChapterReceived = null;
        this.rewards = rewards;
        ShowPopUp(); 
    }

    public void SetChapterData(int clearedChapter, List<IReward> rewards, Action onReceived)
    {
        chapter = clearedChapter;
        onChapterReceived = onReceived;
        this.rewards = rewards;
        ShowPopUp();
    }

    public void OnContinueButton()
    {
        if (chapter <= 0) { CloseUI(); return; }
        var callback = onChapterReceived;
        if (callback == null) return;
        onChapterReceived = null;
        base.CloseUI();
        callback.Invoke();
    }

    protected override void DrawPopUp()
    {
        if (rewards == null)
            return;

        InitReplaceContentObject(rewards.Count);
        remainRewardCount = 0;

        for (int index = 0; index < content.transform.childCount; index++)
            content.transform.GetChild(index).gameObject.SetActive(index < rewards.Count);

        for (int i = 0; i < rewards.Count; i++)
        {
            var cardObj = content.transform.GetChild(i);

            cardObj.gameObject.SetActive(true);

            if (cardObj.TryGetComponent<UIRewardCard>(out var card))
            {
                card.OnReceived -= HandleReceived;
                card.OnReceived += HandleReceived;
                card.Setup(rewards[i], chapter <= 0);
            }
            remainRewardCount++; 
        }

        if (titleText != null)
        {
            titleText.text = chapter > 0 ? $"챕터 {chapter} 클리어 보상" : "획득 보상";
        }
        continueText.SafeInvoke(text => text.text = chapter > 0 ? "보상 수령" : "닫기");
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
        if (chapter > 0) return; // Escape/background cannot skip a chapter reward.
        base.CloseUI();

        RewardManager.Instance.SafeInvoke(v => v.ClearPendingRewards());
    }
}
