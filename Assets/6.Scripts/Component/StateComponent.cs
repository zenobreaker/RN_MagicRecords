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
    public StateType Type { get => type;  }

    public event Action<StateType, StateType> OnStateTypeChanged;

    private StatusEffectComponent statusEffect;

    private void Awake()
    {
        statusEffect = GetComponent<StatusEffectComponent>();
        Debug.Assert(statusEffect != null);

        statusEffect.OnStatusEffectChanged += OnStatusEffectChanged;
    }

    private void OnDestroy()
    {
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
    public void SetDamagedMode() => ChangeType(StateType.Damaged);
    public void SetDeadMode() => ChangeType(StateType.Dead);
    public void SetStopMode() => ChangeType(StateType.Stop);


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

        bool? movable = null;
        movable = statusEffect?.GetMovableCondition();
        if (movable != null && movable.Value == true) 
            SetStopMode();
        else 
            SetIdleMode();
    }
}

