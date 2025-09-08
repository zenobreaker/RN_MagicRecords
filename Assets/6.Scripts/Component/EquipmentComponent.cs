using UnityEngine;

public class EquipmentComponent : MonoBehaviour
{
    private EquipmentItem[] slots;
    private StatusComponent status;

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
        slots[(int)parts].UnequipItem(status);  
        slots[(int)parts] = null; 
    }
}
