using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Inventory
{
    protected List<ItemData> items = new();

    public event Action<Inventory> OnInventoryChanged;

    public virtual void AddItem(ItemData item)
    {
        items.Add(item);

        item.OnChanged += HandleItemChanged;
        OnInventoryChanged?.Invoke(this);
    }

    public virtual void RemoveItem(ItemData item)
    {
        items.Remove(item);
    }

    public virtual bool RemoveItem(int itemID, int itemCount)
    {
        ItemData item = items.Find(x => x.id == itemID);
        if (item == null) return false;

        item.ModifyCount(-1 * itemCount);
        if (item.GetCount() <= 0)
            RemoveItem(item);

        OnInventoryChanged?.Invoke(this);

        return true;
    }

    public virtual List<ItemData> GetItems() => items;

    public virtual ItemData GetItem(int itemID)
    {
        return items.Find(x => x.id == itemID);
    }

    public void HandleItemChanged(ItemData item)
    {
        OnInventoryChanged?.Invoke(this);
    }

    public virtual void SaveInventory()
    {

    }
}

public class EquipmentInventory : Inventory
{
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
            foreach (var existing in items)
            {
                if (existing != null && existing.category == ItemCategory.EQUIPMENT && existing.uniqueID == toAdd.uniqueID)
                {
                    needsCopy = true;
                    break;
                }
            }
        }

        // 전달된 객체와 인벤토리 내 동일 참조가 있으면 복사 필요
        if (!needsCopy)
        {
            foreach (var existing in items)
            {
                if (ReferenceEquals(existing, item))
                {
                    needsCopy = true;
                    break;
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

        base.AddItem(toAdd);
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
        var existingItem = items.Find(i => i != null && i.id == item.id);
        if (existingItem != null)
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
        var existingItem = items.Find(i => i != null && i.id == item.id);
        if (existingItem != null)
        {
            existingItem.ModifyCount(-1 * item.GetCount());
            if (existingItem.GetCount() <= 0)
            {
                items.Remove(existingItem);
            }
        }
    }

    public int GetItemCount(int itemID)
    {
        var existingItem = items.Find(i => i != null && i.id == itemID);
        if (existingItem != null)
        {
            return existingItem.GetCount();
        }
        return 0;
    }
}

public class CurrencyInventory : Inventory
{
    public void AddCurrency(CurrencyType type, int amount, Action updateCurrency = null)
    {
        var currencyItem = items.Find(i => (i as CurrencyItem)?.Type == type) as CurrencyItem;
        if (currencyItem != null)
        {
            currencyItem.ModifyCount(+amount);
        }
        else
        {
            var currency = AppManager.Instance?.GetCurrencyItemByType(type);
            if (currency != null)
            {
                currency.SetCount(amount);
                AddItem(currency);
            }
        }

        updateCurrency?.Invoke();
    }

    public bool SpendCurrency(CurrencyType type, int amount, Action updatedCurrency = null)
    {
        var currencyItem = items.Find(i => (i as CurrencyItem)?.Type == type) as CurrencyItem;
        if (currencyItem == null)
            return false;

        bool success = RemoveItem(currencyItem.id, amount);

        if (success)
            updatedCurrency?.Invoke();

        return success;
    }

    public int GetCurrency(CurrencyType type)
    {
        var currencyItem = items.Find(i => (i as CurrencyItem)?.Type == type) as CurrencyItem;
        if (currencyItem != null)
        {
            return currencyItem.GetCount();
        }

        return 0;
    }
}
