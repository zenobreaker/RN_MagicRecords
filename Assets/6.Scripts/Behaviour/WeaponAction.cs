using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Weapon", story: "Try Attack with [AIController]", category: "Action", id: "80427657f30cee1fb3681ada612a869e")]
public partial class WeaponAction : Action
{
    [SerializeReference] public BlackboardVariable<AIController> aiController;
    private StateComponent state;
    private bool hasStartAction = false; 
    protected override Status OnStart()
    {
        if (aiController == null || aiController.Value == null
            || aiController.Value.State == null)
            return Status.Failure;

        state = aiController.Value.State;
        hasStartAction = false; 

        // 일단 공격 명령을 내린다. 
        aiController.Value.DoAction();

        // DoAction 안의 조건문에 걸려서 ActionMode로 진입하지 못했다면 실패처리 
        if (state.IdleMode)
            return Status.Failure;

        // 액션모드로 들어갔다면 성공 
        hasStartAction = true; 

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if(state == null) 
            return Status.Failure;

        // 상황 A 아직 공격 중 
        if (state.ActionMode)
            return Status.Running;

        // 상황 B 공격이 끝나서 Idle로 온 경우
        if (state.IdleMode && hasStartAction)
            return Status.Success;

        // 상황 C ActionMode도 아니고 Idle도 아닌 경우 
        // 이때는 공격 중단된 것이므로 실패처리 
        return Status.Failure;
    }
}

