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
}
