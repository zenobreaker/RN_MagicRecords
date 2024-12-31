using BT.Helpers;
using BT.Nodes;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace BT
{
    public class BehaviorTreeRunner : MonoBehaviour
    {
        public BehaviorTree tree; 
        
        private Coroutine btRunCoroutine;

        private void Start()
        {
            tree = tree.Clone();
            tree.Bind(); 
            RunBehaviorTree(0.01f);
        }

      

        public void RunBehaviorTree(float interval = -1.0f, bool debugMode = false)
        {
            if (btRunCoroutine == null)
            {
                Debug.Log("Run Behaivor !!");
                btRunCoroutine = StartCoroutine(StartBTRunCoroutine(debugMode, interval));
            }
        }

        public void StopBehaviorTree()
        {
            if(btRunCoroutine != null)
                StopCoroutine(btRunCoroutine);
            
            StopEvaluate();
            CoroutineHelper.Instance.StopAllCoroutines();
        }
        public void OperateNode()
        {
            tree.Evaluate();
        }
        public void OperateNode(bool debugMode)
        {
            OperateNode();
        }

        public void StopEvaluate()
        {
            tree.StopEvaluate();
        }

        public IEnumerator StartBTRunCoroutine(bool debugMode = false, float interval = -1.0f)
        {
            while(true)
            {
                if(interval > 0.0f)
                    yield return new WaitForSeconds(interval);

                OperateNode(debugMode);

                yield return null;
            }
        }
    }

}