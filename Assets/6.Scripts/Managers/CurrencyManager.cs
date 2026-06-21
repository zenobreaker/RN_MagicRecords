using System;
using System.Collections.Generic;

public class CurrencyManager
    : Singleton<CurrencyManager>
{
    private CurrencyInventory currencyInventory = new();

    public event Action OnUpdatedCurrency;

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicate) return;
    }

    public void OnInit(CurrencyInventory inventory)
    {
        if (inventory == null) return;

        currencyInventory = inventory;
        currencyInventory.OnInventoryChanged += CurrencyInventory_OnInventoryChanged;
    }

    private void CurrencyInventory_OnInventoryChanged(Inventory obj)
    {
        OnUpdatedCurrency?.Invoke();
    }

    public void Cheat_AddedCurrenices()
    {
        AddCurrency(CurrencyType.GOLD, 999999);
        AddCurrency(CurrencyType.DIAMOND, 999999);
        AddCurrency(CurrencyType.EXPOLORE_COIN, 999999);
    }

    protected override void SyncDataFromSingleton()
    {
        currencyInventory = Instance.currencyInventory;
    }

    private void AddItem(ItemData item)
    {
        if (item == null || (item is CurrencyItem) == false) return;

        CurrencyItem currency = item as CurrencyItem;
        currencyInventory.AddItem(item);
        
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        CurrencyItem currency = AppManager.Instance.GetCurrencyItemByType(type);
        currency.SetCount(amount);
        AddItem(currency);
    }

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        return currencyInventory.SpendCurrency(type, amount, OnUpdatedCurrency);
    }

    public int GetCurrency(CurrencyType type) => currencyInventory.GetCurrency(type);

    public void ClearExploreCurrency()
    {
        currencyInventory.SetCurrency(CurrencyType.EXPOLORE_COIN, 0); 
    }
}
