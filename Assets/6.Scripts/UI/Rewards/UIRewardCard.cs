using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIRewardCard : UIItemSlot
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button receiveButton;

    public event Action<UIRewardCard> OnReceived;
    private IReward reward;

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
        reward?.Receive();
        OnReceived?.Invoke(this);
    }
}
