using System;
using TMPro;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIRewardCard : UIItemSlot
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button receiveButton;

    public event Action<UIRewardCard> OnReceived;
    private IReward reward;
    private bool received; 

    private void Awake()
    {
        if (receiveButton != null)
            receiveButton.onClick.AddListener(OnClickReceive);

        itemImage = transform.FindChildByName("Icon").GetComponent<Image>();
    }
    private void OnDestroy()
    {
        if (receiveButton != null)
            receiveButton.onClick.RemoveAllListeners();
    }

    public void Setup(IReward reward)
    {
        this.reward = reward;

        if (titleText != null)
            titleText.text = reward.Title;

        if (descriptionText != null)
            descriptionText.text = reward.Description;

        if (itemImage != null)
            itemImage.sprite = reward.Icon;

        received = false; 
    }

    public override void Refresh()
    {
        base.Refresh();

        if (itemData != null)
        {
            titleText.SafeInvoke(v => v.text = itemData.name);
            descriptionText.SafeInvoke(v => v.text = itemData.description);
        }
    }

    private void OnClickReceive()
    {
        if (received) return;
        received = true; 

        reward?.Receive();
        OnReceived?.Invoke(this);
    }
}
