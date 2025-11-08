using System;
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

        // Use ManagerWaiter convenience helper to auto register/unregister events
        ManagerWaiter.RegisterManagerEvent<InventoryManager>(this,
            onRegister: inventory =>
            {
                inventory.OnInitialized += DrawInventory;
                inventory.OnDataChanged += DrawInventory;
                DrawInventory();
            },
            onUnregister: inventory =>
            {
                inventory.OnInitialized -= DrawInventory;
                inventory.OnDataChanged -= DrawInventory;
            });
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // No manual unsubscribe needed; RegisterManagerEvent attaches a ManagerEventUnsubscriber to this GameObject
    }

    private void DrawInventory()
    {
        var manager = InventoryManager.Instance;
        if (manager == null) return;

        items = manager.GetItems(category);
        if (items == null)
            return;

        UIListDrawer.DrawList<UIInvenSlot, ItemData>(
          items, (slot, item, index) =>
          {
              // Use provided item to avoid indexing closure
              slot.SetItemData(item);
              if (slot.gameObject.activeSelf == false)
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
           SetContentChildObjectsCallback<UIInvenSlot>
          );
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
