using System;
using UnityEngine;
using UnityEngine.UI;

public class UIInvenSlot : UIItemSlot
{
    private void Awake()
    {
        itemImage = GetComponent<Image>();
    }

    public override void SetItemData(ItemData itemData)
    {
        base.SetItemData(itemData);
        DrawSlot(); 
    }

    protected override void DrawSlot()
    {
        base.DrawSlot();
    }
}
