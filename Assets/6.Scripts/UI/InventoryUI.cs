using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : UiBase
{
    private ItemCategory category;
    private List<ItemData> items;

    protected override void OnEnable()
    {
        base.OnEnable();

        category = ItemCategory.EQUIPMENT;
        StopAllCoroutines();
        StartCoroutine(WaitForManager()); 
    }

    private IEnumerator WaitForManager()
    {
        yield return new WaitUntil(() => InventoryManager.Instance != null);
        DrawInventory();
    }

    private void DrawInventory()
    {
        if (InventoryManager.Instance == null) return;

        items = InventoryManager.Instance.GetItems(category);
        if (items == null)
        {

            return;
        }

        InitReplaceContentObject(items.Count);

        int index = 0;
        SetContentChildObjectsCallback<UIInvenSlot>(slot =>
        {
            if (index < items.Count)
            {
                slot.SetItemData(items[index]);
                slot.gameObject.SetActive(true);
                slot.OnClickedSlot -= ClickSlot;
                slot.OnClickedSlot += ClickSlot;
                index++;
            }
            else
            {
                slot.OnClickedSlot -= ClickSlot;
                slot.gameObject.SetActive(false);
            }
        });
    }

    public void OnClickedCategoryButton(int category)
    {
        this.category = (ItemCategory)category;
        DrawInventory(); 
    }

    public void ClickSlot(ItemData item)
    {
        if(item is EquipmentItem)
        {
            // 장비 처리 
            UIManager.Instance.OpenItemPopUp(item);
        }
    }
}
