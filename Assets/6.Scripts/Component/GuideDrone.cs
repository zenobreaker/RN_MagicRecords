using System.Collections.Generic;
using UnityEngine;

/// <summary>소유 캐릭터에서 드론 효과만 실행합니다. 장착 UI, 입력, 외형 생성은 담당하지 않습니다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(StatusComponent), typeof(EffectComponent))]
public sealed class GuideDrone : MonoBehaviour
{
    [SerializeField] private SO_GuideDroneData data;

    private StatusComponent status;
    private EffectComponent effects;
    private HealthPointComponent health;
    private BattleManager battle;
    private readonly List<StatModifier> passiveModifiers = new();
    private GuideDroneActiveEffect activeEffect;
    private SO_HUDHandler hud;
    private float points;
    private bool initialized;
    private bool active;

    public SO_GuideDroneData Data => data;
    public float Points => points;
    public bool IsActive => active;
    public bool IsInitialized => initialized && data != null;
    public float ChargeRatio => IsInitialized ? Mathf.Clamp01(points / data.maximumPoints) : 0f;
    public bool IsFullyCharged => IsInitialized && points >= data.maximumPoints;
    public bool CanActivate => IsFullyCharged && active == false && CanUseActive() && Time.timeScale > 0f;

    private void Awake()
    {
        status = GetComponent<StatusComponent>();
        effects = GetComponent<EffectComponent>();
        health = GetComponent<HealthPointComponent>();
        hud = Resources.Load<SO_HUDHandler>("SO_HUDHandler");
    }

    private void OnEnable()
    {
        Initialize(data);
    }

    /// <summary>전달받은 드론 설정으로 초기화합니다. 설정이 없으면 효과를 적용하지 않습니다.</summary>
    public void Initialize(SO_GuideDroneData definition)
    {
        Release();
        data = definition;
        if (isActiveAndEnabled == false || data == null || data.HasValidPointSettings() == false)
        {
            return;
        }

        initialized = true;
        points = Mathf.Min(data.initialPoints, data.maximumPoints);
        foreach (var entry in data.passiveStats)
        {
            if (entry == null)
            {
                continue;
            }

            var modifier = new StatModifier(entry.Status, entry.Value, entry.ValueType);
            status.ApplyBuff(modifier);
            passiveModifiers.Add(modifier);
        }
        battle = BattleManager.Instance;
        if (battle != null)
        {
            battle.OnBattleEnded += Deactivate;
        }
        PublishState();
    }

    /// <summary>전투 보상 등에서 지급할 포인트를 상한까지 더합니다. 지급 조건은 호출 측에서 결정합니다.</summary>
    public void AddPoints(float amount)
    {
        if (initialized == false || active || data == null || amount <= 0f ||
            float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }
        points = Mathf.Min(data.maximumPoints, points + amount);
        if (data.maximumPoints - points <= data.maximumPoints * 0.00001f)
        {
            points = data.maximumPoints;
        }
        PublishState();
    }

    /// <summary>자연 발동한 기억만 등급에 따른 최대 충전량 비율로 충전합니다.</summary>
    public void OnMemoryEffectActivated(RecordRarity rarity)
    {
        if (CanUseActive() == false || active || Time.timeScale <= 0f || IsInitialized == false)
        {
            return;
        }
        AddPoints(data.GetMemoryCharge(rarity));
    }

    /// <summary>소유자의 액티브 스킬 사용이 성립하면 한 번 충전합니다. 일반 공격과 보조 행동은 제외합니다.</summary>
    public void OnSkillUsed(ActiveSkill skill)
    {
        if (skill == null || skill.Owner == null || skill.Owner.gameObject != gameObject ||
            skill.IsBasicAttack || skill.IsSubAction || CanUseActive() == false ||
            active || Time.timeScale <= 0f || IsInitialized == false)
        {
            return;
        }
        AddPoints(data.GetSkillUseCharge());
    }

    /// <summary>고유 액티브와 보유 기억의 지속 효과를 한 번 발동하고 포인트 소모를 시작합니다.</summary>
    public bool TryActivate()
    {
        if (initialized == false || active || data == null || data.HasValidPointSettings() == false ||
            CanActivate == false)
        {
            return false;
        }

        active = true;
        activeEffect = new GuideDroneActiveEffect($"GuideDrone:{GetInstanceID()}", data,
            points / data.pointsPerSecond);
        activeEffect.OnRemovedUI += OnActiveEffectRemoved;
        effects.ApplyEffect(activeEffect, gameObject, gameObject);
        PublishState();

        AppManager.Instance.SafeInvoke(app => app.GetRecordManager())
            .SafeInvoke(records => records.ReactivateMemoryRecords(gameObject));
        return true;
    }

    private bool CanUseActive()
    {
        var player = GetComponent<Player>();
        return isActiveAndEnabled && battle != null && battle.IsInBattle &&
            health != null && health.Dead == false &&
            (player == null || player.CombatInputEnabled);
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    /// <summary>전투 시간만큼 포인트를 소모하며 사망 및 전투 이탈 시 고유 액티브를 정리합니다.</summary>
    private void Tick(float deltaTime)
    {
        if (active == false)
        {
            return;
        }
        if (CanUseActive() == false || data == null || data.HasValidPointSettings() == false)
        {
            Deactivate();
            return;
        }

        // 시간 진행은 기존 EffectComponent 한 곳에서 담당합니다.
        points = activeEffect != null ? Mathf.Max(0f, activeEffect.RemainingTime * data.pointsPerSecond) : 0f;
        if (points <= 0f)
        {
            Deactivate();
        }
    }

    /// <summary>드론 고유 액티브만 해제합니다. 재발동한 기억은 각자의 지속시간을 유지합니다.</summary>
    private void Deactivate()
    {
        bool wasActive = active;
        active = false;
        var previous = activeEffect;
        activeEffect = null;
        if (previous != null)
        {
            previous.OnRemovedUI -= OnActiveEffectRemoved;
            effects.SafeInvoke(component => component.RemoveEffect(previous, true));
        }
        if (wasActive)
        {
            points = previous != null && data != null
                ? Mathf.Clamp(previous.RemainingTime * data.pointsPerSecond, 0f, data.maximumPoints) : 0f;
            PublishState();
        }
    }

    /// <summary>기존 버프 시스템에서 만료 또는 해제된 경우에도 충전 UI를 즉시 복원합니다.</summary>
    private void OnActiveEffectRemoved(BaseEffect effect)
    {
        if (ReferenceEquals(activeEffect, effect))
        {
            Deactivate();
        }
    }

    private void PublishState()
    {
        hud.SafeInvoke(handler => handler.OnChangeGuideDrone(this));
    }

    /// <summary>자신이 적용한 효과와 전투 이벤트 구독을 회수합니다.</summary>
    private void Release()
    {
        Deactivate();
        foreach (var modifier in passiveModifiers)
        {
            status.SafeInvoke(component => component.RemoveBuff(modifier));
        }
        passiveModifiers.Clear();
        if (battle != null)
        {
            battle.OnBattleEnded -= Deactivate;
        }
        battle = null;
        points = 0f;
        initialized = false;
        if (hud.SafeInvoke(handler => handler.CurrentGuideDrone) == this)
        {
            hud.SafeInvoke(handler => handler.OnChangeGuideDrone(null));
        }
    }

    private void OnDisable()
    {
        Release();
    }
}
