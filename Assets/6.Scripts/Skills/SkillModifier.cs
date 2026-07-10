using System;
using System.Collections.Generic;
using UnityEngine;

public enum RecordModifierTarget
{
    PlayerStat,
    Skill
}

public enum SkillModifierType
{
    Damage,
    Cooldown,
    Range,
    ProjectileCount,
    ProjectileSpeed,
    AttackCount,
    Duration,
    ExplosionRadius,
    CriticalChance,
    CriticalDamage,
    ManaCost,
}

public enum ModifierOperation
{
    Add,
    Multiply,
    Override
}



[Serializable]
public sealed class BaseValues
{
    public int PatternCount;
    public float PatternAngle;

    public DamageData Damage;
    public float Range;
    public float Cooldown;
    public float DamageMultiplier = 1.0f;
    //public float ProjectileSpeed;
}

[Serializable]
public class SkillModifier
{
    public int SkillId;

    public SkillModifierType ModifierType;

    public ModifierOperation Operation;

    public float Value;
}

public sealed class ModifierContext
{
    public readonly List<SkillModifier> Modifiers = new();
}


[Serializable]
public sealed class CastContext
{
    public Character Caster;
    public Character Target;

    public Vector3 CastPosition;
    public Vector3 CastDireciton;
    public Vector3 TargetPosition;

    public float CastingTime;
    public float ChargedTime;

    public float MaxCastingTime; 
    public float MaxChargeTime;        
    public bool AutoFireOnMaxCharge = false;  
    public bool IsInstantCast = false;        

    public Quaternion Direction;
}

[Serializable]
public sealed class SpawnContext
{
    public float SearchRadius;

    public int ChainCount;

    public float ExplosionRadius;

    public float Lifetime;

    public Vector3 TargetPosition;
    public List<Vector3> TargetPositions;
    public string OverridePrefabName = string.Empty; 
}

[Serializable]
public sealed class CombatContext
{
    public int PatternCountBonus;

    public float PatternAngleBonus;
    public float BonusMultipiler = 1.0f;

    public float CriticalDamageMultiplier;

    public bool IsCritical;
}


public sealed class SkillRuntimeContext
{
    public BaseValues Base = new();
    public CastContext Cast = new();
    public SpawnContext Spawn = new();
    public ModifierContext Modifier = new();
    public CombatContext Combat = new();

    public int PatternCount
    {
        get
        {
            return Base.PatternCount + Combat.PatternCountBonus;
        }
    }

    public float PatternAngle
    {
        get
        {
            return Base.PatternAngle + Combat.PatternAngleBonus;
        }
    }

    public float DamageMultiplier
    {
        get
        {
            return Base.DamageMultiplier * Combat.BonusMultipiler;
        }
    }
}



