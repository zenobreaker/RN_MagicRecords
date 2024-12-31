using BT.Nodes;
using System;
using UnityEngine;
using UnityEngine.AI;


namespace BT.TaskNodes
{
    /// <summary>
    /// MoveToNode 는 NavMeshAgent 컴포넌트의 의존도가 높다.
    /// </summary>
    public class MoveToNode : TaskNode
    {


        private NavMeshAgent agent;
        private Vector3 target;
        public Vector3 Target { set => target = value; }

        private bool observeValue = false;
        public Action<Vector3> OnDestination;
        public MoveToNode(GameObject ownerObject, SO_Blackboard blackboard, bool observeValue = false)
            : base(ownerObject, blackboard)
        {
            nodeName = "MoveTo";

            agent = ownerObject.GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = ownerObject.AddComponent<NavMeshAgent>();

            onBegin = OnBegin;
            onUpdate = OnUpdate;
            onEnd = OnEnd;
            onAbort = OnAbort;
            this.observeValue = observeValue;
        }

        // 도착 지점을 언제 어디서 어떻게 세팅할 것인지가 문제다. 
        // 해결방안 1. OnBegin함수를 생성자에서 전달받아서 처리해본다. => MoveTo 전용 OnBegin 등등 필요할지도?

        private bool AgentCheck()
        {
            bool bCheck = true;
            bCheck &= agent != null;
            bCheck &= agent.enabled;

            return bCheck;
        }

        protected override NodeState OnBegin()
        {
            if (AgentCheck() == false)
                return NodeState.Failure;
            
            if (blackboard != null)
            {
                ResearchTarget();
                
                return NodeState.Running;
            }

            return NodeState.Failure;
        }

        protected override NodeState OnUpdate()
        {
            if (AgentCheck() == false || CheckPath() == false)
                return NodeState.Failure;

            //Debug.Log($"Move Update / {currActionState}");
            if (CalcArrive() == false)
            {
                if (observeValue)
                    ResearchTarget();
                
                return NodeState.Running;
            }

            ResetAgent();
            ChangeActionState(ActionState.End);
            return NodeState.Success;
        }

        protected override NodeState OnEnd()
        {
            ResetAgent();

            return base.OnEnd();
        }

        private void ResearchTarget()
        {
            GameObject targetObject = blackboard.GetValue<GameObject>("Target");
            if (targetObject != null)
            {
                target = targetObject.transform.position;
                agent.SetDestination(target);
                OnDestination?.Invoke(target);
            }
            else
                ResetAgent();
        }


        private void ResetAgent()
        {
            if (AgentCheck() == false)
                return;

            if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
                agent.ResetPath();
            agent.velocity = Vector3.zero;
            //agent.isStopped = true;
        }

        protected override NodeState OnAbort()
        {
            // Debug.Log($"Move Abort / {currActionState}");
            ResetAgent();

            return base.OnAbort();
        }

        private bool CalcArrive()
        {
            float distanceSquared = (target - agent.transform.position).magnitude;
            distanceSquared = Mathf.Floor(distanceSquared * 10) / 10;
            
            if (distanceSquared <= agent.stoppingDistance ||
                agent.remainingDistance <= agent.stoppingDistance)
            {
                Debug.Log("도착");
                return true;
            }
            
            return false;
        }

        private bool CheckPath()
        {
            if (AgentCheck() == false)
                return false;

            NavMeshPath path = new NavMeshPath();
            return agent.CalculatePath(target, path);
        }

    }
}
