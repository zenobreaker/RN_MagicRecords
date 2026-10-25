using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIRewardCardPopUp : UIPopUp
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text continueText;
    [SerializeField] private Button receiveAllButton;
    [SerializeField] private TMP_Text receiveAllText;

    private List<IReward> rewards;
    private int remainRewardCount; 
    private Action onChapterReceived;
    private int chapter;
    private bool receivingAll;

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

    public void OnReceiveAllButton()
    {
        if (chapter > 0 || receivingAll || !gameObject.activeSelf || remainRewardCount <= 0)
            return;

        receivingAll = true;
        receiveAllButton.SafeInvoke(button => button.interactable = false);
        var grantedRecords = new List<RecordData>();
        try
        {
            // Use the same per-card receipt guard, including cards already claimed individually.
            for (int i = 0; i < content.transform.childCount; i++)
            {
                var child = content.transform.GetChild(i);
                if (!child.gameObject.activeSelf || !child.TryGetComponent<UIRewardCard>(out var card))
                    continue;
                if (card.TryReceive(false, out var record) && record != null)
                    grantedRecords.Add(record);
            }
        }
        finally
        {
            receivingAll = false;
            receiveAllButton.SafeInvoke(button => button.interactable = remainRewardCount > 0);
        }

        if (remainRewardCount <= 0)
            CloseAndClearRewards();

        // Grant first, then show one read-only result, just like chapter rewards.
        if (grantedRecords.Count > 0)
            UIManager.Instance.SafeInvoke(ui =>
                ui.OpenRecordSelectPopUp(grantedRecords, false, RecordUIMode.VIEW));
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
        receiveAllButton.SafeInvoke(button =>
        {
            button.gameObject.SetActive(chapter <= 0);
            button.interactable = remainRewardCount > 0;
        });
        receiveAllText.SafeInvoke(text => text.text =
            LocalizationManager.Instance != null
                ? LocalizationManager.Instance.GetText("ui_reward_receive_all")
                : "모두 받기");
    }

    private void HandleReceived(UIRewardCard card)
    {
        if (card == null) return;
        card.gameObject.SetActive(false);
        remainRewardCount--;

        if (remainRewardCount <= 0 && !receivingAll)
            CloseUI();
    }

    /// <summary>
    /// 미수령 보상이 남아 있으면 확인 후 보상창을 닫습니다.
    /// </summary>
    public override void CloseUI()
    {
        if (chapter > 0) return; // Escape/background cannot skip a chapter reward.
        if (receivingAll) return;
        if (!gameObject.activeSelf) return;

        if (remainRewardCount > 0)
        {
            UIManager.Instance.SafeInvoke(ui => ui.OpenTwoButtonPopUp(
                "보상 수령 확인",
                "아직 받지 않은 보상이 있습니다.\n닫으면 남은 보상을 받을 수 없습니다.\n정말 닫으시겠습니까?",
                "확인", "취소", CloseAndClearRewards, null));
            return;
        }

        CloseAndClearRewards();
    }

    /// <summary>
    /// 수령 완료 또는 닫기 확인 후 보상창과 대기 보상을 정리합니다.
    /// </summary>
    private void CloseAndClearRewards()
    {
        if (chapter > 0 || !gameObject.activeSelf) return;
        base.CloseUI();

        RewardManager.Instance.SafeInvoke(v => v.ClearPendingRewards());
    }
}
