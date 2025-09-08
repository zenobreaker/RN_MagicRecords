using UnityEngine;

public abstract class ItemData
{
    public int id;
    public Sprite icon;

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
        
    }

    //TODO : StatusComponent가 아닌 CharEquipment 클래스에 전달해야 한다.
    public void EquipItem(StatusComponent status)
    {
     //   status?.SetStatusValue(mainStatus, mainValue);
    }

    public void UnequipItem(StatusComponent status)
    {
       // status?.SetStatusValue(mainStatus, mainValue * -1.0f);
    }
}
