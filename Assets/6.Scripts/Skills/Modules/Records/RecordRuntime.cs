using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>레코드 모듈이 기존 전투·버프·스킬 API를 공유하도록 돕습니다.</summary>
public static class RecordRuntime
{
    public static bool HasTag(IReadOnlyList<string> tags, string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return true;
        }
        if (tags == null)
        {
            return false;
        }
        foreach (string candidate in tags)
        {
            if (string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public static void ReduceCooldowns(GameObject owner, float ratio, ActiveSkill excluded = null)
    {
        SkillComponent skills = owner != null ? owner.GetComponent<SkillComponent>() : null;
        if (skills == null)
        {
            return;
        }
        SkillSlot[] slots =
        {
            SkillSlot.SLOT1, SkillSlot.SLOT2, SkillSlot.SLOT3, SkillSlot.SLOT4, SkillSlot.SubAction
        };
        foreach (SkillSlot slot in slots)
        {
            if (skills.TryGetRegisteredActiveSkill(slot, out ActiveSkill skill) && skill != null && skill != excluded)
            {
                skill.ReduceRemainingCooldown(ratio);
            }
        }
    }

    public static List<Character> Enemies(GameObject owner, Vector3 center, float radius)
    {
        List<Character> result = new();
        Character caster = owner != null ? owner.GetComponent<Character>() : null;
        if (caster == null || BattleManager.Instance == null)
        {
            return result;
        }
        foreach (Character target in BattleManager.Instance.CurrentEnemies)
        {
            if (target == null || target.gameObject.activeInHierarchy == false ||
                Vector3.Distance(target.transform.position, center) > radius ||
                target.GetComponent<HealthPointComponent>()?.Dead != false ||
                CombatHelper.IsFriendly(caster, target.gameObject, null))
            {
                continue;
            }
            result.Add(target);
        }
        result.Sort((a, b) => (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));
        return result;
    }

    public static int DebuffKinds(EffectComponent effects, string excluded = null)
    {
        HashSet<string> kinds = new();
        if (effects != null)
        {
            foreach (BaseEffect effect in effects.ActiveEffects)
            {
                if (effect.Type == EffectType.DEBUFF && effect.ID != excluded)
                {
                    kinds.Add(effect.ID);
                }
            }
        }
        return kinds.Count;
    }
}

/// <summary>공통 전투 이벤트를 구독하고 해제 시 자신이 적용한 버프만 회수합니다.</summary>
[Serializable]
public abstract class RecordEventModule : CommonRecordModule
{
    [NonSerialized] protected StatusComponent status;
    [NonSerialized] protected EffectComponent effects;
    [NonSerialized] protected BattleManager battle;
    [NonSerialized] private readonly List<BaseEffect> ownedEffects = new();

    protected RecordEventModule()
    {
        triggerTime = PassiveTriggerTime.OnSkillCast;
    }

    public override void OnAcquire(GameObject skillOwner, int level)
    {
        OnLose();
        base.OnAcquire(skillOwner, level);
        status = owner != null ? owner.GetComponent<StatusComponent>() : null;
        effects = owner != null ? owner.GetComponent<EffectComponent>() : null;
        battle = BattleManager.Instance;
        if (battle != null)
        {
            battle.OnAnyAttackHit += HandleBeforeHit;
            battle.OnAttackResolved += HandleResolved;
            battle.OnBattleEnded += OnBattleEnded;
        }
    }

    private void HandleBeforeHit(GameObject attacker, GameObject target, DamageEvent hit)
    {
        if (owner == null || hit == null || owner.activeInHierarchy == false)
        {
            return;
        }
        BeforeAnyHit(attacker, target, hit);
        if (attacker == owner && hit.IsDOTEffect() == false && hit.SuppressRecordTriggers == false &&
            CombatHelper.IsFriendly(owner.GetComponent<Character>(), target, null) == false)
        {
            BeforeOwnHit(target, hit);
        }
    }

    private void HandleResolved(GameObject attacker, GameObject target, DamageEvent hit, float damage)
    {
        if (owner == null || attacker != owner || hit == null || damage <= 0f ||
            CombatHelper.IsFriendly(owner.GetComponent<Character>(), target, null))
        {
            return;
        }
        if (hit.KilledTarget)
        {
            OnOwnedKill(target, hit);
        }
        if (hit.IsDOTEffect() == false && hit.SuppressRecordTriggers == false)
        {
            AfterOwnHit(target, hit, damage);
        }
    }

    protected virtual void BeforeAnyHit(GameObject attacker, GameObject target, DamageEvent hit)
    {
    }
    protected virtual void BeforeOwnHit(GameObject target, DamageEvent hit)
    {
    }
    protected virtual void AfterOwnHit(GameObject target, DamageEvent hit, float damage)
    {
    }
    protected virtual void OnOwnedKill(GameObject target, DamageEvent hit)
    {
    }
    protected virtual void OnBattleEnded()
    {
    }

    protected void ApplyOwned(BaseEffect effect)
    {
        if (effect == null || effects == null)
        {
            return;
        }
        effects.RemoveEffect(effect.ID);
        effects.ApplyEffect(effect, owner, owner);
        ownedEffects.RemoveAll(previous => effects.HasEffect(previous.ID) != previous);
        ownedEffects.Add(effect);
    }

    protected void ClearOwned()
    {
        if (effects != null)
        {
            foreach (BaseEffect effect in ownedEffects)
            {
                effects.RemoveEffect(effect, true);
            }
        }
        ownedEffects.Clear();
    }

    protected void ApplyStat(SO_BaseEffect definition, StatusType stat, float amount,
        ModifierValueType valueType = ModifierValueType.FIXED, float duration = 0f, string suffix = "")
    {
        ApplyOwned(new StatBuffEffect($"Record:{recordId}:{GetType().Name}:{stat}:{suffix}", duration,
            new StatModifier(stat, amount, valueType), definition, definition != null ? definition.name : GetType().Name,
            BuffStackPolicy.REFRESH_ONLY));
    }

    public override void OnLose()
    {
        if (battle != null)
        {
            battle.OnAnyAttackHit -= HandleBeforeHit;
            battle.OnAttackResolved -= HandleResolved;
            battle.OnBattleEnded -= OnBattleEnded;
        }
        ClearOwned();
        status = null;
        effects = null;
        battle = null;
        base.OnLose();
    }
}

public abstract class RecordDefinedEffect : BaseEffect
{
    protected readonly SO_BaseEffect definition;
    protected RecordDefinedEffect(string id, SO_BaseEffect definition, float duration, EffectType type = EffectType.BUFF)
        : base(id, definition != null ? definition.description : id, duration)
    {
        this.definition = definition;
        Type = type;
        Name = definition != null ? definition.name : id;
        FxIcon = definition != null ? definition.icon : null;
        FxObject = definition != null ? definition.vfxObject : null;
    }
}

/// <summary>최대 마나·타격 회복·초당 회복을 출처별로 적용하는 버프입니다.</summary>
public sealed class RecordManaEffect : RecordDefinedEffect
{
    private readonly Vector3 modifier;
    private ManaComponent mana;
    public RecordManaEffect(string id, SO_BaseEffect definition, Vector3 modifier) : base(id, definition, 0f)
    {
        this.modifier = modifier;
    }
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        mana = target.GetComponent<ManaComponent>();
        mana?.SetRecordModifier(this, modifier);
    }
    public override void OnRemove()
    {
        mana?.SetRecordModifier(this, null);
        mana = null;
        base.OnRemove();
    }
}

/// <summary>받은 피해를 흡수하며 만료나 소진 시 제거되는 방어막입니다.</summary>
public sealed class RecordShieldEffect : RecordDefinedEffect
{
    private float remainingShield;
    public RecordShieldEffect(string id, SO_BaseEffect definition, float shield, float duration) : base(id, definition, duration)
    {
        remainingShield = Mathf.Max(0f, shield);
    }
    public float RemainingShield => remainingShield;
    public float Absorb(float damage)
    {
        float absorbed = Mathf.Min(Mathf.Max(0f, damage), remainingShield);
        remainingShield -= absorbed;
        if (remainingShield <= 0f)
        {
            owner.GetComponent<EffectComponent>()?.RemoveEffect(this);
        }
        return Mathf.Max(0f, damage - absorbed);
    }
}

/// <summary>표적의 받는 피해를 증가시키며 남은 시간 그대로 전이할 수 있습니다.</summary>
public sealed class RecordMarkEffect : RecordDefinedEffect
{
    private readonly float bonus;
    public float DamageBonus => bonus;
    private StatModifier modifier;
    private StatusComponent status;
    public RecordMarkEffect(string id, SO_BaseEffect definition, float duration, float bonus)
        : base(id, definition, duration, EffectType.DEBUFF)
    {
        this.bonus = bonus;
    }
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        status = target.GetComponent<StatusComponent>();
        modifier = new StatModifier(StatusType.INCOMING_DAMAGE_BONUS, bonus, ModifierValueType.FIXED);
        status?.ApplyBuff(modifier);
    }
    public override void OnRemove()
    {
        status?.RemoveBuff(modifier);
        base.OnRemove();
    }
    public override BaseEffect CreateTransferCopy(float duration) => new RecordMarkEffect(ID, definition, duration, bonus);
}

/// <summary>랜덤 제어형 상태이상의 적용과 해제를 기존 상태 플래그로 처리합니다.</summary>
public sealed class RecordControlEffect : CrowdControlEffect
{
    private readonly StatusEffectType flag;
    public RecordControlEffect(string id, SO_BaseEffect definition, float duration, StatusEffectType flag)
        : base(id, definition != null ? definition.description : id, duration)
    {
        this.flag = flag;
        Type = EffectType.DEBUFF;
        Name = id;
        FxIcon = definition != null ? definition.icon : null;
    }
    public override StatusEffectType EffectFlag => flag;
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        target.GetComponent<StatusEffectComponent>()?.AddStatusEffect(flag);
    }
    public override void OnRemove()
    {
        owner.GetComponent<StatusEffectComponent>()?.RemoveStatusEffect(flag);
        base.OnRemove();
    }
    public override BaseEffect CreateTransferCopy(float duration) => new RecordControlEffect(ID, null, duration, flag)
    {
        FxIcon = FxIcon,
        FxObject = FxObject
    };
}
