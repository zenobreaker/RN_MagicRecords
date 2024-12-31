using BT.Nodes;
using UnityEngine;


namespace BT.TaskNodes
{
    public class DebugNode : TaskNode
    {

        [SerializeField] private string message; 

        public DebugNode()
            : base(null, null, null)
        {
            onBegin = OnBegin;
            onUpdate = OnUpdate;
            onEnd = OnEnd;
        }

        public override BTNode Clone()
        {
            return base.Clone();
        }
        protected override NodeState OnAbort()
        {
            return base.OnAbort();
        }

        protected override NodeState OnBegin()
        {
            Debug.Log($"{message}");

            return NodeState.Success;
        }

        protected override NodeState OnEnd()
        {
            return base.OnEnd();
        }

        protected override NodeState OnUpdate()
        {
            return base.OnUpdate();
        }
    }
}
