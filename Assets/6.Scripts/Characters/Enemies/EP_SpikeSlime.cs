using System.Collections.Generic;
using UnityEngine;

public class EP_SpikeSlime
    : Enemy_Pattern
{
    [SerializeField]
    private SO_ActiveSkillData spikeShot;

    [SerializeField]
    private SO_ActiveSkillData jumpPress;



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

        ActiveSkill spikeShotSkill = new SpikeShot(spikeShot);

        AddPattern("Slot1", spikeShotSkill, patternConditions);


        patternConditions.Clear();

        patternConditions = new List<PatternCondition>
        {
            new PatternCondition {
                type = PatternConditionType.Distance, 
                ctype = ComparisonType.LessThanOrEqual,
                value = 3.0f },
            new PatternCondition {
                type = PatternConditionType.Cooldown, 
                ctype = ComparisonType.Equal,
                value = 10.0f }
        };

        ActiveSkill jumpPressSkill = new JumpPress(jumpPress);
        AddPattern("Slot2", jumpPressSkill, patternConditions);
    }
}
