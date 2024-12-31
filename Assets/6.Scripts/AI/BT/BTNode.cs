using UnityEngine;


namespace BT
{
    public abstract class BTNode : ScriptableObject
    {
        public enum NodeState { Running, Success, Failure, Abort, Max}
        protected NodeState nodeState = NodeState.Max;
        public NodeState GetNodeState { get => nodeState; }

        protected string nodeName;
        public string NodeName { get => nodeName; set => nodeName = value; }

        #region Editor
        public string guid;
        public Vector2 position;


        #endregion

        protected GameObject owner;
        [SerializeField] public SO_Blackboard blackboard;
        [TextArea] public string description;

        public virtual BTNode Clone()
        {
            return Instantiate(this);
        }

        public abstract NodeState Evaluate();

        public abstract void StopEvaluate();


        //TODO : 전체적으로 BT를 수정한다면 아래 변수와 함수들로 구성할 수 있을 것이다.
        //private bool started = false;
        //public NodeState Evaluate()
        //{
        //    if (!started)
        //    {
        //        OnBegin();  // 이렇게 추상 메서드를 호출할 수 있다. 
        //        started = true; 
        //    }

        //    nodeState = OnUpdate(); 

        //    if(nodeState == NodeState.Failure || nodeState == NodeState.Success)
        //    {
        //        OnEnd();
        //        started = false; 
        //    }
        //    return nodeState;
        //}

        //protected abstract void OnBegin();
        //protected abstract void OnEnd();
        //protected abstract NodeState OnUpdate();
    }
}