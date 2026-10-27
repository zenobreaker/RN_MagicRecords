using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class CommonRecordModule : PassiveContextModule
{
    [NonSerialized] protected int recordId;
    public void SetRecordId(int id)
    {
        recordId = id;
    }
    public virtual void OnBattleStarted(IReadOnlyList<Character> enemies)
    {
    }
    public virtual void OnUpdate(float deltaTime)
    {
    }
}

[Serializable]
public sealed class RecordBuffEntry
{
    public enum BuffKind
    {
        Stat, CooldownReduction
    }
    public SO_BaseEffect buff;
    public BuffKind kind;
    public StatusType stat;
    public ModifierValueType valueType = ModifierValueType.PERCENT;
    public string displayName;
    public float amount;
    [Min(0)] public float duration;
}

[Serializable]
[ModuleCategory("Record/공용/버프 적용")]
public sealed class CommonRecordBuffModule : CommonRecordModule
{
    public enum Activation
    {
        Persistent, BattleStart, Attack, LowHealth
    }
    public Activation activation;
    public List<RecordBuffEntry> buffs = new();
    [Range(0, 1)] public float probability = 1f;
    [Range(0, 1)] public float healthThreshold = .3f;
    [NonSerialized] private readonly List<BaseEffect> applied = new();
    [NonSerialized] private EffectComponent effects;
    [NonSerialized] private Character attackOwner;

    public override void OnAcquire(GameObject skillOwner, int level)
    {
        OnLose();
        base.OnAcquire(skillOwner, level);
        effects = owner != null ? owner.GetComponent<EffectComponent>() : null;
        if (activation == Activation.Attack && owner != null)
        {
            attackOwner = owner.GetComponent<Character>();
            if (attackOwner != null)
            {
                attackOwner.OnAttackExecuted += OnAttack;
            }
        }
        if (activation == Activation.Persistent)
        {
            Apply();
        }

        if (activation == Activation.LowHealth)
        {
            OnUpdate(0);
        }
    }
    public override void OnApplyStaticEffect(StatusComponent status)
    {
        if (status != null && status.gameObject == owner && activation == Activation.Persistent)
        {
            Apply();
        }
    }
    public override void OnBattleStarted(IReadOnlyList<Character> enemies)
    {
        if (activation == Activation.BattleStart)
        {
            Clear();
            Apply();
        }
    }
    private void OnAttack(ActionData action, Character attacker)
    {
        if (attacker != null && attacker.gameObject == owner)
        {
            TryApplyAttack(UnityEngine.Random.value);
        }
    }
    public bool TryApplyAttack(float sample)
    {
        if (activation != Activation.Attack || Roll(sample) == false)
        {
            return false;
        }

        Apply();
        return true;
    }
    public bool Roll(float sample)
    {
        return probability > 0 && sample < probability;
    }
    public override void OnUpdate(float deltaTime)
    {
        if (activation != Activation.LowHealth || owner == null)
        {
            return;
        }

        var status = owner.GetComponent<StatusComponent>();
        bool active = status != null && status.GetMaxHP() > 0 && status.GetCurrentHP() > 0 &&
            status.GetCurrentHP() / status.GetMaxHP() <= healthThreshold;
        if (active)
        {
            Apply();
        }
        else
        {
            Clear();
        }
    }
    private void Apply()
    {
        if (owner == null || owner.activeInHierarchy == false || effects == null)
        {
            return;
        }

        for (int i = 0; i < buffs.Count; i++)
        {
            var entry = buffs[i];
            if (entry?.buff == null)
            {
                continue;
            }

            string id = $"Record:{recordId}:{entry.buff.id}:{i}";
            if (effects.HasEffect(id) != null)
            {
                continue;
            }

            BaseEffect effect = entry.kind == RecordBuffEntry.BuffKind.CooldownReduction
                            ? new CooldownReductionEffect(entry.buff, entry.amount, entry.duration, id, entry.displayName)
                            : new StatBuffEffect(id, entry.duration, new StatModifier(entry.stat, entry.amount, entry.valueType),
                                entry.buff, entry.displayName, BuffStackPolicy.IGNOREIFEXSIST);
            effects.ApplyEffect(effect, owner, owner);
            applied.Add(effect);
        }
        applied.RemoveAll(effect => effects.HasEffect(effect.ID) != effect);
    }
    private void Clear()
    {
        if (effects != null)
        {
            foreach (var effect in applied)
            {
                effects.RemoveEffect(effect);
            }
        }

        applied.Clear();
    }
    public override void OnLose()
    {
        if (attackOwner != null)
        {
            attackOwner.OnAttackExecuted -= OnAttack;
        }

        attackOwner = null;
        Clear();
        effects = null;
        base.OnLose();
    }
}

[Serializable]
[ModuleCategory("Record/공용/전투 시작 필드 출혈")]
public sealed class CommonRecordBattleBleedModule : CommonRecordModule
{
    public SO_BaseEffect bleed;
    [Min(0)] public float duration = 10;
    [Min(0)] public float attackRatio = .1f;
    public override void OnBattleStarted(IReadOnlyList<Character> enemies)
    {
        if (owner == null || bleed == null || enemies == null)
        {
            return;
        }

        float power = (owner.GetComponent<StatusComponent>()?.GetStatusValue(StatusType.ATTACK) ?? 0) * attackRatio;
        foreach (var target in enemies)
        {
            if (target == null || target.gameObject.activeInHierarchy == false)
            {
                continue;
            }

            var effects = target.GetComponent<EffectComponent>();
            if (effects == null)
            {
                continue;
            }

            var effect = EffectFactory.CreateDotStatusEffect("Bleed", bleed.description, duration,
                            bleed, Mathf.Max(.01f, bleed.tickInterval), power);
            effects.ApplyEffect(effect, target.gameObject, owner);
        }
    }
}

// Cooldown is tracked by source in StatusComponent, independently of stat modifiers.
public sealed class CooldownReductionEffect : BaseEffect
{
    private readonly float amount;
    private StatusComponent status;

    public CooldownReductionEffect(SO_BaseEffect definition, float amount, float duration, string sourceId, string displayName)
        : base(sourceId, definition.description, duration)
    {
        this.amount = amount;
        Type = EffectType.BUFF;
        Name = displayName;
        FxIcon = definition.icon;
        FxObject = definition.vfxObject;
    }
    public override BuffStackPolicy StackPolicy
    {
        get
        {
            return BuffStackPolicy.IGNOREIFEXSIST;
        }
    }
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        status = target != null ? target.GetComponent<StatusComponent>() : null;
        status.SafeInvoke(value => value.SetCooldownReduction(this, amount));
    }
    public override void OnRemove()
    {
        status.SafeInvoke(value => value.RemoveCooldownReduction(this));
        status = null;
        base.OnRemove();
    }
}
