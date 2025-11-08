using System;
using UnityEngine;

public abstract class ItemData
{
    public int id;
    public string uniqueID; 
    public Sprite icon;
    public ItemCategory category;

    public string name;
    public string description;
    public int itemCount = 0;
    public ItemData(int id, Sprite icon = null)
    {
        this.id = id;
        this.icon = icon;
        this.uniqueID = Guid.NewGuid().ToString();
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
        StatusType mainStatus, float mainValue, bool isPercent)
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
    }

    public override ItemData Copy()
    {
        // StatModifier도 깊은 복사 (값 복사)
        var newModifier = new StatModifier(modifier.type, modifier.value, modifier.valueType);

        var copy = new EquipmentItem(id, icon, name, description, parts,
            newModifier.type, newModifier.value, newModifier.valueType == ModifierValueType.PERCENT);

        copy.owner = owner;
        copy.uniqueID = uniqueID;
        copy.Eqeuipped = Eqeuipped;
        copy.itemCount = itemCount;

        return copy;
    }

    public void EquipItem(StatusComponent status)
    {
        status?.ApplyBuff(modifier);
    }

    public void UnequipItem(StatusComponent status)
    {
        status?.RemoveBuff(modifier);
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

    public ShopItem(int id, int targetItemID, Sprite icon = null,
        int pirce = 0, CurrencyType currencyType = CurrencyType.NONE) 
        : base(id, icon)
    {
        this.price = pirce;
        this.targetItemID = targetItemID;
        this.currencyType = currencyType;
    }
    public ShopItem(ItemData data) :base(data) 
    { 
        if(data is ShopItem shopItem)
        {
            this.targetItemID = shopItem.targetItemID;
            this.price = shopItem.price;
            this.currencyType = shopItem.currencyType;
        }
    }


    public override ItemData Copy()
    {
        return new ShopItem(id, targetItemID, icon, price, currencyType);
    }
}