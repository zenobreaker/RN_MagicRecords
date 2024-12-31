using BT;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace BT.Nodes
{
    [System.Serializable]
    public abstract class CompositeNode : BTNode
    {
        [SerializeField] protected List<BTNode> children = new List<BTNode>();

        public List<BTNode> Children { get => children; }

        protected bool bRunning = false; 
        protected bool hasFirstStart = true; 

        public void AddChild(BTNode node)
        {
            children.Add(node);
        }

        public void RemoveChild(BTNode node)
        {
            children.Remove(node);
        }

        protected virtual void OnStart()
        {
            if(bRunning == false)
                bRunning = true;
        }
        
        protected virtual void OnEnd()
        {
            bRunning = false;
        }

        // 하위 노드들의 Abort 명령 실행
        public virtual void AbortTask()
        {
            if (bRunning == false)
                return;

            foreach(BTNode node in children)
            {
                if (node is TaskNode taskNode)
                    taskNode.AbortTask();
                else if (node is DecoratorNode decoratorNode)
                    decoratorNode.AbortTask();
                else if( node is CompositeNode compositeNode)
                    compositeNode.AbortTask();
            }
        }
        public override BTNode Clone()
        {
            CompositeNode node = Instantiate(this);
            node.children = children.ConvertAll(c => c.Clone());    

            return node;
        }
    }

}