using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EquipmentInfoData
{
    public int slotId;
    public int itemId;
}


[System.Serializable]
public class CharEquipmentData
{
    public int characterId;
    public List<EquipmentInfoData> equipments = new List<EquipmentInfoData>();

    public void Init()
    {
        equipments.Clear();
        equipments.Add(new EquipmentInfoData());
        equipments.Add(new EquipmentInfoData());
        equipments.Add(new EquipmentInfoData());
        equipments.Add(new EquipmentInfoData());
        equipments.Add(new EquipmentInfoData());
        equipments.Add(new EquipmentInfoData());
    }

    public void TestItem()
    {
        equipments[0].itemId = 1000;
        equipments[1].itemId = 2000;
        equipments[5].itemId = 3000;
    }

    public void EquipItem(EquipmentItem item)
    {
        if (item == null) return;

        Debug.Log($"Item equip {characterId} : {item.id}");
        equipments[(int)item.parts].slotId = (int)item.parts;
        equipments[(int)item.parts].itemId = item.id;
    }

    public void UnequipItem(EquipmentItem item)
    {
        if (item == null) return;

        Debug.Log($"Item unequip {characterId} : {item.id}");
        equipments[(int)item.parts].slotId = 0;
        equipments[(int)item.parts].itemId = 0;
    }
}
