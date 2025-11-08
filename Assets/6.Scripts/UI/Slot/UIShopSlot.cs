using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopSlot : UIItemSlot
{
    [SerializeField] private Image bgImage;
    [SerializeField] private TextMeshProUGUI itemNameTxt;
    [SerializeField] private TextMeshProUGUI itemPriceTxt; 

    private void Awake()
    {
        bgImage = GetComponent<Image>();    
        itemImage = transform.FindChildByName("Icon").GetComponent<Image>();
        itemNameTxt = transform.FindChildByName("Name").GetComponentInChildren<TextMeshProUGUI>();
        itemPriceTxt = transform.FindChildByName("Price").GetComponentInChildren<TextMeshProUGUI>();
    }

    public override void DrawSlot()
    {
        base.DrawSlot();

        if(itemNameTxt != null ) 
        {
            itemNameTxt.text = itemData.name;
        }

        if(itemPriceTxt != null && itemData is ShopItem shopItem)
        {
            itemPriceTxt.text = shopItem.Price.ToString();
        }
    }
}
