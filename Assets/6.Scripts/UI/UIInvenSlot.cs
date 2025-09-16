using System;
using UnityEngine;
using UnityEngine.UI;

public class UIInvenSlot : MonoBehaviour
{
    [SerializeField] private Sprite noDataIcon;

    private Image itemImage;
    private ItemData itemData;

    public event Action<ItemData> OnClickedSlot; 

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

    public void OnClick()
    {
        OnClickedSlot?.Invoke(itemData);
    }
}
