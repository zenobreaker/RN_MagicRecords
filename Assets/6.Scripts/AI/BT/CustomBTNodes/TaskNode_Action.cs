using BT.Nodes;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace BT.CustomBTNodes
{
    public class TaskNode_Action : TaskNode
    {

        BTAIController controller;
        StateComponent state;
        ActionComponent action;

        public TaskNode_Action(GameObject owner, SO_Blackboard blackboard)
            : base(owner, blackboard)
        {
            this.nodeName = "Action";

            action = owner.GetComponent<ActionComponent>();
            controller = owner.GetComponent<BTAIController>();
            state = owner.GetComponent<StateComponent>();
            onBegin = OnBegin;
            onUpdate = OnUpdate;
            onEnd = OnEnd;
            onAbort = OnAbort;
        }


        protected override NodeState OnBegin()
        {
            if (action == null)
                return NodeState.Failure;

            action.DoAction();

            return NodeState.Running;
        }


        protected override NodeState OnUpdate()
        {
            if (state == null || controller == null)
            {
                return NodeState.Failure;
            }

            //Debug.Log($"{nodeName} action update");
            bool bCheck = true;
            bCheck &= (state.IdleMode);
            bCheck &= action.InAction == false;

            if (bCheck)
            {
                return NodeState.Success;
            }

            //Debug.Log($"{nodeName} action end {state.IdleMode}");
            //controller.SetWaitMode(bCheck);
            return NodeState.Running;
        }


        protected override NodeState OnAbort()
        {
            if(currActionState == ActionState.End)
                return NodeState.Abort;

            state.SetIdleMode();
            action.End_DoAction();

            Debug.Log($"{nodeName} action abort {state.IdleMode}");

            return NodeState.Success;
        }
    }
}
