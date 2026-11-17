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
    private readonly List<BaseEffect> activeEffects = new();
    private float points;
    private bool initialized;
    private bool active;

    public SO_GuideDroneData Data => data;
    public float Points => points;
    public bool IsActive => active;

    private void Awake()
    {
        status = GetComponent<StatusComponent>();
        effects = GetComponent<EffectComponent>();
        health = GetComponent<HealthPointComponent>();
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
    }

    /// <summary>전투 보상 등에서 지급할 포인트를 상한까지 더합니다. 지급 조건은 호출 측에서 결정합니다.</summary>
    public void AddPoints(float amount)
    {
        if (initialized == false || data == null || amount <= 0f ||
            float.IsNaN(amount) || float.IsInfinity(amount))
        {
            return;
        }
        points = Mathf.Min(data.maximumPoints, points + amount);
    }

    /// <summary>고유 액티브와 보유 기억의 지속 효과를 한 번 발동하고 포인트 소모를 시작합니다.</summary>
    public bool TryActivate()
    {
        if (initialized == false || active || data == null || data.HasValidPointSettings() == false ||
            points < data.activationPoints || CanUseActive() == false || Time.timeScale <= 0f)
        {
            return false;
        }

        active = true;
        for (int i = 0; i < data.activeStats.Count; i++)
        {
            var entry = data.activeStats[i];
            if (entry == null)
            {
                continue;
            }

            var effect = new StatBuffEffect($"GuideDrone:{GetInstanceID()}:{i}", 0f,
                new StatModifier(entry.Status, entry.Value, entry.ValueType), null,
                data.droneName, BuffStackPolicy.IGNOREIFEXSIST);
            effect.FxIcon = data.icon;
            effects.ApplyEffect(effect, gameObject, gameObject);
            activeEffects.Add(effect);
        }

        AppManager.Instance.SafeInvoke(app => app.GetRecordManager())
            .SafeInvoke(records => records.ReactivateMemoryRecords(gameObject));
        return true;
    }

    private bool CanUseActive()
    {
        return isActiveAndEnabled && battle != null && battle.IsInBattle &&
            health != null && health.Dead == false;
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

        points = Mathf.Max(0f, points - data.pointsPerSecond * deltaTime);
        if (points <= 0f)
        {
            Deactivate();
        }
    }

    /// <summary>드론 고유 액티브만 해제합니다. 재발동한 기억은 각자의 지속시간을 유지합니다.</summary>
    private void Deactivate()
    {
        active = false;
        foreach (var effect in activeEffects)
        {
            effects.SafeInvoke(component => component.RemoveEffect(effect, true));
        }
        activeEffects.Clear();
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
    }

    private void OnDisable()
    {
        Release();
    }
}
