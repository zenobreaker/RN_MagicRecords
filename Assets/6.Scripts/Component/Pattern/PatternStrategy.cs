using UnityEngine;

public class AIContext
{
    public float timeSinceLastPattern;
    public float CurrentHP;
    public Vector3 SelfPosition;
    public Vector3 PlayerPosition; 
}

public interface IPatternCondition
{
    bool Evaluate(float value, AIContext ctx);
}

public interface IUpdatableCondition : IPatternCondition
{
    void Update(float deltaTime);
}

public interface IComparisonOperator
{
    bool Compare(float lhs, float rhs);
}

public abstract class PatternConditionBase : IPatternCondition
{
    protected IComparisonOperator comparisonOperator;
    protected float targetValue;

    protected PatternConditionBase(IComparisonOperator comparisonOperator, float targetValue)
    {
        this.comparisonOperator = comparisonOperator;
        this.targetValue = targetValue;
    }

    protected abstract float GetContextValue(AIContext ctx);

    public bool Evaluate(float value, AIContext ctx)
    {
        float contextValue = GetContextValue(ctx);
        return comparisonOperator.Compare(contextValue, targetValue);
    }
}

// Cooldown ���°� �����Ƿ� ������ �ٸ�
public class CooldownCondition : IUpdatableCondition
{
    private float cooldown = 0.0f;
    private float maxCooldown;
    private readonly IComparisonOperator comparisonOperator;

    public CooldownCondition(IComparisonOperator comparisonOperator, float targetValue)
    {
        this.comparisonOperator = comparisonOperator;
        this.maxCooldown = targetValue;
    }

    public void ResetCooldown() => cooldown = 0.0f;

    public void Update(float deltaTime)
    {
        cooldown = Mathf.Clamp(cooldown + deltaTime, 0.0f, maxCooldown);
    }

    public bool Evaluate(float value, AIContext ctx)
    {
        return comparisonOperator.Compare(cooldown, maxCooldown);
    }
}

public class HealthCondition : PatternConditionBase
{
    public HealthCondition(IComparisonOperator comparisonOperator, float targetValue) 
        : base(comparisonOperator, targetValue)
    {
    }

    protected override float GetContextValue(AIContext ctx)
        => ctx.CurrentHP;
}

public class DistanceCondition : PatternConditionBase
{
    public DistanceCondition(IComparisonOperator comparisonOperator, float targetValue) 
        : base(comparisonOperator, targetValue)
    {
    }

    protected override float GetContextValue(AIContext ctx)
    => Vector3.Distance(ctx.SelfPosition, ctx.PlayerPosition );
}


/////////////////////////////////////////////////////////////////////////////////////////
///

public class GreaterThanOperator : IComparisonOperator
{
    public bool Compare(float lhs, float rhs) => lhs > rhs;
}

public class LessThanOperator : IComparisonOperator
{
    public bool Compare(float lhs, float rhs) => lhs < rhs;
}

public class EqualOperator : IComparisonOperator
{
    public bool Compare(float lhs, float rhs) => lhs == rhs;
}

public class NotEqualOperator : IComparisonOperator
{
    public bool Compare(float lhs, float rhs) => lhs != rhs;
}

public class GreaterThanOrEqualOperator : IComparisonOperator
{
    public bool Compare(float lhs, float rhs) => lhs >= rhs;
}

public class LessThanOrEqualOperator : IComparisonOperator
{
    public bool Compare(float lhs, float rhs) => lhs <= rhs;
}


/// Comparison Factory

public static class ComparisonOperatorFactory
{
    public static IComparisonOperator Create(ComparisonType type)
    {
        return type switch
        {
            ComparisonType.GreaterThan => new GreaterThanOperator(),
            ComparisonType.LessThan => new LessThanOperator(),
            ComparisonType.Equal => new EqualOperator(),
            ComparisonType.NotEqual => new NotEqualOperator(),
            ComparisonType.GreaterThanOrEqual => new GreaterThanOrEqualOperator(),
            ComparisonType.LessThanOrEqual => new LessThanOrEqualOperator(),
            _ => throw new System.NotImplementedException(),
        };
    }
}