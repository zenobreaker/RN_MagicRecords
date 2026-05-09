using UnityEngine;

/// <summary>
/// 강화 관련 도메인 서비스 클래스
/// </summary>
public static class EnhanceSystem
{ 
    public static bool CanEnhance(EquipmentItem equip, out string reason)
    {
        reason = string.Empty; 

        EnhanceLevelData data = AppManager.Instance.GetEnhanceLevelData(equip.rank, equip.Enhance + 1);

        if(data == null)
        {
            reason = "ui_toast_not_enhance_max_level";
            return false; 
        }

        // 골드 검사 
        if(data.usegold)
        {
            int gold = CurrencyManager.Instance.GetCurrency(CurrencyType.GOLD);
            int requiredGold = EnhanceCalculator.CalculateEnhanceCost(equip, equip.Enhance + 1);
            if (gold < requiredGold)
            {
                reason = "ui_toast_not_enhance_not_enough_currency";
                return false; 
            }
        }


        // 재료 검사
        if(data.requires != null && data.requires.Count > 0)
        {
            for(int i = 0; i < data.requires.Count; i++)
            {
                int requiredItemId = data.requires[i];
                int requiredItemCount = data.costs[i];
                int ownedItemCount = InventoryManager.Instance.GetItemCount(requiredItemId);
                if(ownedItemCount < requiredItemCount)
                {
                    reason = "ui_toast_not_enhance_not_enough_materials";
                    return false; 
                }
            }
        }

        return true; 
    }

    public static void ApplyEnhance(EquipmentItem equip)
    {
        EnhanceLevelData data = AppManager.Instance.GetEnhanceLevelData(equip.rank, equip.Enhance + 1);
        if(data == null)
        {
            Debug.LogError("Enhance Data Not Found");
            return; 
        }
        // 골드 차감 
        if(data.usegold)
        {
            int requiredGold = EnhanceCalculator.CalculateEnhanceCost(equip, equip.Enhance + 1);
            CurrencyManager.Instance.SpendCurrency(CurrencyType.GOLD, requiredGold);
        }
        // 재료 차감
        if(data.requires != null && data.requires.Count > 0)
        {
            for(int i = 0; i < data.requires.Count; i++)
            {
                int requiredItemId = data.requires[i];
                int requiredItemCount = data.costs[i];
                InventoryManager.Instance.RemoveItem(requiredItemId, requiredItemCount);
            }
        }
        // 강화 적용
        //Debug.Log($"강화 성공 {equip.Enhance + 1}");
        equip.EnhanceItem(equip.Enhance + 1);
    }
}
