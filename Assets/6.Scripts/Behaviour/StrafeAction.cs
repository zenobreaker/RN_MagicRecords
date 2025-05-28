using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StrafeAction", story: "[Self] Strafe to [Target] with [Radius]", category: "Action", id: "4199cff30d4c561053466e4fd716cab1")]
public partial class StrafeAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Radius;

    private NavMeshAgent agent;
    private List<Vector3> points = new List<Vector3>();
    private Vector3 closestPoint = Vector3.zero;
    private Vector3 selfPosition = Vector3.zero;
    protected override Status OnStart()
    {
        if (Target == null || Target.Value == null)
            return Status.Failure;

        agent = Self.Value.GetComponent<NavMeshAgent>();
        if (agent == null) return Status.Failure;

        Vector3 center = Target.Value.transform.position;
        points = PositionHelpers.GenerateCirclePoints(center, Radius.Value);

        //각 지점 중 갈 수 없는 곳이 있으면 탈락
        points = points.Where(p =>
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(p, out hit, 0.5f, NavMesh.AllAreas);
        }).ToList();

        if (points.Count == 0)
            return Status.Failure;

        // Self로부터 가장 가까운 point 검색 
        selfPosition = Self.Value.transform.position;
        //TODO: 자신이랑 너무 가까운 (이전에 도착해버린) 값이면 빼고 다시 구하기 
        closestPoint = points.OrderBy(p => Vector3.SqrMagnitude(p-selfPosition)).First();
        
        agent.SetDestination(closestPoint); 

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (agent == null)
            return Status.Failure;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            return Status.Success;

        //float thresholdSqr = 0.1f * 0.1f;
        //if ((Self.Value.transform.position - closestPoint).sqrMagnitude <= thresholdSqr)
        //{
        //    return Status.Success;
        //}

        return Status.Running;
    }
}

