using UnityEngine;

public abstract class ItemData
{
    public int id;
    public Sprite icon;
    public ItemCategory category; 
    public ItemData(int id, Sprite icon)
    {
        this.id = id;
        this.icon = icon;
    }
}

public class EquipmentItem : ItemData
{
    public string name;
    public string description;
    public EquipParts parts;
    public StatModifier modifier; 

    public EquipmentItem(int id, Sprite icon, string name, string description, EquipParts parts,
        StatusType mainStatus, float mainValue, bool isPercent)
        :base(id, icon)
    {
        this.name = name;
        this.description = description;
        this.parts = parts;
        modifier = new StatModifier(mainStatus, mainValue
            , isPercent == true ? ModifierValueType.Percent : ModifierValueType.Fixed);

        category = ItemCategory.Equipment;
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
