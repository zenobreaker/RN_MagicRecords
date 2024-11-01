using System;
using UnityEngine;


/// <summary>
/// 상태 결정 컴포넌트 
/// </summary>
public class StateComponent : MonoBehaviour
{
    public enum StateType
    {
        Idle = 0, Equip, Action, Evade, Damaged, Dead,
    }

    private StateType type = StateType.Idle;
    public StateType Type { get => type;  }

    public event Action<StateType, StateType> OnStateTypeChanged;

    public bool IdleMode { get => type == StateType.Idle; }
    public bool EquipMode { get => type == StateType.Equip; }
    public bool ActionMode { get => type == StateType.Action; }
    public bool EvadeMode { get => type == StateType.Evade; }
    public bool DamagedMode { get => type == StateType.Damaged; }
    public bool DeadMode { get => type == StateType.Dead; }

    public void SetIdleMode() => ChangeType(StateType.Idle);
    public void SetEquipMode() => ChangeType(StateType.Equip);
    public void SetActionMode() => ChangeType(StateType.Action);
    public void SetEvadeMode() => ChangeType(StateType.Evade);
    public void SetDamagedMode() => ChangeType(StateType.Damaged);
    public void SetDeadMode() => ChangeType(StateType.Dead);


    private void ChangeType(StateType type)
    {
        if (this.type == type)
            return;

        StateType prevType = this.type;
        this.type = type;

        OnStateTypeChanged?.Invoke(prevType, type);
    }

}
