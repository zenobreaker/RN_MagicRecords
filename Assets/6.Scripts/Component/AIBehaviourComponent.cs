using System;
using Unity.Behavior;
using UnityEngine;



public class AIBehaviourComponent : MonoBehaviour
{

    [SerializeField]
    private string CanMoveName = "CanMove";
    [SerializeField]
    private string MoveEnableName = "MoveEnable";
    [SerializeField]
    private string AIStateTypeName = "State";
    [SerializeField]
    private string TargetName = "Target";

    private BehaviorGraphAgent bgAgent;
    private StateComponent state;

    private AIState aiState;

    private void Awake()
    {
        bgAgent = GetComponent<BehaviorGraphAgent>();
        Debug.Assert(bgAgent != null);

        state = GetComponent<StateComponent>();
        Debug.Assert(state != null);

        state.OnStateTypeChanged += OnStateTypeChanged;
    }

    private void OnDestroy()
    {
        if(state != null)
            state.OnStateTypeChanged -= OnStateTypeChanged;
    }

    public bool WaitMode { get => aiState == AIState.Wait; }
    public bool PatrolMode { get => aiState == AIState.Patrol; }
    public bool ApproachMode { get => aiState == AIState.Approach; }
    public bool ActionMode { get => aiState == AIState.Action; }
    public bool DamagedMode { get => aiState == AIState.Damaged; }
    public bool DeadMode { get => aiState == AIState.Dead; }

    public void SetWaitMode() => ChangedState(AIState.Wait);
    public void SetPatrolMode() => ChangedState(AIState.Patrol);
    public void SetApproachMode() => ChangedState(AIState.Approach);
    public void SetActionMode() => ChangedState(AIState.Action);
    public void SetDamagedMode() => ChangedState(AIState.Damaged);
    public void SetDeadMode() => ChangedState(AIState.Dead);


    public bool GetCanMove()
    {
        if (bgAgent.GetVariable<bool>(CanMoveName, out BlackboardVariable<bool> result))
            return result.Value;

        return true;
    }
    public void SetCanMove(bool canMove)
    {
        bgAgent?.SetVariableValue<bool>(CanMoveName, canMove);
    }

    public bool GetMoveEnable()
    {
        if (bgAgent.GetVariable<bool>(MoveEnableName, out BlackboardVariable<bool> result))
            return result.Value;

        return true; 
    }

    public void SetMoveEnable(bool moveEnable)
    {
        bgAgent?.SetVariableValue<bool> (MoveEnableName, moveEnable);
    }

    public GameObject GetTarget()
    {
        if(bgAgent.GetVariable<GameObject>(TargetName, out BlackboardVariable<GameObject> target))
            return target.Value;
        return null;
    }

    public void SetTarget(GameObject target)
    {
        bgAgent?.SetVariableValue<GameObject>(TargetName, target); 
    }


    private void ChangedState(AIState state)
    {
        if (aiState == state) return;

        aiState = state; 
        bgAgent.SetVariableValue<AIState>(AIStateTypeName, state);
    }

    private void OnStateTypeChanged(StateType oldType, StateType newType)
    {
        bool bCanMove = false; 
        switch(newType)
        {
            case StateType.Idle: SetWaitMode(); bCanMove = true; break; 
            case StateType.Action: SetActionMode(); bCanMove = false; break; 
            case StateType.Damaged: SetDamagedMode(); bCanMove = false; break; 
            case StateType.Dead: SetDeadMode(); bCanMove = false; break; 
            case StateType.Stop: SetWaitMode(); bCanMove = false; break; 
        }

        bCanMove &= GetMoveEnable();
        SetCanMove(bCanMove);
    }
}
