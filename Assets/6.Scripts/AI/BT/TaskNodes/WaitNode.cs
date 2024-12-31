using BT.Nodes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BT.TaskNodes
{
    public class WaitNode : TaskNode
    {
        [SerializeField] private float waitTime = 1.0f;
        public float WaitTime { set => waitTime = value; }
        [SerializeField] private float waitRandDelay = 0;
        public float WaitRandDelay { set => waitRandDelay = value; }

        private float currentWaitTime;
        public float CurrentWaitTime { get => currentWaitTime; }

        private float lastTime; 
        public WaitNode()
            : base(null, null, null)
        {
            onBegin = OnBegin;
            onUpdate = OnUpdate;
            onEnd = OnEnd;
        }

        public WaitNode(float waitTime, float waitRandDelay = 0.0f)
            : base(null, null, null)
        {
            nodeName = "Wait";

            this.waitTime = waitTime;
            this.waitRandDelay = waitRandDelay;

            onBegin = OnBegin;
            onUpdate = OnUpdate;
            onEnd = OnEnd;
        }



        protected override BTNode.NodeState OnBegin()
        {
            //Debug.Log("Wait Node Begin");
            currentWaitTime = Random.Range(waitTime + (-1.0f * waitRandDelay),
                waitTime + (+1.0f * waitRandDelay));
            lastTime = Time.time; 
            return NodeState.Running;
        }


        protected override BTNode.NodeState OnUpdate()
        {
            //currentWaitTime = Time.time;

            //Debug.Log($"Time : {Time.time - lastTime} / {currentWaitTime} ");
            if (Time.time - lastTime < currentWaitTime)
            {
                return BTNode.NodeState.Running;
            }

            return NodeState.Success;
        }

    }
}