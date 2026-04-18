using System;
using UnityEngine;

public enum StateType
{
    Idle = 0, Equip, Action, Evade, Damaged, Dead, Stop, Max
}

/// <summary>
/// 상태 결정 컴포넌트 
/// </summary>
public class StateComponent : MonoBehaviour
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

        statusEffect.OnStatusEffectChanged += OnStatusEffectChanged;
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

    // 💡 가장 중요한 부분: 피격 상태 진입 시 외부의 개입 없이 스스로 리액션을 챙깁니다!
    // DamageHandleComponent에서는 이제 SetDamagedMode(hitData)만 호출하면 됩니다.
    public void SetDamagedMode(HitData hitData = null)
    {
        // 1. 상태를 Damaged로 변경
        ChangeType(StateType.Damaged);

        // 2. 현재 공격 중이거나 행동 중이었다면 취소 (경직)
        character?.End_Damaged();

        // 3. 시각적 피격 애니메이션 재생!
        if (hitData != null)
        {
            character?.Visual?.PlayDamageAnimation(hitData);
        }
    }

    private void ChangeType(StateType type)
    {
        if (this.type == type)
            return;

        StateType prevType = this.type;
        this.type = type;

        OnStateTypeChanged?.Invoke(prevType, type);
    }

    private void OnStatusEffectChanged(StatusEffectType oldSE, StatusEffectType newSE)
    {
        if (statusEffect == null) return;

        bool? movable = statusEffect?.GetMovableCondition();
        if (movable != null && movable.Value == true)
            SetStopMode();
        else
            SetIdleMode();
    }
}