using UnityEngine;

public abstract class ItemData
{
    public int id;
    public Sprite icon;
    public ItemCategory category;

    public string name;
    public string description;
    public int itemCount = 0;
    public ItemData(int id, Sprite icon)
    {
        this.id = id;
        this.icon = icon;
    }
}

public class EquipmentItem : ItemData
{
    public EquipParts parts;
    public StatModifier modifier;
    
    public int owner = 0; 
    public bool Eqeuipped = false; 

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
    }

    public IngredientItem(int id, Sprite icon, ItemCategory category = ItemCategory.INGREDIANT) 
        : base(id, icon)
    {
        this.category = category;
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
}