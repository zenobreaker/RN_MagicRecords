using UnityEngine;

public static class StatusCalculator
{
    public static float GetFinalStatus(CharStatusData baseStatus, CharEquipmentData equipment, StatusType type)
    {
        float baseValue = baseStatus.GetStatusValue(type);

        float flatTotal = 0.0f;
        float percentTotal = 0.0f;

        if (equipment != null)
        {
            foreach (var eq in equipment.equipments)
            {
                if (string.IsNullOrEmpty(eq.itemUniqueId)) continue;

                var item = InventoryManager.Instance.FindItem(eq.itemUniqueId) as EquipmentItem;
                item ??= AppManager.Instance.GetEquipmentItem(eq.itemId);
                
                if (item == null || item.modifier == null) continue;

                if (item.modifier.type != type) continue;

                if (item.modifier.valueType == ModifierValueType.FIXED)
                    flatTotal += item.modifier.value;
                else if (item.modifier.valueType == ModifierValueType.PERCENT)
                    percentTotal *= (1f + item.modifier.value);
            }
        }

        float finalValue = (baseValue + flatTotal) * (1f + percentTotal);
        return finalValue;
    }
}
