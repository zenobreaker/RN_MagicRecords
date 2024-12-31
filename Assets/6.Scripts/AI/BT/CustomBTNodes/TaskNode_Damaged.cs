using BT.Nodes;
using UnityEngine;

namespace BT.CustomBTNodes
{

    public class TaskNode_Damaged : TaskNode
    {

        private BTAIController controller;
        private StateComponent state; 

        public TaskNode_Damaged(GameObject owner, SO_Blackboard blackboard)
            : base(owner, blackboard)
        {
            nodeName = "Damaged";

            controller = owner?.GetComponent<BTAIController>();
            state = owner?.GetComponent<StateComponent>();

            onBegin = OnBegin;
            onUpdate = OnUpdate;
            onEnd = OnEnd;
        }

        protected override NodeState OnBegin()
        {
            if (controller == null)
                return NodeState.Failure;

            Debug.Log("Damaged Node Begin");

            controller.StopMovement();

            return base.OnBegin();
        }

        protected override NodeState OnUpdate()
        {
            if(state == null) return NodeState.Failure;

            Debug.Log("Damaged Node Update");

            if (state.DamagedMode)
                return NodeState.Running;

            return base.OnUpdate();
        }

        protected override NodeState OnEnd() 
        {
            if (controller == null)
                return NodeState.Failure;

            Debug.Log("Damaged Node End");

            controller.StartMovement();

            return base.OnEnd();
        }

    }
}