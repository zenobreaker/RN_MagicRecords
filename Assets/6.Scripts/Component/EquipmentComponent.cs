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
                var equipment = InventoryManager.Instance.FindItem(data.equipments[(int)e]?.itemUniqueId) as EquipmentItem;
                equipment ??= AppManager.Instance.GetEquipmentItem(data.equipments[(int)e].itemId);

                if (equipment != null)
                    EquipToSlot(equipment);
                else
                    UnequipToSlot(e);
            }
        }

        OnEquippedEquipments?.Invoke(data);
    }
}
