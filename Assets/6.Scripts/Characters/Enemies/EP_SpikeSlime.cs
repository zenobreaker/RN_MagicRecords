using System.Collections.Generic;
using UnityEngine;

public class EP_SpikeSlime
    : Enemy_Pattern
{
    [SerializeField]
    private ActiveSkill spikeShot;

    [SerializeField]
    private ActiveSkill normalAttack;



    protected override void DefinePatterns()
    {
        List<PatternCondition> patternConditions = new List<PatternCondition>
        {
            new PatternCondition{
                type = PatternConditionType.Distance, 
                ctype = ComparisonType.GreaterThan, 
                value = 3.0f},
            new PatternCondition{ 
                type = PatternConditionType.Cooldown,
                ctype = ComparisonType.Equal,
                value = 5.0f}
        };

        AddPattern("Slot1", spikeShot, patternConditions);


        patternConditions.Clear();

        patternConditions = new List<PatternCondition>
        {
            new PatternCondition {
                type = PatternConditionType.Distance, 
                ctype = ComparisonType.LessThanOrEqual,
                value = 5.0f },
            new PatternCondition {
                type = PatternConditionType.Cooldown, 
                ctype = ComparisonType.Equal,
                value = 5.0f }
        };

        AddPattern("Slot2", normalAttack, patternConditions);
    }
}
