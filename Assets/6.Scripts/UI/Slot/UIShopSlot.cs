using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopSlot : UIItemSlot
{
    [SerializeField] private Image bgImage;
    [SerializeField] private TextMeshProUGUI itemNameTxt;
    [SerializeField] private UICurrency itemPrice; 

    private void Awake()
    {
        bgImage = GetComponent<Image>();    
        itemImage = transform.FindChildByName("Icon").GetComponent<Image>();
        itemNameTxt = transform.FindChildByName("Name").GetComponentInChildren<TextMeshProUGUI>();
        itemPrice = transform.FindChildByName("Price").GetComponentInChildren<UICurrency>();
    }

    public override void DrawSlot()
    {
        base.DrawSlot();

        if(itemNameTxt != null ) 
        {
            Debug.Assert(LocalizationManager.Instance != null); 
            itemNameTxt.text = LocalizationManager.Instance?.GetText(itemData.name);
        }

        if(itemPrice != null && itemData is ShopItem shopItem)
        {
            itemPrice.SetValue(shopItem.Price);
        }
    }
}
