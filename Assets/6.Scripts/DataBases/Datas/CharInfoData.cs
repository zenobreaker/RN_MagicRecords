using UnityEngine;

[System.Serializable]
public class StatGrowth
{
    public float baseValue;
    public float growth;
    public float bonus;
    public float exponent;

    public StatGrowth(float baseValue = 0.0f, float growth = 0.0f, float bonus = 0.0f, float exponent = 0.0f)
    {
        this.baseValue = baseValue;
        this.growth = growth;
        this.bonus = bonus;
        this.exponent = exponent;
    }

    public float GetValue(int level)
    {
        return baseValue
            + (level * growth)
            + (bonus * Mathf.Pow(level, exponent));
    }
}


[System.Serializable]
public class CharStatusData
{
    public int id;
    public int level;

    public StatGrowth hp;
    public StatGrowth attack;
    public StatGrowth defense;
    public StatGrowth attackSpeed;
    public StatGrowth moveSpeed;
    public StatGrowth critical;
    public StatGrowth critDamage;

    public virtual float GetStatusValue(StatusType type) { return 0.0f; }
}

public class TurtleInfoData
    : CharStatusData
{
    public TurtleInfoData(int id, int level) : base()
    {
        this.id = id;
        this.level = level;
        hp = new StatGrowth(100, 1.0f, 1.5f, 1.0f);
        attack = new StatGrowth(10, 1.0f, 1.5f, 1.0f);
        defense = new StatGrowth(10, 1.0f, 1.5f, 1.0f);
        attackSpeed = new StatGrowth(1.0f);
        moveSpeed = new StatGrowth(1.0f);
        critical = new StatGrowth();
        critDamage = new StatGrowth(1.5f);
    }

    public override float GetStatusValue(StatusType type)
    {
        float value = 0.0f;
        return type switch
        {
            StatusType.ATTACK => attack.GetValue(level),
            StatusType.DEFENSE => defense.GetValue(level),
            StatusType.ATTACKSPEED => attackSpeed.GetValue(level),
            StatusType.MOVESPEED => moveSpeed.GetValue(level),
            StatusType.CRIT_RATIO => critical.GetValue(level),
            StatusType.CRIT_DMG => critDamage.GetValue(level),
            StatusType.HEALTH => hp.GetValue(level),
            _ => value,
        };
    }
}

