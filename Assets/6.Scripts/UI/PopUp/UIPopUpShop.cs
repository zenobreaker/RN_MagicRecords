using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopUpShop : UIPopUp
{
    [SerializeField] protected ItemData item;
    [SerializeField] protected Image itemIconImage;
    [SerializeField] protected TextMeshProUGUI itemNameText;
    [SerializeField] protected TextMeshProUGUI itemMainDescText;
    [SerializeField] protected TextMeshProUGUI priceText;
    [SerializeField] protected Button buyButton;
    [SerializeField] protected Button exitButton;
    [SerializeField] protected Button plusButton;
    [SerializeField] protected Button minusButton;
    [SerializeField] protected Button maximumButton;
    [SerializeField] protected TMP_InputField amountField;

    private int price, unitPrice, amount = 1;
    private CurrencyType priceCurrency;
    private Func<bool> purchase;
    private bool submitting, completed, offerMode;
    private Sprite offerIcon;
    private string offerName, offerDescription;

    protected override void Awake()
    {
        base.Awake();
        // RequireComponent does not repair prefabs saved before the requirement.
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (exitButton != null) exitButton.onClick.AddListener(CloseUI);
        if (buyButton != null) buyButton.onClick.AddListener(TryBuyItem);
        if (plusButton != null) plusButton.onClick.AddListener(() => SetAmount(amount == int.MaxValue ? amount : amount + 1));
        if (minusButton != null) minusButton.onClick.AddListener(() => SetAmount(Math.Max(1, amount - 1)));
        if (maximumButton != null) maximumButton.onClick.AddListener(() => SetAmount(unitPrice > 0 && CurrencyManager.Instance != null
            ? CurrencyManager.Instance.GetCurrency(priceCurrency) / unitPrice : 1));
        if (amountField != null) amountField.onEndEdit.AddListener(text => SetAmount(int.TryParse(text, out var value) ? value : 1));
    }

    public void SetData(ItemData item, int price, CurrencyType currency)
    {
        this.item = item;
        unitPrice = this.price = price;
        priceCurrency = currency;
        amount = 1;
        purchase = null;
        offerMode = false;
        submitting = completed = false;
        ShowPopUp();
    }

    // The same popup/serialized visual references serve exploration purchases.
    public void SetOffer(Sprite icon, string title, string description, int cost, Func<bool> onPurchase)
    {
        item = null;
        offerMode = true;
        offerIcon = icon; offerName = title; offerDescription = description;
        price = unitPrice = cost; amount = 1;
        priceCurrency = CurrencyType.EXPOLORE_COIN;
        purchase = onPurchase;
        submitting = completed = false;
        ShowPopUp();
    }

    private bool CanChooseAmount => !offerMode && item is ShopItem shop && !(shop.TargetItemData is EquipmentItem);

    protected override void DrawPopUp()
    {
        if (itemIconImage != null) itemIconImage.sprite = offerMode ? offerIcon : item?.Icon;
        if (itemNameText != null) itemNameText.text = offerMode ? offerName : item?.LocalizedName;
        if (itemMainDescText != null) itemMainDescText.text = offerMode ? offerDescription : item?.LocalizedDescription;
        if (priceText != null) priceText.text = price.ToString();
        if (plusButton != null) plusButton.gameObject.SetActive(CanChooseAmount);
        if (minusButton != null) minusButton.gameObject.SetActive(CanChooseAmount);
        if (maximumButton != null) maximumButton.gameObject.SetActive(CanChooseAmount);
        if (amountField != null)
        {
            amountField.gameObject.SetActive(!offerMode);
            amountField.interactable = CanChooseAmount;
            amountField.SetTextWithoutNotify(amount.ToString());
        }
        if (buyButton != null) buyButton.interactable = !completed && price >= 0;
    }

    private void SetAmount(int value)
    {
        if (!CanChooseAmount) return;
        amount = Math.Max(1, Math.Min(value, unitPrice > 0 ? int.MaxValue / unitPrice : int.MaxValue));
        price = unitPrice * amount;
        DrawPopUp();
    }

    public override void OnSubmit() => TryBuyItem();

    private void TryBuyItem()
    {
        if (!isActiveAndEnabled || submitting || completed || price < 0) return;
        submitting = true;
        try
        {
            if (offerMode)
            {
                if (purchase == null || !purchase()) return;
            }
            else
            {
                if (!(item is ShopItem shop) || shop.TargetItemData == null ||
                    InventoryManager.Instance == null || CurrencyManager.Instance == null) return;
                var granted = shop.TargetItemData.Copy();
                if (granted == null) return;
                granted.uniqueID = Guid.NewGuid().ToString();
                granted.SetCount(amount);
                if (!CurrencyManager.Instance.SpendCurrency(priceCurrency, price))
                {
                    UIManager.Instance?.ShowToast("ui_toast_not_enough_cost_coin");
                    return;
                }
                InventoryManager.Instance.AddItem(granted);
            }
            completed = true;
            CloseUI();
            UIManager.Instance?.ShowToast("ui_toast_success_buy_item");
        }
        finally { submitting = false; }
    }

    protected override void OnDisable()
    {
        purchase = null;
        base.OnDisable();
    }
}
