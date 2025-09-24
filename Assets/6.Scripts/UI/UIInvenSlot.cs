using System;
using UnityEngine;
using UnityEngine.UI;

public class UIInvenSlot : UIItemSlot
{
    [SerializeField] private Sprite noDataIcon;

    private Image itemImage;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
    }

    public void SetItemData(ItemData itemData)
    {
        this.itemData = itemData;
        DrawSlot(); 
    }

    private void DrawSlot()
    {
        if (itemImage == null) return; 

        if (itemData == null)
        {
            itemImage.sprite = noDataIcon;

            return; 
        }

        itemImage.sprite = itemData.icon; 
    }

}
