using UnityEngine;

namespace BT.Nodes
{
    public class InvertorNode : BTNode
    {
        BTNode child;

        public InvertorNode(GameObject owner, BTNode child)
        {
            this.child = child;
        }

        public override NodeState Evaluate()
        {
            if (child == null)
                return NodeState.Success;

            NodeState childState = child.Evaluate();

            if (childState == NodeState.Success)
                return NodeState.Failure;

            return NodeState.Success;
        }

        public override void StopEvaluate()
        {
            child.StopEvaluate();
        }
    }
}
