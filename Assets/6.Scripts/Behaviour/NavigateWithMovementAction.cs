using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Navigate With Movement",
    story: "[Agent] navigates to [Target] using [Movement]",
    category: "Action/Navigation",
    id: "CustomNavigateToTarget_MagicalRecords")]
public partial class NavigateWithMovementAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<MovementComponent> Movement;
    [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new BlackboardVariable<float>(1.5f);

    private NavMeshAgent m_NavMeshAgent;
    private Vector3 lastTargetPos; // 💡 타겟이 움직였는지 확인하기 위한 변수

    protected override Status OnStart()
    {
        if (Agent.Value == null || Target.Value == null || Movement.Value == null)
            return Status.Failure;

        m_NavMeshAgent = Agent.Value.GetComponent<NavMeshAgent>();
        if (m_NavMeshAgent == null)
            return Status.Failure;

        m_NavMeshAgent.enabled = true;
        m_NavMeshAgent.updatePosition = false;
        m_NavMeshAgent.updateRotation = false;

        // 시작할 때 타겟의 위치를 저장하고 목적지로 설정
        lastTargetPos = Target.Value.transform.position;
        m_NavMeshAgent.SetDestination(lastTargetPos);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent.Value == null || Target.Value == null || Movement.Value == null)
            return Status.Failure;

        Vector3 currentAgentPos = Agent.Value.transform.position;
        Vector3 currentTargetPos = Target.Value.transform.position;

        // 1. 내 위치 동기화
        m_NavMeshAgent.nextPosition = currentAgentPos;

        // 💡 2. 함정 회피: 타겟이 0.1 이상 움직였을 때만 경로 재탐색 (매 프레임 재계산 방지!)
        if (Vector3.Distance(lastTargetPos, currentTargetPos) > 0.1f)
        {
            lastTargetPos = currentTargetPos;
            m_NavMeshAgent.SetDestination(currentTargetPos);
        }

        // 💡 3. 함정 회피: 3D 거리가 아닌 X, Z축(바닥 평면) 거리만 계산! (높이 차이 무시)
        Vector3 agentPos2D = new Vector3(currentAgentPos.x, 0, currentAgentPos.z);
        Vector3 targetPos2D = new Vector3(currentTargetPos.x, 0, currentTargetPos.z);
        float distance = Vector3.Distance(agentPos2D, targetPos2D);

        // 4. 사정거리 안에 들어오면 정지
        if (distance <= DistanceThreshold.Value)
        {
            Movement.Value.SetDirection(Vector2.zero); // 브레이크 콱!
            return Status.Success;
        }

        // 5. 경로 계산 중 대기
        if (m_NavMeshAgent.pathPending)
            return Status.Running;

        // 6. 다음 이동할 곳으로 방향 지시
        Vector3 directionToTarget = m_NavMeshAgent.steeringTarget - currentAgentPos;
        directionToTarget.y = 0; // 평면 이동

        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Vector2 inputDir = new Vector2(directionToTarget.x, directionToTarget.z).normalized;
            Movement.Value.SetDirection(inputDir);
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Movement.Value != null)
        {
            Movement.Value.SetDirection(Vector2.zero);
        }

        if (m_NavMeshAgent != null && m_NavMeshAgent.isOnNavMesh)
        {
            m_NavMeshAgent.ResetPath();
        }
    }
}