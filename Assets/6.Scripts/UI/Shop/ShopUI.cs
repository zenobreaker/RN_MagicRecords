using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

public class ShopUI : UiBase
{
    private ItemCategory category;
    private List<ItemData> items;

    private AppManager app;

    protected override void OnEnable()
    {
        base.OnEnable();

        app = AppManager.Instance;
        category = ItemCategory.EQUIPMENT;

        DrawShop();
    }

    private void DrawShop()
    {
        if (app == null)
            return;

        items = app.GetShopItems(category);
        if (items == null)
            return;


        UIListDrawer.DrawList<UIShopSlot, ItemData>(
            items, (slot, item, index) =>
            {
                slot.SetItemData(item);
                if(slot.gameObject.activeSelf == false) 
                    slot.gameObject.SetActive(true);

                slot.OnClickedSlot -= OnClickedSlot;
                slot.OnClickedSlot += OnClickedSlot;
                slot.DrawSlot();
            },
            slot =>
            {
                slot.OnClickedSlot -= OnClickedSlot;
                if (slot.gameObject.activeSelf == true)
                    slot.gameObject.SetActive(false);
            },
            InitReplaceContentObject,
             SetContentChildObjectsCallback<UIShopSlot>
            );
    }

    // ShopUI.cs - OnClickedSlot
    public void OnClickedSlot(ItemData item)
    {
        if(item is ShopItem shopItem == false)
            return;
        UIManager.Instance.OpenShopPopUp(item, shopItem.Price, (CurrencyType)shopItem.CurrencyType);
    }
}
