using System.Collections.Generic;
using UnityEngine;

public static class EnhanceCalculator 
{
    private static readonly Dictionary<ItemRank, float> getMulitplier = new()
    {
        {ItemRank.COMMON, 1.0f},
        {ItemRank.MAGIC, 1.2f},
        {ItemRank.RARE, 1.5f },
        {ItemRank.UNIQUE, 2.0f},
        {ItemRank.LEGENDARY, 3.0f},
    };

    private const int BASE_COST = 100;
    private const float GROWTH_RATE = 0.25f;

    private static readonly Dictionary<int, float> extraMultipliers=new()
    {
        {10, 1.5f},
        {13, 2.0f},
        {15, 3.0f},
    };

    public static int CalculateEnhanceCost(EquipmentItem equipment, int targetEnhanceLevel)
    {
        if (equipment == null || targetEnhanceLevel <= 0)
            return 0;
     
        float rankMultiplier = getMulitplier.ContainsKey(equipment.rank) ? getMulitplier[equipment.rank] : 1.0f;
        float extraMultiplier = extraMultipliers.ContainsKey(targetEnhanceLevel) ? extraMultipliers[targetEnhanceLevel] : 1.0f;
        float cost = BASE_COST * Mathf.Pow(1 + GROWTH_RATE, targetEnhanceLevel - 1) * rankMultiplier * extraMultiplier;
        
        return Mathf.CeilToInt(cost);
    }

    public static float CaclculateEnhancedStat(EquipmentItem item, List<EnhanceStatData> allStats)
    {
        if (item == null)
            return 0;

        float enhancedValue = 0.0f;
        var valueType = item.modifier.valueType;
        float baseValue = item.modifier.value;
        if (allStats == null)
            return baseValue;

        for (int level = 1; level <= item.Enhance; level++)
        {
            EnhanceStatData stat = allStats.Find(s => s.level == level);
            if (valueType == ModifierValueType.FIXED)
            {
                enhancedValue += stat.flat;
            }
            else if (valueType == ModifierValueType.PERCENT)
            {
                enhancedValue += stat.percent;
            }
        }

        return baseValue + enhancedValue;
    }
}
