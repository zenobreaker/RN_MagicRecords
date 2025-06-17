using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum PatternConditionType
{
    Cooldown, 
    Health, 
    ElapseTime,
    Distance, 
    Custom,
}

public enum ComparisonType
{
    GreaterThan,
    LessThan,
    Equal,
    NotEqual,
    GreaterThanOrEqual,
    LessThanOrEqual
}

[System.Serializable]
public struct PatternCondition 
{
    public PatternConditionType type; 
    public ComparisonType ctype ; 
    public float value;

}


[System.Serializable]
public class PatternEntry
{
    public List<PatternCondition> conditions = new List<PatternCondition>();
    public ActiveSkill skill;

    private bool bCanUse = false;
    public bool CanUse => bCanUse;

    private float cooldown;
    private float lastUsedTime; 

    private  readonly Dictionary<PatternConditionType, IPatternCondition> strategyTable = new();

    public bool IsCooldownReady(float currentTime)
        => currentTime - lastUsedTime >= cooldown;
    public void MarkCooldonwUsed(float currentTime)
        => lastUsedTime = currentTime;

    public void InitiaizePattern()
    {
        foreach (var condition in conditions)
        {
            IComparisonOperator op = ComparisonOperatorFactory.Create(condition.ctype);

            IPatternCondition instance = condition.type switch
            {
                PatternConditionType.Cooldown => new CooldownCondition(op, condition.value),
                PatternConditionType.Health => new HealthCondition(op, condition.value),
                PatternConditionType.Distance => new DistanceCondition(op, condition.value),
                _ => throw new NotImplementedException()
            };

            strategyTable[condition.type] = instance;
        }
    }

    public void Update(float time, AIContext ctx)
    {
        foreach (var pair in strategyTable)
        {
            if (pair.Value is IUpdatableCondition updatable)
                updatable.Update(time);
        }
    }

    public void CheckUsePattern(AIContext ctx)
    {
        bCanUse = true;

        foreach (var condition in conditions)
        {
            if (strategyTable.TryGetValue(condition.type, out IPatternCondition value))
            {
                bCanUse &= value.Evaluate(condition.value, ctx);
            }
        }
    }

}
