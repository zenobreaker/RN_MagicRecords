using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class InventoryManager
    : Singleton<InventoryManager>
{
    private List<ItemData> items = new();
    private Dictionary<ItemCategory, List<ItemData>> categoriesItems = new();
    private Dictionary<string, ItemData> uniqueIdLookup = new();    // key : unique id 

    private bool isDirty = false;
    public bool IsDirty => isDirty;
    public Action OnDataChanged;
    public void OnInit()
    {
        categoriesItems.Clear();
        categoriesItems.Add(ItemCategory.EQUIPMENT, new List<ItemData>());
        categoriesItems.Add(ItemCategory.INGREDIANT, new List<ItemData>());

        var loadData = SaveManager.LoadInventoryData();
        if (loadData == null) return;

        foreach (var info in loadData.itemInfoList)
        {
            var item = AppManager.Instance.GetItemData(info.itemId, info.itemCategoy);
            if (item == null) continue;
            item.uniqueID = info.uniqueId;
            item.itemCount = info.itemCount;

            uniqueIdLookup[item.uniqueID] = item;

            switch (info.itemCategoy)
            {
                case ItemCategory.EQUIPMENT:
                    categoriesItems[ItemCategory.EQUIPMENT].Add(item);
                    break;
                case ItemCategory.INGREDIANT:
                    categoriesItems[ItemCategory.INGREDIANT].Add(item);
                    break;
                case ItemCategory.CURRENCY:
                    categoriesItems[ItemCategory.INGREDIANT].Add(item);
                    break;
            }
        }
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
        this.items = Instance.items;
        categoriesItems = Instance.categoriesItems;
    }

    public void AddItems(List<ItemData> items)
    {
        foreach (ItemData item in items)
            AddItem(item);
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;

        if (item.category == ItemCategory.CURRENCY && CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(item);
            return;
        }

        items.Add(item);
        categoriesItems[item.category].Add(item);
        uniqueIdLookup[item.uniqueID] = item;

        OnDataChanged?.Invoke();
        isDirty = true;
    }

    public void RemoveItem(ItemData item)
    {
        items.Remove(item);
        categoriesItems[item.category].Remove(item);
        uniqueIdLookup.Remove(item.uniqueID);

        OnDataChanged?.Invoke();
        isDirty = true;
    }

    public ItemData FindItem(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return null;
        return uniqueIdLookup.TryGetValue(uniqueId, out ItemData value) ? value : null;
    }

    public List<ItemData> GetItems(ItemCategory category)
    {
        if (categoriesItems.TryGetValue(category, out var value))
            return value;
        return null;
    }

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

    public void SaveIfDirty()
    {
        if (isDirty == false) return;

        ItemInfoListData listData = new();
        foreach (var item in items)
        {
            ItemInfoSaveData save = new();
            save.itemId = item.id;
            save.uniqueId = item.uniqueID;
            save.itemCategoy = item.category;
            save.itemCount = item.itemCount;
            save.enhanceLevel = item is EquipmentItem ? (item as EquipmentItem).Enhance : 0;

            listData.itemInfoList.Add(save);
        }

        SaveManager.SaveInvetoryData(listData);
        isDirty = false; 
    }

}
