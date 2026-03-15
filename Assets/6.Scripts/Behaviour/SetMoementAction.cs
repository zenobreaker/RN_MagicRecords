using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
// 💡 story 속성에 [변수명]을 적으면 노드 UI에 슬롯이 자동으로 생깁니다!
[NodeDescription(name: "Set Movement Speed", story: "Set [MoveSpeed] from [Movement]", category: "Action", id: "fa37dbbd3172981f35f9a225fea99693")]
public partial class SetMoementAction : Action
{
    // 1. 블랙보드에서 받아올 MovementComponent 변수
    [SerializeReference] public BlackboardVariable<MovementComponent> Movement;

    // 2. 속도값을 저장할 float형 블랙보드 변수
    [SerializeReference] public BlackboardVariable<float> MoveSpeed;

    protected override Status OnStart()
    {
        // 방어 코드: 컴포넌트가 연결 안 되어 있으면 노드 실행 실패 처리
        if (Movement == null || Movement.Value == null || MoveSpeed == null)
        {
            Debug.LogWarning("SetMovementAction: 블랙보드 변수가 연결되지 않았습니다.");
            return Status.Failure;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        // 3. MovementComponent에서 속도값을 가져와서 블랙보드의 MoveSpeed에 세팅!
        // (애니메이터에 쓰는 비율값이면 Speed, 실제 이동 수치면 Speed를 쓰시면 됩니다)
        MoveSpeed.Value = Movement.Value.Speed;

        // 값을 세팅하고 바로 끝나길 원한다면 Success 반환
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}