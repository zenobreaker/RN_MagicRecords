using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ModuleCategory("Record/디버프/표식 부여")]
public sealed class RecordMarkModule : RecordEventModule
{
    public SO_BaseEffect debuff;
    public float chance = 0.3f;
    public float duration = 5f;
    public float damageBonus = 0.1f;
    protected override void AfterOwnHit(GameObject target, DamageEvent hit, float damage)
    {
        if (UnityEngine.Random.value < chance && target.GetComponent<HealthPointComponent>()?.Dead == false)
        {
            target.GetComponent<EffectComponent>()?.ApplyEffect(new RecordMarkEffect("RecordMark", debuff, duration, damageBonus), target, owner);
        }
    }
}

public static class RecordDebuffFactory
{
    public static BaseEffect RandomEffect(SO_BaseEffect[] definitions, float duration, float power)
    {
        if (definitions == null || definitions.Length == 0)
        {
            return null;
        }
        SO_BaseEffect selected = definitions[UnityEngine.Random.Range(0, definitions.Length)];
        if (selected == null)
        {
            return null;
        }
        if (selected.id == "Stun" || selected.id == "Ice")
        {
            return new RecordControlEffect(selected.id, selected, duration,
                selected.id == "Stun" ? StatusEffectType.Stun : StatusEffectType.Ice);
        }
        float value = selected.id == "Poison" ? 1f : power;
        return EffectFactory.CreateDotStatusEffect(selected.id, selected.description, duration,
            selected, Mathf.Max(0.01f, selected.tickInterval), value);
    }
}

[Serializable]
[ModuleCategory("Record/디버프/갈망하는 탄식")]
public sealed class RecordRandomDebuffModule : RecordEventModule
{
    public SO_BaseEffect[] debuffs;
    public float[] cooldownByLevel =
    {
        5f, 4f, 3f
    };
    public float duration = 5f;
    public float attackRatio = 0.1f;
    [NonSerialized] private float remaining;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        remaining = 0f;
    }
    public override void OnUpdate(float deltaTime)
    {
        remaining = Mathf.Max(0f, remaining - deltaTime);
    }
    protected override void AfterOwnHit(GameObject target, DamageEvent hit, float damage)
    {
        if (remaining > 0f || target.GetComponent<HealthPointComponent>()?.Dead != false)
        {
            return;
        }
        BaseEffect effect = RecordDebuffFactory.RandomEffect(debuffs, duration, status.GetStatusValue(StatusType.ATTACK) * attackRatio);
        if (effect == null)
        {
            return;
        }
        remaining = GetLevelValue(cooldownByLevel, 5f);
        target.GetComponent<EffectComponent>()?.ApplyEffect(effect, target, owner);
    }
}

[Serializable]
[ModuleCategory("Record/디버프/현현하는 반추")]
public sealed class RecordRancorModule : RecordEventModule
{
    public SO_BaseEffect debuff;
    public float delay = 3f;
    public float attackRatio = 1f;
    public float bonusPerDebuff = 0.1f;
    protected override void AfterOwnHit(GameObject target, DamageEvent hit, float damage)
    {
        EffectComponent targetEffects = target.GetComponent<EffectComponent>();
        string id = "RecordRancor:" + owner.GetInstanceID();
        if (target.GetComponent<HealthPointComponent>()?.Dead != false || targetEffects == null || targetEffects.HasEffect(id) != null)
        {
            return;
        }
        targetEffects.ApplyEffect(new RecordRancorEffect(id, debuff, delay,
            status.GetStatusValue(StatusType.ATTACK) * attackRatio, bonusPerDebuff), target, owner);
    }
}

public sealed class RecordRancorEffect : RecordDefinedEffect
{
    private readonly float power;
    private readonly float perDebuff;
    private bool exploded;
    public RecordRancorEffect(string id, SO_BaseEffect definition, float duration, float power, float perDebuff)
        : base(id, definition, duration, EffectType.DEBUFF)
    {
        this.power = power;
        this.perDebuff = perDebuff;
    }
    public override BuffStackPolicy StackPolicy => BuffStackPolicy.IGNOREIFEXSIST;
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (IsExpired == false || exploded || owner == null || appliedBy == null || owner.activeInHierarchy == false)
        {
            return;
        }
        exploded = true;
        int count = RecordRuntime.DebuffKinds(owner.GetComponent<EffectComponent>(), ID);
        DamageEvent damage = new DamageEvent(power * (1f + count * perDebuff))
        {
            SuppressRecordTriggers = true,
            IsStatusEffectDamage = true
        };
        owner.GetComponent<DamageHandleComponent>()?.OnDamage(appliedBy, damage);
    }
    public override BaseEffect CreateTransferCopy(float duration) => new RecordRancorEffect(ID, definition, duration, power, perDebuff);
}

[Serializable]
[ModuleCategory("Record/디버프/끝나지 않은 유언")]
public sealed class RecordDebuffTransferModule : RecordEventModule
{
    public float radius = 8f;
    public int[] targetCounts =
    {
        1, 2, 3
    };
    protected override void OnOwnedKill(GameObject target, DamageEvent hit)
    {
        EffectComponent source = target != null ? target.GetComponent<EffectComponent>() : null;
        if (source == null)
        {
            return;
        }
        List<BaseEffect> transferred = new();
        foreach (BaseEffect effect in source.ActiveEffects)
        {
            if (effect.Type == EffectType.DEBUFF && effect.AppliedBy == owner && effect.IsExpired == false)
            {
                transferred.Add(effect);
            }
        }
        int levelIndex = Mathf.Clamp(skillLevel - 1, 0, targetCounts.Length - 1);
        int count = targetCounts.Length > 0 ? targetCounts[levelIndex] : 1;
        List<Character> targets = RecordRuntime.Enemies(owner, target.transform.position, radius);
        int remaining = Mathf.Min(count, targets.Count);
        for (int i = 0; i < remaining; i++)
        {
            EffectComponent receiver = targets[i].GetComponent<EffectComponent>();
            if (receiver == null)
            {
                continue;
            }
            foreach (BaseEffect effect in transferred)
            {
                BaseEffect copy = effect.CreateTransferCopy(effect.Duration > 0f ? effect.RemainingTime : 0f);
                if (copy == null)
                {
                    continue;
                }
                for (int stack = 1; stack < effect.StackCount; stack++)
                {
                    copy.AddStack();
                }
                receiver.ApplyEffect(copy, targets[i].gameObject, owner);
            }
        }
    }
}

[Serializable]
[ModuleCategory("Record/디버프/흑주갑")]
public sealed class RecordBlackArmorModule : RecordEventModule
{
    public SO_BaseEffect buff;
    public SO_BaseEffect[] damageDebuffs;
    public float duration = 5f;
    public float attackRatio = 0.1f;
    public float reductionPerKind = 0.1f;
    public float maximumReduction = 0.3f;
    public float damagePerKind = 0.05f;
    public float maximumDamage = 0.2f;
    [NonSerialized] private int previousCount = -1;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        ApplyOwned(new RecordArmorEffect(buff));
        previousCount = -1;
        if (battle != null)
        {
            battle.OnEffectApplied += OnEffectApplied;
        }
        OnUpdate(0f);
    }
    private void OnEffectApplied(GameObject target, GameObject caster, BaseEffect effect)
    {
        if (caster != owner || target == owner || effect.ID != "Curse")
        {
            return;
        }
        BaseEffect selfDebuff = RecordDebuffFactory.RandomEffect(damageDebuffs, duration,
            status.GetStatusValue(StatusType.ATTACK) * attackRatio);
        effects?.ApplyEffect(selfDebuff, owner, owner);
        OnUpdate(0f);
    }
    public override void OnUpdate(float deltaTime)
    {
        int count = RecordRuntime.DebuffKinds(effects, RecordArmorEffect.EffectID);
        if (count == previousCount)
        {
            return;
        }
        previousCount = count;
        ApplyStat(buff, StatusType.DAMAGE_REDUCTION, Mathf.Min(maximumReduction, count * reductionPerKind));
        ApplyStat(buff, StatusType.DAMAGE_BONUS, Mathf.Min(maximumDamage, count * damagePerKind));
    }
    public override void OnLose()
    {
        if (battle != null)
        {
            battle.OnEffectApplied -= OnEffectApplied;
        }
        base.OnLose();
    }
}

public sealed class RecordArmorEffect : RecordDefinedEffect
{
    public const string EffectID = "BlackCurseArmor";
    public override bool IsDispellable => false;
    private StatusComponent status;
    public RecordArmorEffect(SO_BaseEffect definition) : base(EffectID, definition, 0f, EffectType.DEBUFF)
    {
    }
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        status = target.GetComponent<StatusComponent>();
        status?.SetDebuffImmunity(this, true);
    }
    public override void OnRemove()
    {
        status?.SetDebuffImmunity(this, false);
        base.OnRemove();
    }
}
