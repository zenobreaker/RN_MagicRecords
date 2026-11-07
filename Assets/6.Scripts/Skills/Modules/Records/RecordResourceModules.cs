using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[ModuleCategory("Record/마나/능력치")]
public sealed class RecordManaModule : RecordEventModule
{
    public SO_BaseEffect buff;
    public float maxManaBonus;
    public float hitRecoveryBonus;
    public float regenerationBonus;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        ApplyOwned(new RecordManaEffect($"Record:{recordId}:Mana", buff,
            new Vector3(maxManaBonus, hitRecoveryBonus, regenerationBonus)));
    }
}

[Serializable]
[ModuleCategory("Record/에너지/시작 에너지")]
public sealed class RecordInitialEnergyModule : RecordEventModule
{
    public int amount = 10;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        effects?.AddLaserEnergy(amount, null, false);
    }
}

[Serializable]
[ModuleCategory("Record/피해/태그 피해 증가")]
public sealed class RecordTaggedDamageModule : RecordEventModule
{
    public string tag;
    public float bonus = 0.1f;
    protected override void BeforeOwnHit(GameObject target, DamageEvent hit)
    {
        if (RecordRuntime.HasTag(hit.SkillTags, tag))
        {
            hit.BaseDamage *= 1f + bonus;
        }
    }
}

[Serializable]
[ModuleCategory("Record/스킬/태그 및 보조 스킬 시간")]
public sealed class RecordSkillTimingModule : RecordEventModule
{
    public string tag;
    public bool subActionOnly;
    public float cooldownReduction;
    public float castTimeReduction;
    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (evt?.Owner != owner || evt.Skill == null || evt.Skill.IsBasicAttack || context == null ||
            (subActionOnly && evt.Skill.IsSubAction == false) || RecordRuntime.HasTag(evt.Skill.Keywords, tag) == false)
        {
            return;
        }
        context.Base.Cooldown *= Mathf.Clamp01(1f - cooldownReduction);
        context.Cast.CastingTime *= Mathf.Clamp01(1f - castTimeReduction);
    }
}

[Serializable]
[ModuleCategory("Record/스킬/시전 시 남은 쿨다운 감소")]
public sealed class RecordCooldownOnCastModule : RecordEventModule
{
    [Range(0f, 1f)] public float reduction = 0.03f;
    public float cooldown = 3f;
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
    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        if (evt?.Owner != owner || evt.Skill == null || evt.Skill.IsBasicAttack || remaining > 0f)
        {
            return;
        }
        remaining = cooldown;
        RecordRuntime.ReduceCooldowns(owner, reduction, evt.Skill);
    }
}

[Serializable]
[ModuleCategory("Record/사출/관통 증가")]
public sealed class RecordPierceModule : RecordEventModule
{
    public string tag = "사출";
    public int count = 1;
    public override void OnSpawnObject(ISkillEffect spawnedObject, ActiveSkill casterSkill)
    {
        if (casterSkill?.Owner == null || casterSkill.Owner.gameObject != owner ||
            RecordRuntime.HasTag(casterSkill.Keywords, tag) == false)
        {
            return;
        }
        if (spawnedObject is IProjectile projectile)
        {
            projectile.PierceCount += count;
        }
    }
}

/// <summary>탄환 공급자의 교체와 늦은 등록까지 처리하는 소비 이벤트 모듈입니다.</summary>
[Serializable]
public abstract class RecordBulletModule : RecordEventModule
{
    [NonSerialized] protected Passive_MagicBulletLoad provider;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        BindProvider();
    }
    public override void OnUpdate(float deltaTime)
    {
        BindProvider();
    }
    private void BindProvider()
    {
        Passive_MagicBulletLoad next = owner != null
            ? owner.GetComponent<SkillComponent>()?.GetCapability<IMagicBulletProvider>() as Passive_MagicBulletLoad : null;
        if (ReferenceEquals(provider, next))
        {
            return;
        }
        if (provider != null)
        {
            provider.OnBulletConsumed -= OnBulletConsumed;
        }
        provider = next;
        if (provider != null)
        {
            provider.OnBulletConsumed += OnBulletConsumed;
        }
    }
    protected abstract void OnBulletConsumed();
    public override void OnLose()
    {
        if (provider != null)
        {
            provider.OnBulletConsumed -= OnBulletConsumed;
        }
        provider = null;
        base.OnLose();
    }
}

[Serializable]
[ModuleCategory("Record/탄환/절반 이하 즉시 장전")]
public sealed class RecordEmergencyReloadModule : RecordBulletModule
{
    public float threshold = 0.5f;
    public float reloadRatio = 0.3f;
    public float cooldown = 15f;
    [NonSerialized] private float remaining;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        remaining = 0f;
    }
    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        remaining = Mathf.Max(0f, remaining - deltaTime);
    }
    protected override void OnBulletConsumed()
    {
        if (provider == null || remaining > 0f || provider.CurrentBulletCount > provider.MaxBulletCount * threshold)
        {
            return;
        }
        remaining = cooldown;
        provider.Reload(Mathf.Max(1, Mathf.FloorToInt(provider.MaxBulletCount * reloadRatio)));
    }
}

[Serializable]
[ModuleCategory("Record/탄환/달궈진 총구")]
public sealed class RecordHeatModule : RecordBulletModule
{
    public SO_BaseEffect buff;
    public float[] damagePerStack =
    {
        0.03f, 0.04f, 0.05f
    };
    public float decayInterval = 4f;
    public int maxStacks = 5;
    protected override void OnBulletConsumed()
    {
        string id = $"Record:{recordId}:Heat";
        if (effects?.HasEffect(id) is RecordHeatEffect heat)
        {
            heat.AddStack();
            return;
        }
        ApplyOwned(new RecordHeatEffect(id, buff, GetLevelValue(damagePerStack, 0.03f), decayInterval, maxStacks));
    }
    public override void OnChangedLevel(int level)
    {
        base.OnChangedLevel(level);
        if (effects?.HasEffect($"Record:{recordId}:Heat") is RecordHeatEffect heat)
        {
            heat.SetDamagePerStack(GetLevelValue(damagePerStack, 0.03f));
        }
    }
}

public sealed class RecordHeatEffect : RecordDefinedEffect
{
    private float perStack;
    private readonly float interval;
    private readonly int limit;
    private float elapsed;
    private StatusComponent status;
    private StatModifier modifier;
    public RecordHeatEffect(string id, SO_BaseEffect definition, float perStack, float interval, int limit)
        : base(id, definition, 0f)
    {
        this.perStack = perStack;
        this.interval = Mathf.Max(0.01f, interval);
        this.limit = Mathf.Max(1, limit);
    }
    public override int MaxStack => limit;
    public override BuffStackPolicy StackPolicy => BuffStackPolicy.STACKABLE;
    public override bool ShowSingleStack => true;
    public override void OnApply(GameObject target, GameObject caster)
    {
        base.OnApply(target, caster);
        status = target.GetComponent<StatusComponent>();
        RefreshModifier();
    }
    public void SetDamagePerStack(float value)
    {
        perStack = value;
        RefreshModifier();
    }
    private void RefreshModifier()
    {
        status?.RemoveBuff(modifier);
        modifier = new StatModifier(StatusType.BASIC_DAMAGE_BONUS, perStack * StackCount, ModifierValueType.FIXED);
        status?.ApplyBuff(modifier);
    }
    public override void AddStack()
    {
        base.AddStack();
        elapsed = 0f;
        RefreshModifier();
        owner?.GetComponent<EffectComponent>()?.RefreshEffectUI(this);
    }
    public override void Update(float deltaTime)
    {
        elapsed += deltaTime;
        while (elapsed >= interval && StackCount > 0)
        {
            elapsed -= interval;
            TryConsumeStacks(1);
            RefreshModifier();
            owner?.GetComponent<EffectComponent>()?.RefreshEffectUI(this);
        }
        if (StackCount == 0)
        {
            owner.GetComponent<EffectComponent>()?.RemoveEffect(this);
        }
    }
    public override void OnRemove()
    {
        status?.RemoveBuff(modifier);
        base.OnRemove();
    }
}

[Serializable]
[ModuleCategory("Record/기억/파멸의 종말")]
public sealed class RecordDoomModule : RecordEventModule
{
    public SO_BaseEffect buff;
    public float delay = 5f;
    public List<RecordBuffEntry> delayedBuffs = new();
    [NonSerialized] private float elapsed;
    [NonSerialized] private bool pending;
    public override void OnBattleStarted(IReadOnlyList<Character> enemies)
    {
        ClearOwned();
        elapsed = 0f;
        pending = true;
        ApplyOwned(new CooldownReductionEffect(buff, -0.1f, delay, $"Record:{recordId}:DoomDelay", "종말의 전조"));
    }
    public override void OnUpdate(float deltaTime)
    {
        if (pending == false || owner == null || owner.activeInHierarchy == false)
        {
            return;
        }
        elapsed += deltaTime;
        if (elapsed < delay)
        {
            return;
        }
        pending = false;
        ClearOwned();
        foreach (RecordBuffEntry entry in delayedBuffs)
        {
            if (entry.kind == RecordBuffEntry.BuffKind.CooldownReduction)
            {
                ApplyOwned(new CooldownReductionEffect(entry.buff, entry.amount, 0f, $"Record:{recordId}:DoomCooldown", entry.displayName));
            }
            else
            {
                ApplyStat(entry.buff, entry.stat, entry.amount, entry.valueType);
            }
        }
    }
    protected override void OnBattleEnded()
    {
        pending = false;
        ClearOwned();
    }
}

[Serializable]
[ModuleCategory("Record/기억/소로")]
public sealed class RecordSoroModule : RecordEventModule
{
    public SO_BaseEffect buff;
    public float delay = 4f;
    public float duration = 30f;
    public float damagePerEnergy = 0.01f;
    public int maxEnergy = 10;
    [NonSerialized] private float elapsed;
    [NonSerialized] private int gained;
    [NonSerialized] private bool active;
    [NonSerialized] private bool started;
    public override void OnAcquire(GameObject skillOwner, int level)
    {
        base.OnAcquire(skillOwner, level);
        elapsed = 0f;
        gained = 0;
        active = false;
        started = false;
        if (effects != null)
        {
            effects.OnLaserEnergyGained += OnEnergyGained;
        }
    }
    public override void OnBattleStarted(IReadOnlyList<Character> enemies)
    {
        ClearOwned();
        elapsed = 0f;
        gained = 0;
        active = false;
        started = true;
    }
    public override void OnUpdate(float deltaTime)
    {
        if (started == false)
        {
            return;
        }
        elapsed += deltaTime;
        if (active == false && elapsed >= delay && elapsed < delay + duration)
        {
            active = true;
            ApplyStat(buff, StatusType.ENERGY_GAIN_BONUS, 1f);
        }
        if (active && elapsed >= delay + duration)
        {
            active = false;
            ClearOwned();
        }
    }
    protected override void OnBattleEnded()
    {
        started = false;
        active = false;
        ClearOwned();
    }
    private void OnEnergyGained(int amount)
    {
        if (active == false)
        {
            return;
        }
        gained = Mathf.Min(maxEnergy, gained + amount);
        ApplyStat(buff, StatusType.ATTACK, damagePerEnergy * gained, ModifierValueType.PERCENT);
    }
    public override void OnLose()
    {
        if (effects != null)
        {
            effects.OnLaserEnergyGained -= OnEnergyGained;
        }
        started = false;
        active = false;
        base.OnLose();
    }
}

[Serializable]
[ModuleCategory("Record/기억/폭주하는 야성")]
public sealed class RecordWildnessModule : RecordEventModule
{
    public SO_BaseEffect shieldBuff;
    public float healChance = 0.03f;
    public float healRatio = 0.03f;
    public float shieldChance = 0.05f;
    public float shieldRatio = 0.01f;
    public float shieldDuration = 5f;
    protected override void AfterOwnHit(GameObject target, DamageEvent hit, float damage)
    {
        if (hit.IsSubAction == false && UnityEngine.Random.value < healChance)
        {
            HealthPointComponent health = owner.GetComponent<HealthPointComponent>();
            health?.Heal(health.GetMaxHP * healRatio);
        }
    }
    public override void OnSkillCast(SkillUseEvent evt, SkillRuntimeContext context)
    {
        string id = $"Record:{recordId}:WildShield";
        if (evt?.Owner != owner || evt.Skill?.IsSubAction != true || effects == null ||
            effects.HasEffect(id) != null || UnityEngine.Random.value >= shieldChance)
        {
            return;
        }
        ApplyOwned(new RecordShieldEffect(id, shieldBuff, status.GetMaxHP() * shieldRatio, shieldDuration));
    }
}
