using UnityEngine;

public abstract class ItemData
{
    public int id;
    public Sprite icon;
}

public class EquipmentItem : ItemData
{
    public string name;
    public string description;
    public EquipParts parts;
    public StatusType mainStatus;
    public float mainValue;

    public void EquipItem(StatusComponent status)
    {
        status?.SetStatusValue(mainStatus, mainValue);
    }

    public void UnequipItem(StatusComponent status)
    {
        status?.SetStatusValue(mainStatus, mainValue * -1.0f);
    }
}
