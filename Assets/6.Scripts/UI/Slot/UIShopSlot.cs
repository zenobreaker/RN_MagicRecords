using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopSlot : UIItemSlot
{
    private System.Action offerClick;
    [SerializeField] private Image bgImage;
    [SerializeField] private TextMeshProUGUI itemNameTxt;
    [SerializeField] private UICurrency itemPrice; 

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        bgImage = GetComponent<Image>();
        itemImage = transform.Find("Icon")?.GetComponent<Image>();
        itemNameTxt = transform.Find("Name")?.GetComponentInChildren<TextMeshProUGUI>(true);
        itemPrice = GetComponentInChildren<UICurrency>(true);
    }

    public override void SetItemData(ItemData data)
    {
        CacheReferences(); // Pooled slots are configured while inactive, before Awake.
        offerClick = null;
        GetComponent<Button>().interactable = true;
        base.SetItemData(data);
    }

    public void SetOffer(Sprite icon, string title, int price, bool sold, System.Action click)
    {
        CacheReferences();
        itemData = null;
        OnClickedSlot = null;
        offerClick = sold ? null : click;
        if (itemImage != null) itemImage.sprite = icon;
        if (itemNameTxt != null) itemNameTxt.text = title;
        itemPrice?.SetValue(price, CurrencyType.EXPOLORE_COIN,
            sold ? "판매 완료" : price < 0 ? "가격 미정" : null, itemNameTxt?.font);
        GetComponent<Button>().interactable = !sold;
    }

    public override void OnClick()
    {
        if (offerClick != null) offerClick();
        else if (itemData != null) base.OnClick();
    }

    public override void Refresh()
    {
        base.Refresh();

        if(itemNameTxt != null ) 
        {
            Debug.Assert(LocalizationManager.Instance != null); 
            itemNameTxt.text = itemData?.LocalizedName ?? "";
        }

        if(itemPrice != null && itemData is ShopItem shopItem)
        {
            itemPrice.SetValue(shopItem.Price, shopItem.CurrencyType);
        }
    }
}
