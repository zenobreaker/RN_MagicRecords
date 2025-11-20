using System;
using Unity.VisualScripting;
using UnityEngine;

public abstract class ItemData
{
    public int id;
    public string uniqueID; 
    public Sprite icon;
    public ItemCategory category;
    public ItemRank rank; 

    public string name;
    public string description;
    protected int itemCount = 0;

    public event Action<ItemData> OnChanged;
    public ItemData(int id, Sprite icon = null)
    {
        this.id = id;
        this.icon = icon;
        this.uniqueID = Guid.NewGuid().ToString();
        rank = ItemRank.NONE;
    }

    public ItemData (ItemData other)
    {
        id = other.id;
        uniqueID = other.uniqueID;
        icon = other.icon;
        category = other.category;
        name = other.name;
        description = other.description;
        itemCount = other.itemCount;
        rank = other.rank;  
    }

    public int GetCount() => itemCount;
    public virtual void SetCount(int newCount)
    {
        itemCount = newCount;
        NotifyChanged();
    }

    public virtual void ModifyCount(int delta)
    {
        itemCount += delta;
        if (itemCount < 0) itemCount = 0;
        NotifyChanged();
    }

    protected void NotifyChanged()
    {
        OnChanged?.Invoke(this);
    }

    public abstract ItemData Copy();
}

public class EquipmentItem : ItemData
{
    public EquipParts parts;
    public StatModifier modifier;
    
    public int owner = 0; 
    public bool Eqeuipped = false;

    private int enhance; 
    public int Enhance { get { return enhance; } }

    public EquipmentItem(int id, Sprite icon, string name, string description, EquipParts parts,
        ItemRank rank, StatusType mainStatus, float mainValue, bool isPercent)
        :base(id, icon)
    {
        this.name = name;
        this.description = description;
        this.parts = parts;
        modifier = new StatModifier(mainStatus, mainValue
            , isPercent == true ? ModifierValueType.PERCENT : ModifierValueType.FIXED);

        category = ItemCategory.EQUIPMENT;
        itemCount = 1;
        enhance = 0;
        this.rank = rank;
    }

    public override ItemData Copy()
    {
        // StatModifier도 깊은 복사 (값 복사)
        var newModifier = new StatModifier(modifier.type, modifier.value, modifier.valueType);

        var copy = new EquipmentItem(id, icon, name, description, parts, rank,
            newModifier.type, newModifier.value, newModifier.valueType == ModifierValueType.PERCENT);

        copy.owner = owner;
        copy.uniqueID = uniqueID;
        copy.Eqeuipped = Eqeuipped;
        copy.itemCount = itemCount;

        return copy;
    }

    public string GetMainOptionText()
    {
        return modifier.GetFullValue();
    }

    public void EquipItem(StatusComponent status)
    {
        status?.ApplyBuff(modifier);
    }

    public void UnequipItem(StatusComponent status)
    {
        status?.RemoveBuff(modifier);
    }

    public void EnhanceItem(int enhanceLevel, bool loaded = false)
    {
        enhance = enhanceLevel;
        Debug.Log($"강화 전 {modifier.value}");
        modifier.value = EnhanceCalculator.CaclculateEnhancedStat(this, 
            AppManager.Instance.GetEnhanceStatDatas(rank));
        Debug.Log($"강화 후 {modifier.value}");
        if (loaded)
            return;
        NotifyChanged();
    }
}

public class IngredientItem
    : ItemData
{
    public IngredientItem(int id, Sprite icon, int itemCategory) 
        : base(id, icon)
    {
        category = (ItemCategory)itemCategory;
        itemCount = 1; 
    }

    public IngredientItem(int id, Sprite icon, ItemCategory category = ItemCategory.INGREDIANT) 
        : base(id, icon)
    {
        this.category = category;
        itemCount = 1; 
    }

    public override ItemData Copy()
    {
        var copy = new IngredientItem(id, icon, category);
        copy.uniqueID = uniqueID;
        copy.itemCount = itemCount;
        return copy;
    }
}

public class CurrencyItem
    : ItemData
{
    private CurrencyType type;
    public CurrencyType Type { get => type; }
    public CurrencyItem(int id, Sprite icon, CurrencyType type)
        : base(id, icon)
    {
        category = ItemCategory.CURRENCY;
        this.type = type;
    }

    public override ItemData Copy()
    {
        var copy = new CurrencyItem(id, icon, Type);
        copy.uniqueID = uniqueID;
        copy.itemCount = itemCount;
        return copy;
    }
}


public class ShopItem : ItemData
{
    private int targetItemID;
    public int TargetItemID => targetItemID;
    private CurrencyType currencyType;
    public CurrencyType CurrencyType => currencyType;
    private int price;
    public int Price => price; 
    public ItemData TargetItemData { get; private set; }

    public ShopItem(int id, int targetItemID, Sprite icon = null,
        int price = 0, CurrencyType currencyType = CurrencyType.NONE) 
        : base(id, icon)
    {
        this.price = price;
        this.targetItemID = targetItemID;
        this.currencyType = currencyType;
    }
    public ShopItem(ItemData data) :base(data) 
    { 
        if(data is ShopItem shopItem)
        {
            this.targetItemID = shopItem.targetItemID;
            this.price = shopItem.Price;
            this.currencyType = shopItem.currencyType;
        }
    }

    public void SetShopData(ShopItem shopItem)
    {
        if (shopItem == null) return;

        this.targetItemID = shopItem.targetItemID;
        this.price = shopItem.Price;
        this.currencyType = shopItem.currencyType;
    }

    public void ResolveTargetItem(ItemDataBase database)
    {
        if (database == null) return;
        TargetItemData = database.GetItem(TargetItemID);
    }

    public override ItemData Copy()
    {
        return new ShopItem(id, targetItemID, icon, price, currencyType);
    }
}