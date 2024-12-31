using System;
using UnityEngine;

namespace BT.Nodes
{
    // 최하위 작업 노드 
    public abstract class TaskNode : BTNode
    {
        protected enum ActionState
        {
            Begin, Update, End
        }

        protected NodeState previousResult;
        protected ActionState currActionState;

        protected Func<BTNode.NodeState> onBegin = null;
        protected Func<BTNode.NodeState> onUpdate = null;
        protected Func<BTNode.NodeState> onEnd = null;
        protected Func<BTNode.NodeState> onAbort = null;

        #region Constructor

        public TaskNode(GameObject owner = null, SO_Blackboard blackboard = null,
            Func<BTNode.NodeState> onBegin = null, Func<BTNode.NodeState> onUpdate = null,
            Func<BTNode.NodeState> onEnd = null)
        {
            this.owner = owner;
            this.blackboard = blackboard;

            currActionState = ActionState.Begin;

            this.onBegin = onBegin;
            this.onUpdate = onUpdate;
            this.onEnd = onEnd;
            this.onAbort = OnAbort;
        }


        public TaskNode(Func<BTNode.NodeState> onBegin = null,
            Func<BTNode.NodeState> onUpdate = null,
            Func<BTNode.NodeState> onEnd = null)
        {
            currActionState = ActionState.Begin;

            this.onBegin = onBegin;
            this.onUpdate = onUpdate;
            this.onEnd = onEnd;
            this.onAbort = OnAbort;
        }

        #endregion


        public void SetOwnerObject(GameObject owner)
        {
            this.owner = owner;
        }

        public void SetBlackboard(SO_Blackboard blackboard)
        {
            this.blackboard = blackboard;
        }

        //////////////////////////////////////////////////////////////////////////////////// 


        /// <summary>
        /// 액션 노드의 ActionState 값을 변경 시키는 함수 
        /// </summary>
        /// <param name="newState"></param>
        protected void ChangeActionState(ActionState newState) => currActionState = newState;


        public override NodeState Evaluate()
        {
            // 각 상태를 분리하여 매 프레임마다 평가
            switch (currActionState)
            {
                case ActionState.Begin:
                {
                    // Begin 상태일 때 onBegin을 호출 후 Running 반환
                    nodeState = onBegin?.Invoke() ?? BTNode.NodeState.Failure;
                    
                    if (nodeState == NodeState.Running)
                        ChangeActionState(ActionState.Update);
                    
                    previousResult = nodeState;

                    return nodeState;
                }
                case ActionState.Update:
                {
                    // Update 상태일 때 onUpdate 호출 후 결과에 따라 End로 넘길 수 있다.
                    nodeState = onUpdate?.Invoke() ?? BTNode.NodeState.Failure;
                    
                    if(nodeState == NodeState.Success || nodeState == NodeState.Failure)
                        ChangeActionState(ActionState.End);
                    
                    previousResult = nodeState;
                    return NodeState.Running;
                }
                case ActionState.End:
                {
                    //Debug.Log($"{nodeName} Action Node End ");
                    //bRunning = false;
                    nodeState = onEnd?.Invoke() ?? BTNode.NodeState.Failure;
                    if (nodeState == NodeState.Success || nodeState == NodeState.Failure)
                        ChangeActionState(ActionState.Begin);
                    
                    previousResult = nodeState;
                    return nodeState; 
                }
                default:
                return NodeState.Failure;
            }

        }

        protected virtual BTNode.NodeState OnBegin()
        {
            return BTNode.NodeState.Success;
        }


        protected virtual BTNode.NodeState OnUpdate()
        {

            return BTNode.NodeState.Success;
        }


        protected virtual BTNode.NodeState OnEnd()
        {
            return BTNode.NodeState.Success;
        }

        protected virtual BTNode.NodeState OnAbort()
        {
            return BTNode.NodeState.Abort;
        }

        public void AbortTask()
        {
            //Debug.Log($"NodeName {nodeName} Abort");
            if (previousResult == NodeState.Running) 
            {
                onAbort?.Invoke();
                ChangeActionState(ActionState.End);
            }
        }

        public override string ToString()
        {
            return onUpdate.Method.Name;
        }

        public override void StopEvaluate()
        {
            AbortTask();
            ChangeActionState(ActionState.End);
        }
    }

}