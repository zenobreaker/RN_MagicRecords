using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class InventoryManager
    : Singleton<InventoryManager>
{
    private Dictionary<ItemCategory, Inventory> inventories = new();
    private Dictionary<string, ItemData> uniqueIdLookup = new();    // key : unique id 

    private bool isDirty = false;
    public bool IsDirty => isDirty;
    public Action OnDataChanged;
    public event Action OnInitialized; // Added: event to notify when initialization is finished

    public void OnInit()
    {
        inventories.Clear();
        inventories.Add(ItemCategory.EQUIPMENT, new EquipmentInventory());
        inventories.Add(ItemCategory.INGREDIANT, new StackableInventory());
        inventories.Add(ItemCategory.CURRENCY, new CurrencyInventory());

        foreach(var pair in inventories)
        {
            pair.Value.OnInventoryChanged += (inventory) =>
            {
                isDirty = true;
            };
        }

        var loadData = SaveManager.LoadInventoryData();
        if (loadData == null)
        {
            // Still notify listeners that initialization is complete even if no saved data
            OnInitialized?.Invoke();
            return;
        }

        foreach (var info in loadData.itemInfoList)
        {
            var item = AppManager.Instance.GetItemData(info.itemId, info.itemCategoy);
            if (item == null) continue;
            item.uniqueID = info.uniqueId;
            item.SetCount(info.itemCount);

            if (item is EquipmentItem equipment)
                equipment.EnhanceItem(info.enhanceLevel);

            uniqueIdLookup[item.uniqueID] = item;

            switch (info.itemCategoy)
            {
                case ItemCategory.EQUIPMENT:
                    inventories[ItemCategory.EQUIPMENT].AddItem(item);
                    break;
                case ItemCategory.INGREDIANT:
                    inventories[ItemCategory.INGREDIANT].AddItem(item);
                    break;
                case ItemCategory.CURRENCY:
                    inventories[ItemCategory.CURRENCY].AddItem(item);
                    break;
            }
        }

        // Notify listeners that initialization is finished
        OnInitialized?.Invoke();
    }


    public void TestAddItems()
    {
        // Test
        var item = AppManager.Instance.GetEquipmentItem(1000);
        AddItem(item);
        item = AppManager.Instance.GetEquipmentItem(2000);
        AddItem(item);
        item = AppManager.Instance.GetEquipmentItem(3000);
        AddItem(item);
    }

    protected override void SyncDataFromSingleton()
    {
        inventories = Instance.inventories;
        uniqueIdLookup = Instance.uniqueIdLookup;
        isDirty = Instance.isDirty;
    }

    public void AddItems(List<ItemData> items)
    {
        foreach (ItemData item in items)
            AddItem(item);
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;

        inventories[item.category].AddItem(item);
        uniqueIdLookup[item.uniqueID] = item;

        OnDataChanged?.Invoke();
        //isDirty = true;
    }

    public void RemoveItem(ItemData item)
    {
        inventories[item.category].RemoveItem(item);
        uniqueIdLookup.Remove(item.uniqueID);

        OnDataChanged?.Invoke();
        //isDirty = true;
    }

    public bool RemoveItem(int itemID, int itemCount)
    {
        foreach(var pair in inventories)
        {
            var inventory = pair.Value;
            var item = inventory.GetItem(itemID);

            if(item != null)
              return inventory.RemoveItem(itemID, itemCount);
        }

        return false; 
    }

    public ItemData FindItem(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return null;
        return uniqueIdLookup.TryGetValue(uniqueId, out ItemData value) ? value : null;
    }

    public List<ItemData> GetItems(ItemCategory category)
    {
        if (inventories.TryGetValue(category, out var value))
            return value.GetItems();
        return null;
    }

    public int GetItemCount(int itemID)
    {
        if (inventories.TryGetValue(ItemCategory.INGREDIANT, out var value))
            if (value is StackableInventory stack)
                return stack.GetItemCount(itemID);
        return 0;
    }

    /////////////////////////////////////////////////////////////////////////////

    public void EquipItem(int charid, CharEquipmentData equip)
    {
        if (equip == null) return;

        // 플레이어매니저랑 통신하는데 이이템 데이터가 들어오기도 전에 호출되서 정상적으로
        // 아이템을 장착 정보를 갱신할 수 없다. 

        foreach (string uid in equip.GetEquippedItemIDs())
            EquipItem(charid, uid);
    }

    public void EquipItem(int charid, string equipmentUID)
    {
        if (uniqueIdLookup.TryGetValue(equipmentUID, out var value))
        {
            EquipItem(charid, value as EquipmentItem);
            return;
        }
    }

    public void EquipItem(int charid, EquipmentItem equipment)
    {
        if (PlayerManager.Instance == null || equipment == null) return;

        var charEquipment = PlayerManager.Instance.GetCharEquipmentData(charid);
        if (charEquipment == null) return;
        if (equipment.Eqeuipped) return;

        equipment.Eqeuipped = true;
        equipment.owner = charid;

        charEquipment.EquipItem(equipment);
        OnDataChanged?.Invoke();
        isDirty = true;
    }

    public void UnequipItem(int charid, EquipmentItem equipment)
    {
        if (PlayerManager.Instance == null || equipment == null) return;

        var charEquipment = PlayerManager.Instance.GetCharEquipmentData(charid);
        if (charEquipment == null) return;

        equipment.Eqeuipped = false;
        equipment.owner = 0;

        charEquipment.UnequipItem(equipment);
        OnDataChanged?.Invoke();
        isDirty = true;
    }

    /// <summary>
    /// /////////////////////////////////////////////////////////////
    /// </summary>

    public void SaveIfDirty()
    {
        if (isDirty == false) return;

        ItemInfoListData listData = new();
        
        foreach(KeyValuePair<ItemCategory, Inventory> pair in inventories)
        {
            var items = pair.Value.GetItems();
            foreach (var item in items)
            {
                ItemInfoSaveData save = new();
                save.itemId = item.id;
                save.uniqueId = item.uniqueID;
                save.itemCategoy = item.category;
                save.itemCount = item.GetCount();
                save.enhanceLevel = item is EquipmentItem ? (item as EquipmentItem).Enhance : 0;

                listData.itemInfoList.Add(save);
            }
        }
        
        SaveManager.SaveInvetoryData(listData);
        isDirty = false; 
    }

    public Inventory GetInvetory(ItemCategory category)
    {
        return inventories[category];
    }
}
