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


[System.Flags]
public enum CharacterJobMask
{
    None = 0,
    BulletShooter = 1 << 0,
    MagicSwordsman = 1 << 1,
    Summoner = 1 << 2,
    All = ~0,
}

/// <summary>
/// Character base-stat data authored as a ScriptableObject asset.
/// Job IDs are mapped to mask bits as (jobId - 1), so job ID 1 maps to BulletShooter.
/// </summary>
[CreateAssetMenu(fileName = "CharInfoData", menuName = "Scriptable Objects/Character/Char Info Data")]
public class CharStatusData : ScriptableObject
{
    public int id;
    public int level = 1;

    [Tooltip("Select every job this character can use. Job ID 1 maps to the first flag, job ID 2 to the second, and so on.")]
    public CharacterJobMask availableJobs = CharacterJobMask.All;

    public StatGrowth hp = new();
    public StatGrowth attack = new();
    public StatGrowth defense = new();
    public StatGrowth attackSpeed = new();
    public StatGrowth moveSpeed = new();
    public StatGrowth critical = new();
    public StatGrowth critDamage = new(1.0f);

    public bool CanUseJob(int jobId)
    {
        if (jobId <= 0 || jobId > 32)
            return false;

        int jobBit = 1 << (jobId - 1);
        return ((int)availableJobs & jobBit) != 0;
    }

    public float GetStatusValue(StatusType type)
    {
        return type switch
        {
            StatusType.ATTACK => attack.GetValue(level),
            StatusType.DEFENSE => defense.GetValue(level),
            StatusType.ATTACKSPEED => attackSpeed.GetValue(level),
            StatusType.MOVESPEED => moveSpeed.GetValue(level),
            StatusType.CRIT_RATIO => critical.GetValue(level),
            StatusType.CRIT_DMG => critDamage.GetValue(level),
            StatusType.HEALTH => hp.GetValue(level),
            _ => 0.0f,
        };
    }
}

