using System;
using System.Collections.Generic;

public class CurrencyManager
    : Singleton<CurrencyManager>
{
    private CurrencyInventory currencyInventory = new();

    public event Action OnUpdatedCurrency;

    public void OnInit(CurrencyInventory inventory)
    {
        if (inventory == null) return;

        currencyInventory = inventory;
    }

    public void Cheat_AddedCurrenices()
    {
        AddCurrency(CurrencyType.GOLD, 999999);
        AddCurrency(CurrencyType.DIAMOND, 999999);
    }

    protected override void SyncDataFromSingleton()
    {
        currencyInventory = Instance.currencyInventory;
    }

    public void AddCurrency(ItemData item)
    {
        if (item == null || (item is CurrencyItem) == false) return;

        CurrencyItem currency = item as CurrencyItem;
        AddCurrency(currency.Type, currency.itemCount);
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        currencyInventory.AddCurrency(type, amount, OnUpdatedCurrency);
    }

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        return currencyInventory.SpendCurrency(type, amount, OnUpdatedCurrency);
    }

    public int GetCurrency(CurrencyType type) => currencyInventory.GetCurrency(type);
}
