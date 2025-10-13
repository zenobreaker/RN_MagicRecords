using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyDataTable", menuName = "Game Data/Currency Table")]
public class SO_CurrecnyDataTable : ScriptableObject
{
    [System.Serializable]
    public class CurrencyData
    {
        public int id;
        public Sprite icon;
        public string displayName; 
    }

    public List<CurrencyData> currencies = new();
    private Dictionary<int, CurrencyData> lookup; 

    public CurrencyData GetCurrency(int id)
    {
        lookup ??= currencies.ToDictionary(c => c.id, c => c);
        return lookup.TryGetValue(id, out var data) ? data : null;
    }
}
