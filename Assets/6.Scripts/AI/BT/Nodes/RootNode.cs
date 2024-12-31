using BT.CustomBTNodes;
using UnityEngine;

namespace BT.Nodes
{
    [System.Serializable]
    public class RootNode : BTNode
    {
 
        [SerializeField] private BTNode childNode;

        public BTNode ChildNode { get => childNode; set => childNode = value; }

        public RootNode(GameObject owner, SO_Blackboard blackboard, BTNode childNode)
        {
            this.nodeName = "Root";
            this.owner = owner;
            this.blackboard = blackboard;
            this.childNode = childNode;

            InitializeNode(childNode);
        }

        /// <summary>
        /// 루트 노드가 생성되면 자식 노드로부터 블랙보드를 전수한다.
        /// </summary>
        /// <param name="node"></param>
        private void InitializeNode(BTNode node)
        {
            if (node == null)
                return;

            // 노드들을 찾아서 해당 노드로 재귀
            if (node is CompositeNode compositeNode)
            {
                foreach (var child in compositeNode.Children)
                {
                    InitializeNode(child);
                }
            }
            //// 데코레이터라면 세팅하고 다시 데코레이터의 자식으로 세팅
            //else if (node is DecoratorNode decoratorNode)
            //{
            //    decoratorNode.SetOwnerObject(owner);
            //    Debug.Log($"Set!!! {decoratorNode.nodeName}");
            //    InitializeNode(decoratorNode.ChildNode);
            //}
            // 작업 노드라면 해당 노드에게 할당할 내용 전달
            else if (node is TaskNode TaskNode)
            {
                TaskNode.SetOwnerObject(owner);
                TaskNode.SetBlackboard(blackboard);
            }
        }

        /// <summary>
        /// 노드의 상태를 초기화 하는 함수 
        /// </summary>
        public void Reset()
        {
            //TODO: 기능 추가
        }

        public override void StopEvaluate()
        {
            childNode.StopEvaluate();
        }

        public override NodeState Evaluate()
        {
            return childNode.Evaluate();
        }

        public override BTNode Clone()
        {
            RootNode node = Instantiate(this);
            node.childNode = childNode.Clone();

            return node; 
        }

    }

}