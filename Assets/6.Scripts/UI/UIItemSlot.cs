using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class UISlot<T> : MonoBehaviour
{
    public Action<T> OnClickedSlot;

    protected void InvokeClick(T data)
    {
        OnClickedSlot?.Invoke(data);
    }


    public abstract void Refresh();

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

        Refresh();
    }

    public override void Refresh()
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
        InvokeClick(itemData);
    }

}
