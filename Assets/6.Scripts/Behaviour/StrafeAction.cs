using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StrafeAction", story: "[Self] Strafe to [Target] with [Radius] from between [MinCooldown] and [MaxCooldown]", category: "Action", id: "4199cff30d4c561053466e4fd716cab1")]
public partial class StrafeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<float> MinCooldown;
    [SerializeReference] public BlackboardVariable<float> MaxCooldown;

    private NavMeshAgent agent;
    private List<Vector3> points = new List<Vector3>();
    private Vector3 closestPoint = Vector3.zero;
    private Vector3 selfPosition = Vector3.zero;
    private Vector3 targetPosition = Vector3.zero;
    private int prevIndex = -1;
    private int direction = -1; // 1 = 시계 방향, -1 = 반시계 방향
    private float cooldown = 0;
    
    protected override Status OnStart()
    {
        if (Target == null || Target.Value == null)
            return Status.Failure;

        if (!Self.Value.TryGetComponent<NavMeshAgent>(out agent)) return Status.Failure;

        targetPosition = Target.Value.transform.position;
        //TODO: 대상의 위치각도 기반으로 방향 변경 가능할 수 있다.
        direction = UnityEngine.Random.Range(0, 2) == 0? -1 : 1;
        cooldown = UnityEngine.Random.Range(MinCooldown.Value, MaxCooldown.Value);

        Vector3 center = Target.Value.transform.position;
        points = PositionHelpers.GenerateCirclePoints(center, Radius.Value);

        return SetNextStep();
    }

    protected override Status OnUpdate()
    {
        if (agent == null)
            return Status.Failure;
        
        // 적이 해당 자리를 많이 벗어난 경우 스트레이프 종료
        if (ChangedTargetPosition(Target.Value.transform.position))
            return Status.Success;

        // 타임 아웃되면 스트레이프 종료 
        cooldown -= Time.deltaTime;
        if (cooldown <= 0.0f)
            return Status.Success;

        // 목표에 도달하면 다음 목표로 
        if (agent.isActiveAndEnabled && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return SetNextStep();
        }

        return Status.Running;
    }

    private bool ChangedTargetPosition(Vector3 targetPosition)
    {
        float minDiff = 1.25f * 1.25f;
        if (Vector3.SqrMagnitude(this.targetPosition - targetPosition) >= minDiff) return true;

        return false; 
    }

    private Status SetNextStep()
    {
        selfPosition = Self.Value.transform.position;


        //각 지점 중 갈 수 없는 곳이 있으면 탈락
        points = points.Where(p =>
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(p, out hit, 0.5f, NavMesh.AllAreas);
        }).ToList();
        if (points.Count == 0) return Status.Failure;

        //자신이랑 너무 가까운 (이전에 도착해버린) 값이면 빼고 다시 구하기 
        //float minDistanceFormSelfSqr = 0.5f * 0.5f;
        //var candidatePoints = points.Where(p => (p - selfPosition).sqrMagnitude >= minDistanceFormSelfSqr).ToList();
        //if (candidatePoints.Count == 0) return Status.Failure;

        SetClosestPoint(points);
        return Status.Running;
    }

    private void SetClosestPoint(List<Vector3> points)
    {
#if UNITY_EDITOR
        Cheater.Instance?.DrawSphereWithPoints(points);
#endif
        // Self로부터 가장 가까운 point 검색 
        int nextIndex;

        if (prevIndex == -1)
        {
            float minDist = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float dist = (points[i] - selfPosition).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    prevIndex = i;
                }
            }
        }

        // 다음 인덱스 계산
        nextIndex = (prevIndex + direction + points.Count) % points.Count;
        closestPoint = points[nextIndex];
        prevIndex = nextIndex;

        agent.SetDestination(closestPoint);
    }
}

