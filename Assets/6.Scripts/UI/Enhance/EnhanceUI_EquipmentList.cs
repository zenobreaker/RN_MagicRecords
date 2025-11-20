using System;
using System.Collections.Generic;

public class EnhanceUI_EquipmentList : UiBase
{
    private List<ItemData> items;

    public event Action<ItemData> OnClicked;

    public void Init(List<ItemData> items)
    {
        this.items = items;
    }
    
    public override void RefreshUI()
    {
        Draw();
    }

    public void Draw()
    {
        UIListDrawer.DrawList<UIEquipmentSlot, ItemData>(
            items, (slot, item, index) =>
            {
                slot.SetItemData(item);
                if(slot.gameObject.activeSelf == false)
                    slot.gameObject.SetActive(true);

                slot.OnClickedSlot -= ClickSlot; 
                slot.OnClickedSlot += ClickSlot;
                slot.DrawSlot();
            }, 
            slot =>
            {
                slot.OnClickedSlot -= ClickSlot;
                if (slot.gameObject.activeSelf == true)
                    slot.gameObject.SetActive(false);
            },
            InitReplaceContentObject,
            SetContentChildObjectsCallback
            );
    }

    public void ClickSlot(ItemData item)
    {
        if (item is EquipmentItem)
        {
            // 장비 처리 
            OnClicked?.Invoke(item);
        }
    }
}
