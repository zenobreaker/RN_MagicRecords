using System;
using UnityEngine;

public enum StateType
{
    Idle = 0, Equip, Action, Evade, Damaged, Dead, Stop, Max
}

/// <summary>
/// 상태 결정 컴포넌트 
/// </summary>
public partial class StateComponent : MonoBehaviour
{
    private StateType type = StateType.Idle;
    public StateType Type { get => type; }

    public event Action<StateType, StateType> OnStateTypeChanged;

    private StatusEffectComponent statusEffect;

    // 💡 자신을 소유한 Character 정보 (행동 취소 및 비주얼 제어를 위함)
    private Character character;

    private void Awake()
    {
        character = GetComponent<Character>();

        statusEffect = GetComponent<StatusEffectComponent>();
        Debug.Assert(statusEffect != null);

        if (statusEffect != null) statusEffect.OnStatusEffectChanged += OnStatusEffectChanged;
        launch = GetComponent<LaunchComponent>();
        skills = GetComponent<SkillComponent>();
    }

    private void OnDestroy()
    {
        if (statusEffect != null)
            statusEffect.OnStatusEffectChanged -= OnStatusEffectChanged;
    }

    public bool IdleMode { get => type == StateType.Idle; }
    public bool EquipMode { get => type == StateType.Equip; }
    public bool ActionMode { get => type == StateType.Action; }
    public bool EvadeMode { get => type == StateType.Evade; }
    public bool DamagedMode { get => type == StateType.Damaged; }
    public bool DeadMode { get => type == StateType.Dead; }
    public bool StopMode { get => type == StateType.Stop; }

    public void SetIdleMode() => ChangeType(StateType.Idle);
    public void SetEquipMode() => ChangeType(StateType.Equip);
    public void SetActionMode() => ChangeType(StateType.Action);
    public void SetEvadeMode() => ChangeType(StateType.Evade);
    public void SetDeadMode() => ChangeType(StateType.Dead);
    public void SetStopMode() => ChangeType(StateType.Stop);

    // Compatibility entry point for existing callers without a DamageEvent.
    public void SetDamagedMode(HitData hitData = null)
    {
        if (DeadMode || StopMode) return;
        legacyDamageAnimation = true;
        ChangeType(StateType.Damaged);

        // 2. 시각적 피격 애니메이션 재생!
        if (character != null && hitData != null && character.Visual != null)
        {
            character.Visual.PlayDamageAnimation(hitData);
        }
    }

    private void ChangeType(StateType type)
    {
        if (this.type == type)
            return;
        //Debug.Log($"{character.name} State Change {type}");
        StateType prevType = this.type;
        this.type = type;

        if (type == StateType.Dead || type == StateType.Stop)
            CancelHitReaction();
        if (type == StateType.Damaged || type == StateType.Stop || type == StateType.Dead)
            skills?.CancelCurrentSkill();

        OnStateTypeChanged?.Invoke(prevType, type);
    }

    private void OnStatusEffectChanged(StatusEffectType oldSE, StatusEffectType newSE)
    {
        if (statusEffect == null || DeadMode) return;
        if (!statusEffect.GetMovableCondition())
            SetStopMode();
        else if (StopMode)
            SetIdleMode();
    }
}
