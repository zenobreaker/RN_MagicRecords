using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharEquipment : UiBase
{
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
            var equipment = ce.equipments[(int)e]?.itemId is int id && id != -1
            ? AppManager.Instance?.GetEquipmentItem(id)
            : null;
            
            if(equipment != null)
            {
                charEquipmentsSlots[(int)e].sprite = equipment.icon;
            }
            else
            {
                // 빈 이미지로 처리 
            }
        } // for(e)
    }
}
