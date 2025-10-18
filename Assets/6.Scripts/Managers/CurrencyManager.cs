using System;
using System.Collections.Generic;

public class CurrencyManager 
    : Singleton<CurrencyManager>
{

    private Dictionary<CurrencyType, int> currencies = new();
    public event Action OnUpdatedCurrency;
    protected override void Awake()
    {
        base.Awake();
        currencies.Clear();

        currencies[CurrencyType.GOLD] = 0;
        currencies[CurrencyType.DIAMOND] = 0;
        currencies[CurrencyType.EXPOLORE_CREDIT] = 0;
    }

    protected override void SyncDataFromSingleton()
    {
        currencies = Instance.currencies;
    }

    public void AddCurrency(ItemData item)
    {
        if (item == null || (item is CurrencyItem) == false) return;

        CurrencyItem currency = item as CurrencyItem;
        AddCurrency(currency.Type, currency.itemCount);
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        if (!currencies.ContainsKey(type))
            currencies[type] = 0;
        currencies[type] += amount;
        OnUpdatedCurrency?.Invoke();
    }

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        if(currencies.TryGetValue(type, out var current) && current >= amount)
        {
            currencies[type] -= amount;
            OnUpdatedCurrency?.Invoke();
            return true; 
        }
        return false; 
    }

    public int GetCurrency(CurrencyType type) => currencies.TryGetValue(type, out var v) ? v : 0;
}
