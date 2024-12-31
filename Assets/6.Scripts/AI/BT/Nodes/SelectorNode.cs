using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BT.Nodes
{
    // SelectorNode 는 실패하면 다음 자식을 확인한다. 
    public class SelectorNode : CompositeNode
    {
        public SelectorNode()
        {
            nodeName = "Selector";
        }

        private int currentRunningNodeIndex = -1;  // 현재 실행 중인 자식 추적하기 위한 변수

        public override NodeState Evaluate()
        {
            OnStart();

            if (currentRunningNodeIndex != -1)
            {
                // 이전에 실행 중이던 노드 평가하기
                NodeState result = children[currentRunningNodeIndex].Evaluate();
                //Debug.Log($"Selector = Previous Node Evaluate {currentRunningNodeIndex} /" +
                //    $"{children[currentRunningNodeIndex].NodeName}"); 
                if (result == NodeState.Running)
                {
                    return nodeState = NodeState.Running;
                }
                else if (result == NodeState.Success)
                {
                 //   Debug.Log($"Selector = Previous Node Evaluate Sucess {currentRunningNodeIndex} /" +
                    //$"{children[currentRunningNodeIndex].NodeName}");
                    currentRunningNodeIndex = -1;
                    OnEnd();
                    return nodeState = NodeState.Success;
                }
                else if (result == NodeState.Abort)
                {
                    //Debug.Log($"Selector = Previous Node Evaluate Sucess {currentRunningNodeIndex} /" +
                    //$"{children[currentRunningNodeIndex].NodeName}");
                    //TODO: 중단시 처음부터 자식 노드들을 검사시킬지 의문이다.
                    currentRunningNodeIndex = -1;
                    OnEnd();
                    return nodeState = NodeState.Abort;
                }
            }

            //Debug.Log($"Next Node Evaluate {currentRunningNodeIndex} /");
                    
            for (int i = currentRunningNodeIndex + 1; i < children.Count; i++)
            {
                currentRunningNodeIndex = i;
                NodeState result = children[i].Evaluate();

                switch (result)
                {
                    case NodeState.Running:
                    return nodeState = NodeState.Running;
                    
                    case NodeState.Success:
                    currentRunningNodeIndex = -1;
                    OnEnd();
                    return nodeState = NodeState.Success;

                    case NodeState.Abort:
                    currentRunningNodeIndex = -1;
                    OnEnd();
                    return nodeState = NodeState.Abort;
                }
            }

            currentRunningNodeIndex = -1;
            OnEnd();
            return nodeState = NodeState.Failure;
        }

        public override void AbortTask()
        {
            if (bRunning == false)
                return;

            base.AbortTask();

            currentRunningNodeIndex = -1;
        }

        public override void StopEvaluate()
        {
            foreach (var child in children)
            {
                child.StopEvaluate();
            }

            currentRunningNodeIndex = -1;
        }

    }
}
