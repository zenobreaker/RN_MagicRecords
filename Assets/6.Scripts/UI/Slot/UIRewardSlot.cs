using UnityEngine;

public class UIRewardSlot : UIItemSlot
{
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
