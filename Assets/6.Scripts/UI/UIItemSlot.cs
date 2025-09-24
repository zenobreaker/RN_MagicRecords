using System;
using UnityEngine;

public class UISlot<T> : MonoBehaviour
{
    public Action<T> OnClickedSlot; 

    public virtual void OnClick()
    {
        
    }
}


public class UIItemSlot : UISlot<ItemData>
{
    protected ItemData itemData; 

    public override void OnClick()
    {
        OnClickedSlot?.Invoke(itemData); 
    }
}
