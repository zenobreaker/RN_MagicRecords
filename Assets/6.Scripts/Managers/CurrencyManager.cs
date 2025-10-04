using System.Collections.Generic;

public class CurrencyManager 
    : Singleton<CurrencyManager>
{

    private Dictionary<CurrencyType, int> currencies = new();

    protected override void SyncDataFromSingleton()
    {
        currencies = Instance.currencies;
    }

    public void AddCurrency(CurrencyType type, int amount)
    {
        if (!currencies.ContainsKey(type))
            currencies[type] = 0;
        currencies[type] += amount; 
    }

    public bool SpendCurrency(CurrencyType type, int amount)
    {
        if(currencies.TryGetValue(type, out var current) && current >= amount)
        {
            currencies[type] -= amount;
            return false; 
        }
        return false; 
    }

    public int GetCurrency(CurrencyType type) => currencies.TryGetValue(type, out var v) ? v : 0;
}
