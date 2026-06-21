using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Inventory
{
    protected List<ItemData> items = new();

    protected Dictionary<int, ItemData> itemMap = new();

    public event Action<Inventory> OnInventoryChanged;

    public virtual void AddItem(ItemData item)
    {
        if (item == null) return; 

        items.Add(item);
        itemMap[item.id] = item; 

        item.OnChanged += HandleItemChanged;
        OnInventoryChanged?.Invoke(this);
    }

    public virtual void RemoveItem(ItemData item)
    {
        if (item == null) return; 

        item.OnChanged -= HandleItemChanged;

        items.Remove(item);
        itemMap.Remove(item.id); 

        OnInventoryChanged?.Invoke(this);
    }

    public virtual bool RemoveItem(int itemID, int itemCount)
    {
        if (itemMap.TryGetValue(itemID, out ItemData item) == false)
            return false; 

        item.ModifyCount(-1 * itemCount);
        if (item.GetCount() <= 0)
            RemoveItem(item);

        return true;
    }

    public virtual IReadOnlyList<ItemData> GetItems()
    {
        return items;
    }

    public virtual ItemData GetItem(int itemID)
    {
        return itemMap.TryGetValue(itemID, out var item) ? item : null; 
    }

    protected virtual void HandleItemChanged(ItemData item)
    {
        OnInventoryChanged?.Invoke(this);
    }

    public virtual void SaveInventory()
    {

    }
}

public class EquipmentInventory : Inventory
{
    private Dictionary<string, ItemData> equipmentMap = new();

    public override void AddItem(ItemData item)
    {
        if (item == null) return;
        if (item.category != ItemCategory.EQUIPMENT)
        {
            Debug.LogWarning("Cannot add non-equipment item to EquipmentInventory.");
            return;
        }

        // 장비는 항상 개별 슬롯(단일 개체)로 추가되어야 함.
        // 전달된 인스턴스가 이미 인벤토리에 존재하거나 uniqueID가 비어있다면 복사해서 고유 ID 부여 후 추가한다.
        ItemData toAdd = item;

        // 항상 수량은 1로 강제
        toAdd.SetCount(1);

        bool needsCopy = false;

        // uniqueID가 없으면 복사 필요
        if (string.IsNullOrEmpty(toAdd.uniqueID))
        {
            needsCopy = true;
        }
        else
        {
            // 동일한 uniqueID를 가진 항목이 이미 있으면 새로운 복사본을 만들어서 다른 슬롯에 추가하도록 함
            if(equipmentMap.TryGetValue(toAdd.uniqueID, out var existing))
            {
                needsCopy = true;
            }
        }

        // 전달된 객체와 인벤토리 내 동일 참조가 있으면 복사 필요
        if (needsCopy == false)
        {
            if(equipmentMap.TryGetValue(toAdd.uniqueID, out var existing))
            {
                if (ReferenceEquals(existing, item))
                {
                    needsCopy = true;
                }
            }
        }

        if (needsCopy)
        {
            var copy = item.Copy();
            if (copy != null)
            {
                copy.uniqueID = Guid.NewGuid().ToString();
                copy.SetCount(1);
                toAdd = copy;
            }
            else
            {
                // 안전장치: 복사 실패 시에도 uniqueID 부여(가능한 경우)하여 추가
                if (string.IsNullOrEmpty(toAdd.uniqueID))
                    toAdd.uniqueID = Guid.NewGuid().ToString();
                toAdd.SetCount(1);
            }
        }

        equipmentMap[toAdd.uniqueID] = toAdd;
        base.AddItem(toAdd); 
    }

    public override void RemoveItem(ItemData item)
    {
        if (item == null)
            return;

        equipmentMap.Remove(item.uniqueID); 

        base.RemoveItem(item);
    }

    public ItemData GetEquipment(string uniqueID)
    {
        return equipmentMap.TryGetValue(uniqueID, out var item) ? item : null;
    }
}

public class StackableInventory : Inventory
{
    public override void AddItem(ItemData item)
    {
        if (item == null) return;
        if (item.category == ItemCategory.EQUIPMENT)
        {
            Debug.LogWarning("Cannot add equipment item to StackableInventory.");
            return;
        }
        // 스택형 아이템은 동일 ID가 있으면 수량만 증가시킨다.
        if(itemMap.TryGetValue(item.id, out var existingItem))
        {
            existingItem.ModifyCount(item.GetCount());
        }
        else
        {
            base.AddItem(item);
        }
    }
    public override void RemoveItem(ItemData item)
    {
        if (item == null) return;
        
        if (itemMap.TryGetValue(item.id, out var existingItem))
        {
            existingItem.ModifyCount(-1 * item.GetCount());

            if (existingItem.GetCount() <= 0)
            {
                base.RemoveItem(existingItem);
            }
        }
    }

    public int GetItemCount(int itemID)
    {
        return itemMap.TryGetValue(itemID, out var item) ? item.GetCount() : 0; 
    }
}

public class CurrencyInventory : Inventory
{
    private Dictionary<CurrencyType, CurrencyItem> currencyMap = new();

    public override void AddItem(ItemData item)
    {
        if (item is not CurrencyItem currency)
            return;

        if (currencyMap.TryGetValue(currency.Type,out var exist))
        {
            exist.ModifyCount(currency.GetCount());
        }
        else
        {
            currencyMap[currency.Type] = currency;

            base.AddItem(currency);
        }
    }

    public bool SpendCurrency(CurrencyType type, int amount, Action updatedCurrency = null)
    {
        if (currencyMap.TryGetValue(type, out var currencyItem) == false || currencyItem.GetCount() < amount)
        {
            return false;
        }

        bool success = RemoveItem(currencyItem.id, amount);

        if (success)
            updatedCurrency?.Invoke();

        return success;
    }

    public override void RemoveItem(ItemData item)
    {
        if (item is CurrencyItem currency)
        {
            currencyMap.Remove(currency.Type);
        }

        base.RemoveItem(item);
    }

    public int GetCurrency(CurrencyType type)
    {
        if (currencyMap.TryGetValue(type, out var currencyItem))
        {
            return currencyItem.GetCount();
        }

        return 0;
    }

    public void SetCurrency(CurrencyType type, int amount)
    {
        if(currencyMap.TryGetValue(type, out var currencyItem))
        {
            currencyItem.SetCount(amount); 
        }
    }
}


public sealed class RecordInventory
{
    private List<RecordData> records = new();
    private HashSet<int> recordIdSet = new();
    private Dictionary<string, RecordData> recordUniqueMap = new();

    private Dictionary<int, List<RecordData>> recordIdMap = new();

    public event Action<RecordInventory> OnInventoryChanged;

    public IReadOnlyList<RecordData> Records => records;

    public void AddRecord(RecordData record)
    {
        if (record == null) return;

        if(recordIdSet.Contains(record.id))
        {
            // 이미 존재하는 레코드 이므로 빈 레코드를 리스트에 추가 
            RecordData emptyRecord = AppManager.Instance.GetEmptyRecord();
            if (emptyRecord != null)
            {
                records.Add(emptyRecord);

                OnInventoryChanged?.Invoke(this);
            }
            return; 
        }

        records.Add(record);

        recordUniqueMap[record.uniqueID] = record;

        if (!recordIdMap.TryGetValue(record.id, out var list))
        {
            list = new List<RecordData>();
            recordIdMap.Add(record.id, list);
        }

        list.Add(record);

        recordIdSet.Add(record.id);

        OnInventoryChanged?.Invoke(this);
    }

    public void RemoveRecord(int id)
    {
        if (recordIdMap.TryGetValue(id, out var list))
        {
            if (list.Count > 0)
            {
                RemoveRecord(list[0]);
            }
        }
    }

    public void RemoveRecord(RecordData target)
    {
        if (target == null)
            return;

        if (!recordUniqueMap.TryGetValue(target.uniqueID, out var finalTarget))
            return;

        records.Remove(finalTarget);

        recordUniqueMap.Remove(finalTarget.uniqueID);

        if (recordIdMap.TryGetValue(finalTarget.id, out var list))
        {
            list.Remove(finalTarget);

            if (list.Count == 0)
            {
                recordIdMap.Remove(finalTarget.id);
                recordIdSet.Remove(finalTarget.id);
            }
        }

        OnInventoryChanged?.Invoke(this);
    }

    public void ClearAll()
    {
        records.Clear();

        recordIdSet.Clear();

        recordUniqueMap.Clear();

        recordIdMap.Clear();

        OnInventoryChanged?.Invoke(this);
    }

    public RecordData GetRecord(string uniqueID)
    {
        return recordUniqueMap.TryGetValue(uniqueID, out var value) ? value : null; 
    }
}