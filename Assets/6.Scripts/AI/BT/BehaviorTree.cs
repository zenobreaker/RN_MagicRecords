using BT.Nodes;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using System;

namespace BT
{
    [CreateAssetMenu()]
    public class BehaviorTree : ScriptableObject
    {
        [SerializeField] public SO_Blackboard blackboard;

        public BTNode rootNode;
        public BTNode.NodeState state = BTNode.NodeState.Running;
        public List<BTNode> nodes = new List<BTNode>();


        public void CreateBlackboardKey()
        {

        }
        public RootNode CreateBehaviorTree()
        {
            return null; 
        }

        public void StopEvaluate()
        {
            rootNode?.StopEvaluate();
        }

        public void Evaluate()
        {
            rootNode?.Evaluate();
        }

        public BTNode CreateBTNode(System.Type type)
        {
            BTNode node = ScriptableObject.CreateInstance(type) as BTNode;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            
            Undo.RecordObject(this, "Behavior Tree (CreateBTNode)");
            nodes.Add(node);

            // 게임 실행 중에 생성하지 못하도록 
            if(Application.isPlaying == false)
            {
                AssetDatabase.AddObjectToAsset(node, this);
            }

            Undo.RegisterCreatedObjectUndo(node, "Behavior Tree (CreateBTNode)");
            AssetDatabase.SaveAssets();
            return node; 
        }

        public void DeleteBTNode(BTNode node)
        {
            Undo.RecordObject(this, "Behavior Tree (DeleteBTNode)");
            nodes.Remove(node);
            
            //AssetDatabase.RemoveObjectFromAsset(node);
            Undo.DestroyObjectImmediate(node);
            AssetDatabase.SaveAssets();
        }

        // 노드에 자식으로 추가하는 작업 
        public void AddChild(BTNode parent, BTNode child)
        {
            DecoratorNode decorator = parent as DecoratorNode;
            if(decorator != null)
            {
                Undo.RecordObject(decorator, "Behavior Tree (AddChild)");
                decorator.ChildNode = child;
                EditorUtility.SetDirty(decorator);
            }

            RootNode root = parent as RootNode;
            if (root != null)
            {
                Undo.RecordObject(root, "Behavior Tree (AddChild)");
                root.ChildNode = child;
                EditorUtility.SetDirty(root);
            }

            CompositeNode composite = parent as CompositeNode;
            if (composite != null)
            {
                Undo.RecordObject(composite, "Behavior Tree (AddChild)");
                composite.AddChild(child);
                EditorUtility.SetDirty(composite);
            }
        }
        // 노드의 자식 제거 
        public void RemoveChild(BTNode parent, BTNode child)
        {
            DecoratorNode decorator = parent as DecoratorNode;
            if (decorator != null)
            {
                Undo.RecordObject(decorator, "Behavior Tree (RemoveChild)");
                decorator.ChildNode = null;
                EditorUtility.SetDirty(decorator);
            }

            RootNode root = parent as RootNode;
            if (root != null)
            {
                Undo.RecordObject(root, "Behavior Tree (RemoveChild)");
                root.ChildNode = null;
                EditorUtility.SetDirty(root);
            }

            CompositeNode composite = parent as CompositeNode;
            if (composite != null)
            {
                Undo.RecordObject(composite, "Behavior Tree (RemoveChild)");
                composite.RemoveChild(child);
                EditorUtility.SetDirty(composite);
            }
        }

        public List<BTNode> GetChildren(BTNode parent)
        {
            List<BTNode> childern = new List<BTNode>();

            DecoratorNode decorator = parent as DecoratorNode;
            if (decorator != null && decorator.ChildNode != null)
            {
                childern.Add(decorator.ChildNode);
            }

            RootNode root = parent as RootNode;
            if (root != null && root.ChildNode != null)
            {
                childern.Add(root.ChildNode);
            }

            CompositeNode composite = parent as CompositeNode;
            if (composite != null)
            {
                return composite.Children;
            }

            return childern;
        }

        public void Traverse(BTNode node, Action<BTNode> visiter)
        {
            if(node != null)
            {
                visiter?.Invoke(node);
                var children = GetChildren(node);
                children.ForEach((n)=>Traverse(n, visiter));
            }
        }

        public BehaviorTree Clone()
        {
            BehaviorTree tree = Instantiate(this);
            tree.rootNode = tree.rootNode.Clone();
            tree.nodes = new List<BTNode>();

            Traverse(tree.rootNode, (n) =>
            {
                tree.nodes.Add(n);
            });

            return tree; 
        }

        //TODO: Bind에 전달할 정보가 추가되면 node와 그와 관련된 내용을 수정해야 한다.
        public void Bind()
        {
            Traverse(this.rootNode, (node) =>
            {
                node.blackboard = blackboard;
            });
        }
    }
}
