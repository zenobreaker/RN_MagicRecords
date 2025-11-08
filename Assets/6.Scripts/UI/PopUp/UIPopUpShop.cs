using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPopUpShop : UIPopUpBase
{
    [SerializeField] protected ItemData item;
    [SerializeField] protected Image itemIconImage;
    [SerializeField] protected TextMeshProUGUI itemNameText;
    [SerializeField] protected TextMeshProUGUI itemMainOptionText;
    [SerializeField] protected TextMeshProUGUI itemMainDescText;
    [SerializeField] protected TextMeshProUGUI priceText;
    [SerializeField] protected Button buyButton;
    [SerializeField] protected Button exitButton;

    [SerializeField] protected Button plusButton;
    [SerializeField] protected Button minusButton;
    [SerializeField] protected Button maximumButton;
    [SerializeField] protected TMP_InputField amountField;

    private int price = 0;
    private CurrencyType priceCurrency = CurrencyType.NONE;

    private int amount = 1;

    protected override void Awake()
    {
        base.Awake();

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() =>
            {
                UIManager.Instance.ClosePopup();
            });
        }

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(() =>
            {
                TryBuyItem();
            });
        }

        if(plusButton != null)
        {
            plusButton.onClick.AddListener(() =>
            {
                amount++;
                DrawAmount();
            });
        }

        if(minusButton != null)
        {
            minusButton.onClick.AddListener(() =>
            {
                amount = Math.Max(1, amount - 1);
                DrawAmount();
            });
        }
    }

    // 호출자는 item, 가격, 통화 타입을 함께 넘겨야 함
    public void SetData(ItemData item, int price, CurrencyType currency)
    {
        this.item = item;
        this.price = price;
        this.priceCurrency = currency;
        this.amount = 1;
        DrawPopUp();
    }

    protected override void DrawPopUp()
    {
        if (item == null) return;

        if (itemIconImage != null)
            itemIconImage.sprite = item.icon;

        if (itemNameText != null)
            itemNameText.text = item.name;

        if (itemMainDescText != null)
            itemMainDescText.text = item.description;

        if (priceText != null)
            priceText.text = $"{price}";

        DrawItemOption();
        DrawAmountButtons();
        DrawAmount();
    }

    protected void DrawItemOption()
    {
        if (item == null) return;
        if (itemMainOptionText == null) return;

        if (item is EquipmentItem equipment)
        {
            itemMainOptionText.text = equipment.modifier.GetFullValue();
        }
        else
        {
            itemMainOptionText.text = string.Empty;
        }
    }

    private void DrawAmountButtons()
    {
        if (item == null) return;

        if(item is EquipmentItem)
        {
            if (plusButton != null) plusButton.gameObject.SetActive(false);
            if (minusButton != null) minusButton.gameObject.SetActive(false);
            if (maximumButton != null) maximumButton.gameObject.SetActive(false);
        }   
        else
        {
            if (plusButton != null) plusButton.gameObject.SetActive(true);
            if (minusButton != null) minusButton.gameObject.SetActive(true);
            if (maximumButton != null) maximumButton.gameObject.SetActive(true);
        }
    }

    private void DrawAmount()
    {
        if(amountField == null) return;

        amountField.text = amount.ToString();
    }

    private void TryBuyItem()
    {
        if (item == null) return;

        // CurrencyManager로 지불 시도
        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager missing.");
            return;
        }

        bool success = CurrencyManager.Instance.SpendCurrency(priceCurrency, price);
        if (!success)
        {
            // 피드백: 통화 부족
            Debug.Log($"Not enough currency: need {price} of {priceCurrency}");
            // TODO: UX 피드백(토스트/애니메이션 등)
            return;
        }

        // 아이템을 인벤토리에 추가: 복사하여 고유 ID 생성
        ItemData newItem = item.Copy();
        if (newItem != null)
            newItem.uniqueID = Guid.NewGuid().ToString();

        InventoryManager.Instance?.AddItem(newItem ?? item);

        // 구매 후 팝업 닫기
        UIManager.Instance.ClosePopup();
    }
}
