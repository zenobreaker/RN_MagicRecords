using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharEquipment : UiBase
{
    [SerializeField] Sprite emptySlot;
    [SerializeField] Image[] charEquipmentsSlots;

    public void OnDrawCharEquipment(CharEquipmentData ce)
    {
        if ( ce == null)
        {
            // 빈 이미지로 처리 
            return;
        }
        if (charEquipmentsSlots.Length != ce.equipments.Count) return;

        for (var e = EquipParts.WEAPON; e < EquipParts.MAX; e++)
        {
            var equipment = InventoryManager.Instance.FindItem(ce.equipments[(int)e]?.itemUniqueId);
            
            if(equipment != null)
            {
                charEquipmentsSlots[(int)e].sprite = equipment.icon;
            }
            else
            {
                // 빈 이미지로 처리 
                charEquipmentsSlots[(int)e].sprite = emptySlot;
            }
        } // for(e)
    }
}
