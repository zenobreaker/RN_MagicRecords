using Mono.Cecil;
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
    public int TotalShots;

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


public struct SpawnPointData
{
    public Vector3 position;
    public Quaternion rotation;

    public SpawnPointData(
        Vector3 position,
        Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }
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

    // 어디서 생성?
    public List<SpawnPointData> SpawnPoints = new();
    
    // 이번 스킬에서 사용할 총구 인덱스
    public int SelectedSpawnPointIndex;
    public List<int> SelectedSpawnPointIndices = new();

    public void Clear()
    {
        SpawnPoints.Clear();
        TargetPositions.Clear();

        TargetPosition = Vector3.zero;
    }
}

[Serializable]
public sealed class CombatContext
{
    public int PatternCountBonus;

    public float PatternAngleBonus;
    public int TotalShotsBonus;
    public float FireIntervalMultiplier = 1.0f;
    public float BonusMultipiler = 1.0f;

    public float CriticalDamageMultiplier;

    public bool IsCritical;
}

[Serializable]
public sealed class HitContext
{
    public HashSet<GameObject> HitTargets { get; } = new();
    public HashSet<GameObject> Ignores { get; } = new();

    public bool IsDectecting { get; set; }

    public void Begin()
    {
        IsDectecting = true; 
        HitTargets.Clear(); 
    }

    public void End()
    {
        IsDectecting = false; 
    }

    public void AddIgnore(GameObject target)
    {
        if (target != null)
            Ignores.Add(target); 
    }

    public bool HasHit(GameObject target)
    { 
        return target != null && HitTargets.Contains(target);
    }

    public bool TryAddTarget(GameObject target)
    {
        return target != null && HitTargets.Add(target); 
    }

    public void ClearTargets()
    {
        HitTargets.Clear(); 
    }

    public void Reset()
    {
        IsDectecting = false; 
        HitTargets.Clear(); Ignores.Clear();
    }
}

[System.Serializable]
public sealed class SkillChainContext
{
    public Vector3 Position;
    public Quaternion Rotation;

    public int PatternIndex;

    public WarningSign WarningSign;
}

public sealed class SkillRuntimeContext
{
    private readonly Dictionary<string, int> phaseLoopCounts = new();
    internal Module_PhaseLoop ActivePhaseLoop;
    internal int PhaseLoopSourceIndex;
    internal int PhaseLoopTargetIndex;
    internal int PhaseLoopLastVersion = -1;

    public int GetPhaseLoopCount(string key) =>
        !string.IsNullOrEmpty(key) && phaseLoopCounts.TryGetValue(key, out int count) ? count : 0;

    public int IncrementPhaseLoopCount(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        int count = GetPhaseLoopCount(key) + 1;
        phaseLoopCounts[key] = count;
        return count;
    }

    public void ResetPhaseLoopCount(string key)
    {
        if (!string.IsNullOrEmpty(key)) phaseLoopCounts.Remove(key);
    }

    public void ResetPhaseLoopCounts()
    {
        phaseLoopCounts.Clear();
        ActivePhaseLoop = null;
        PhaseLoopLastVersion = -1;
    }

    // Per-cast movement values; passives can modify these before phase entry.
    public float DashDistanceMultiplier = 1f;
    public float DashDurationMultiplier = 1f;
    public Vector3 MovementDirection;
    public bool IsBackwardMovement;

    public BaseValues Base = new();
    public CastContext Cast = new();
    public SpawnContext Spawn = new();
    public ModifierContext Modifier = new();
    public CombatContext Combat = new();
    public HitContext Hit = new();
    public SkillChainContext SkillChain = new();

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

    public int TotalShots
    {
        get
        {
            return Base.TotalShots + Combat.TotalShotsBonus;
        }
    }

    public float FireIntervalMultiplier
    {
        get
        {
            return Mathf.Max(0.01f, Combat.FireIntervalMultiplier);
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



