using System;
using UnityEngine;

/// <summary>초당 회복과 공격 적중 회복을 분리하여 캐릭터의 마나를 관리합니다.</summary>
[DisallowMultipleComponent]
public sealed class ManaComponent : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxMana = 100f;
    [SerializeField, Min(0f)] private float regenerationRatioPerSecond;
    [SerializeField, Min(0f)] private float regenerationPerSecond;
    [SerializeField, Min(0f)] private float regenerationPerLevel;
    [SerializeField, Range(0f, 1f)] private float hitRecoveryRatio = 0.02f;
    [SerializeField, Range(0f, 1f)] private float skillHitEfficiency = 0.1f;

    private Character owner;
    private HealthPointComponent health;
    private StatusComponent status;
    private SO_HUDHandler hudHandler;
    private float currentMana;
    private int currentLevel = 1;
    private readonly System.Collections.Generic.Dictionary<object, Vector3> recordModifiers = new();

    private Vector3 RecordModifiers
    {
        get
        {
            Vector3 total = Vector3.zero;
            foreach (Vector3 modifier in recordModifiers.Values)
            {
                total += modifier;
            }
            return total;
        }
    }

    /// <summary>레코드 출처별 마나 능력치를 적용하고 제거합니다.</summary>
    public void SetRecordModifier(object source, Vector3? modifier)
    {
        if (modifier.HasValue)
        {
            recordModifiers[source] = modifier.Value;
        }
        else
        {
            recordModifiers.Remove(source);
        }
        currentMana = Mathf.Min(currentMana, MaxMana);
        NotifyManaChanged();
    }

    public float CurrentMana => currentMana;
    public float MaxMana => Mathf.Max(1f, maxMana + RecordModifiers.x);
    public float RegenerationRatioPerSecond => Mathf.Max(0f, regenerationRatioPerSecond + RecordModifiers.z);
    public float RegenerationPerSecond
    {
        get
        {
            float recovery = Mathf.Max(0f, regenerationPerSecond + (currentLevel - 1) * regenerationPerLevel) +
                MaxMana * RegenerationRatioPerSecond;
            float bonus = status != null ? status.GetStatusValue(StatusType.MANA_REGEN_BONUS) : 0f;
            return recovery * Mathf.Max(0f, 1f + bonus);
        }
    }
    public float HitRecoveryRatio => Mathf.Clamp01(hitRecoveryRatio + RecordModifiers.y);
    public float SkillHitEfficiency => skillHitEfficiency;
    public event Action<float, float> OnChanged;

    private void Awake()
    {
        owner = GetComponent<Character>();
        health = GetComponent<HealthPointComponent>();
        status = GetComponent<StatusComponent>();
        if (owner is Player)
        {
            hudHandler = Resources.Load<SO_HUDHandler>("SO_HUDHandler");
        }
    }

    private void OnEnable()
    {
        ResetMana();
    }

    private void OnDisable()
    {
        hudHandler?.OnChangeValue_MP(0f, 0f);
    }

    /// <summary>마나 변경을 기존 구독자와 플레이어 HUD에 전달합니다.</summary>
    private void NotifyManaChanged()
    {
        OnChanged?.Invoke(currentMana, MaxMana);
        hudHandler?.OnChangeValue_MP(currentMana, MaxMana);
    }

    private void Update()
    {
        Regenerate(Time.deltaTime);
    }

    /// <summary>현재 레벨에 따른 고정 회복량을 적용하며 최대 마나와 현재 마나는 변경하지 않습니다.</summary>
    public void SetLevel(int level)
    {
        currentLevel = Mathf.Max(1, level);
    }

    /// <summary>새 캐릭터 스폰 시 마나를 최대치로 초기화합니다.</summary>
    public void ResetMana()
    {
        currentMana = MaxMana;
        NotifyManaChanged();
    }

    /// <summary>최대 마나 변경 시 현재 마나는 보존하고 초과분만 제한합니다.</summary>
    public void SetMaxMana(float value)
    {
        if (IsFinite(value) == false || value < 1f)
        {
            return;
        }

        maxMana = value;
        currentMana = Mathf.Clamp(currentMana, 0f, MaxMana);
        NotifyManaChanged();
    }

    /// <summary>초당 회복률, 기본 공격 회복률, 스킬 적중 효율을 개별 설정합니다.</summary>
    public void ConfigureRecovery(float perSecondRatio, float perHitRatio, float skillEfficiency)
    {
        if (IsFinite(perSecondRatio) == false || IsFinite(perHitRatio) == false ||
            IsFinite(skillEfficiency) == false)
        {
            return;
        }

        regenerationRatioPerSecond = Mathf.Max(0f, perSecondRatio);
        hitRecoveryRatio = Mathf.Clamp01(perHitRatio);
        skillHitEfficiency = Mathf.Clamp01(skillEfficiency);
    }

    /// <summary>고정 회복량과 비율 회복량에 경과 시간을 적용합니다. 타격 회복과 독립적으로 동작합니다.</summary>
    public void Regenerate(float deltaTime)
    {
        if (IsFinite(deltaTime) == false || deltaTime <= 0f)
        {
            return;
        }

        RestoreMana(RegenerationPerSecond * deltaTime);
    }

    /// <summary>실제로 적에게 가한 직접 피해만 타격 회복에 반영합니다.</summary>
    public void RecoverOnHit(DamageEvent damageEvent, float dealtDamage, GameObject target)
    {
        if (damageEvent == null || (damageEvent.IsDOTEffect() || damageEvent.IsStatusEffectDamage || damageEvent.SuppressRecordTriggers) || IsFinite(dealtDamage) == false || dealtDamage <= 0f)
        {
            return;
        }
        if (owner == null || CombatHelper.IsFriendly(owner, target, null))
        {
            return;
        }

        float efficiency = damageEvent.IsBasicAttack ? 1f : skillHitEfficiency;
        RestoreMana(MaxMana * HitRecoveryRatio * efficiency);
    }

    /// <summary>유효한 회복량만 최대 마나 범위 안에서 적용합니다.</summary>
    public void RestoreMana(float amount)
    {
        if (CanRecover() == false || IsFinite(amount) == false || amount <= 0f)
        {
            return;
        }

        float nextMana = Mathf.Min(MaxMana, currentMana + amount);
        if (nextMana == currentMana)
        {
            return;
        }

        currentMana = nextMana;
        NotifyManaChanged();
    }

    /// <summary>마나가 충분할 때만 지정된 양을 소비합니다.</summary>
    public bool TrySpendMana(float amount)
    {
        if (CanRecover() == false || IsFinite(amount) == false || amount < 0f || amount > currentMana)
        {
            return false;
        }

        currentMana -= amount;
        NotifyManaChanged();
        return true;
    }

    private bool CanRecover() => isActiveAndEnabled && (health == null || health.Dead == false);

    private static bool IsFinite(float value) => float.IsNaN(value) == false && float.IsInfinity(value) == false;
}
