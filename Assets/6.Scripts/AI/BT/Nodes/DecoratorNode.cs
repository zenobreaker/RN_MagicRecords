using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace BT.Nodes
{
    [System.Serializable]
    public abstract class DecoratorNode
        : BTNode
    {
        protected bool isRunning;
        protected string boardKey;
        //protected string keyValue;

        [SerializeField]
        protected BTNode childNode;
        public BTNode ChildNode { get => childNode; set => childNode = value; }


        public DecoratorNode(string nodeName,
            BTNode childNode,
            GameObject owner = null,
            SO_Blackboard blackboard = null,
            string boardKey = null)
        {
            this.nodeName = nodeName;
            this.owner = owner;
            this.childNode = childNode;
            this.blackboard = blackboard;
            this.boardKey = boardKey;
        }

        public void SetOwnerObject(GameObject owner)
        {
            this.owner = owner;
        }

        public override NodeState Evaluate()
        {
            OnStart();

            NodeState result = NodeState.Failure;
            if (ShouldExecute())
            {
                result = childNode.Evaluate();
            }

            if (result != NodeState.Running)
                OnEnd();

            return result;
        }

        protected abstract bool ShouldExecute();

        protected virtual void OnStart()
        {
            isRunning = true;
        }
        protected virtual void OnEnd()
        {
            isRunning = false;
        }

       
        public virtual void AbortTask()
        {
            //Debug.Log($"Task Aboarted  {nodeName}");
            if (isRunning == false)
                return;
            // 자식들을 순회하면서 AbortTask 함수 실행
            if (childNode is CompositeNode composite)
            {
                composite.AbortTask();
            }
            else if(childNode is TaskNode taskNode)
            {
                taskNode.AbortTask();
            }
            else if( childNode is DecoratorNode decorator)
            {
                decorator.AbortTask();
            }

            // 중단 처리하면 여기도 끝낸다.
            OnEnd();
        }

        // 삭제시 구독 해제 
        ~DecoratorNode()
        {
            
        }

        public override BTNode Clone()
        {
            DecoratorNode node = Instantiate(this);
            node.childNode = childNode.Clone();

            return node;
        }

    }
}
