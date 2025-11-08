using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopJsonData
{
    public int id;
    public int itemID;
    public int price;
    public int currencyType; 
}

[System.Serializable]
public class ShopJsonAllData
{
    public List<ShopJsonData> shopItemJsonData;
}

public class ShopDataBase : DataBase
{
    // key : id
    [SerializeField] private Dictionary<int, ShopItem> shopItems = new();

    private Dictionary<ItemCategory, List<ItemData>> lookupItems = new();

    public override void Initialize()
    {
        if (jsonAsset == null) return;

        Debug.Log("Shop DataBase Init");

        JsonLoader.LoadJsonList<ShopJsonAllData, ShopJsonData, ShopItem>
           (
               jsonAsset,
               root => root.shopItemJsonData,

               json =>
               {
                   var data = new ShopItem(json.id, json.itemID, null, 
                       json.price, (CurrencyType)json.currencyType);
                   return data;
               },

               shopItem=>
               {
                   shopItems.Add(shopItem.id, shopItem);
               }
           );

        Debug.Log("===================================================");
        Debug.Log($"Complete Message => shopitems : {shopItems.Count}");
    }

    public void Initialize_Lookup(ItemDataBase database)
    {
        if (database == null) return;

        lookupItems.Add(ItemCategory.CURRENCY, new List<ItemData>());
        lookupItems.Add(ItemCategory.EQUIPMENT, new List<ItemData>());
        lookupItems.Add(ItemCategory.INGREDIANT, new List<ItemData>());

        foreach (KeyValuePair<int, ShopItem> keyValuePair in shopItems)
        {
            ShopItem shopItem = keyValuePair.Value;

            // 아이템 DB에서 참조 가져오기
            var itemData = database.GetItem(shopItem.TargetItemID);
            if (itemData == null)
            {
                Debug.LogWarning($"Item not found in ItemDB: {shopItem.TargetItemID}");
                continue;
            }

            shopItem = new ShopItem(itemData);
            lookupItems[shopItem.category].Unique(shopItem);
        }
    }

    public ShopItem GetShopItemData(int itemID)
    {
        return (ShopItem)(shopItems.TryGetValue(itemID, out var shopItem) ? shopItem.Copy() : null);
    }

    public List<ItemData> GetShopItems(ItemCategory category)
    {
        return lookupItems.TryGetValue(category, out List<ItemData> items) ? items : null;
    }
}
