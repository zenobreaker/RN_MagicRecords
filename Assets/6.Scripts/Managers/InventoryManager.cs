using System.Collections.Generic;

[System.Serializable]
public enum ItemCategory
{
    None, 
    Equipment,
    Ingrediant,
    Max, 
}

public class InventoryManager : Singleton<InventoryManager>
{
    private List<ItemData> items = new();
    private Dictionary<ItemCategory, List<ItemData>> categoriesItems = new();

    protected override void Awake()
    {
        categoriesItems.Clear();
        categoriesItems.Add(ItemCategory.Equipment, new List<ItemData>());
        categoriesItems.Add(ItemCategory.Ingrediant, new List<ItemData>());

        AppManager.Instance.OnAwaked += () =>
        {
            // Test
            var item = AppManager.Instance.GetEquipmentItem(1000);
            AddItem(item);
            item = AppManager.Instance.GetEquipmentItem(2000);
            AddItem(item);
            item = AppManager.Instance.GetEquipmentItem(3000);
            AddItem(item);
        };
    }

    public void AddItem(ItemData item)
    {
        items.Add(item);
        categoriesItems[item.category].Add(item);
    }

    public void RemoveItem(ItemData item)
    {
        items.Remove(item);
        categoriesItems[item.category].Remove(item);
    }

    public ItemData FindItem(int itemId)
    {
        return items[itemId];
    }

    public List<ItemData> GetItems(ItemCategory category)
    {
        if(categoriesItems.TryGetValue(category, out var value))
            return value;
        return null;
    }
}
