using System;
using UnityEngine;

public class EquipmentComponent : MonoBehaviour
{
    private EquipmentItem[] slots;
    private StatusComponent status; 

    public event Action<CharEquipmentData> OnEquippedEquipments;

    private void Awake()
    {
        status = GetComponent<StatusComponent>();
        slots = new EquipmentItem[6];
    }

    public void EquipToSlot(EquipmentItem item)
    {
        if (item == null) return;

        slots[(int)item.parts] = item;
        slots[(int)item.parts].EquipItem(status); 
    }

    public void UnequipToSlot(EquipParts parts)
    {
        slots[(int)parts]?.UnequipItem(status);  
        slots[(int)parts] = null; 
    }

    public void SertEquipmentData(CharEquipmentData data)
    {
        if(data == null)
        {
            foreach (var slot in slots)
            {
                slot.UnequipItem(status);
            }
        }
        else
        {
            Debug.Log("Set Equipments!! ");
            for(var e = EquipParts.WEAPON;  e < EquipParts.MAX; e++)
            {
                var equipment = data.equipments[(int)e]?.itemId is int id && id != -1 ?
                    AppManager.Instance?.GetEquipmentItem(id) : null;

                if (equipment != null)
                    EquipToSlot(equipment);
                else
                    UnequipToSlot(e);
            }
        }

        OnEquippedEquipments?.Invoke(data);
    }
}
