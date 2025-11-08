using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class UISlot<T> : MonoBehaviour
{
    public Action<T> OnClickedSlot;

    public abstract void DrawSlot();

    public virtual void OnClick()
    {
        
    }
}


public class UIItemSlot : UISlot<ItemData>
{
    [SerializeField] private Sprite noDataIcon;
    protected Image itemImage;
    
    protected ItemData itemData;

    public virtual void SetItemData(ItemData itemData)
    {
        this.itemData = itemData;
    }

    public override void DrawSlot()
    {
        if (itemImage == null)
            return; 

        if(itemData == null)
        {
            itemImage.sprite = noDataIcon;
            return; 
        }

        itemImage.sprite = itemData.icon;
    }

    public override void OnClick()
    {
        OnClickedSlot?.Invoke(itemData); 
    }

}
