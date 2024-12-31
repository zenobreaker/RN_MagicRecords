using BT;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using BT.Nodes;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class NodeView : UnityEditor.Experimental.GraphView.Node
{
    public Action<NodeView> OnNodeSelected; 
    public BTNode node;
    public Port input;
    public Port output;

    public NodeView(BTNode node) : base("Assets/Editor/NodeView.uxml")
    { 
        this.node = node;
        this.title = node.name;
        this.viewDataKey = node.guid; // guid를 키로 저장

        var titleLabel = this.Q<Label>("title"); // title 요소 가져오기
        if (titleLabel != null)
        {
            titleLabel.style.color = Color.white; // 텍스트 색상을 흰색으로 설정
        }

        // 노드 위치 세팅 
        style.left = node.position.x;
        style.top = node.position.y;

        CreateInputPorts();
        CreateOutputPorts();
        SetupClasses();

        Label descriptionLabel  = this.Q<Label>("description");
        descriptionLabel.bindingPath = "description";
        descriptionLabel.Bind(new SerializedObject(node));
    }

    // 노드 유형을 전환하는 메서드
    private void SetupClasses()
    {
        if (node is TaskNode)
        {
            AddToClassList("task");
        }
        else if (node is CompositeNode)
        {
            AddToClassList("composite");
        }
        else if (node is DecoratorNode)
        {
            AddToClassList("decorator");
        }
        else if (node is RootNode)
        {
            AddToClassList("root");
        }
    }

    // 각 노드 유형에 대한 포트 요구 사항이 약간 다르므로 어떤 유형의 노드인지 확인
    private void CreateInputPorts()
    {
        if(node is TaskNode)
        {
            input = InstantiatePort(Orientation.Vertical, Direction.Input, 
                Port.Capacity.Single, typeof(bool));
        }
        else if(node is CompositeNode)
        {
            input = InstantiatePort(Orientation.Vertical, Direction.Input,
                Port.Capacity.Single, typeof(bool));
        }
        else if(node is DecoratorNode)
        {
            input = InstantiatePort(Orientation.Vertical, Direction.Input,
                Port.Capacity.Single, typeof(bool));
        }
        else if (node is RootNode)
        {
          
        }

        if (input != null)
        {
            input.portName = "";
            input.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(input);
        }
    }

    private void CreateOutputPorts()
    {
        // 작업 노드(Action/Task)는 자식이 없어야 하는 노드이므로 
        if (node is TaskNode)
        {
         // 비어있게 둔다
        }
        // 컴포짓 노드는 여러 자식을 가질 수 있으므로 Mulit 
        else if (node is CompositeNode)
        {
            output = InstantiatePort(Orientation.Vertical, Direction.Output,
            Port.Capacity.Multi, typeof(bool));
        }
        // 데코레이터는 하나의 자식만 가질 수 있다. Single
        else if (node is DecoratorNode)
        {
            output = InstantiatePort(Orientation.Vertical, Direction.Output,
           Port.Capacity.Single, typeof(bool));
        }
        else if (node is RootNode)
        {
            output = InstantiatePort(Orientation.Vertical, Direction.Output,
           Port.Capacity.Single, typeof(bool));
        }

        if (output!= null)
        {
            output.portName = "";
            output.style.flexDirection = FlexDirection.ColumnReverse;
            outputContainer.Add(output);
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Undo.RecordObject(node, "Behavior Tree (Set Position)");
        node.position.x = newPos.xMin;
        node.position.y = newPos.yMin;
        EditorUtility.SetDirty(node);
    }

    // 인스펙터에 정보 표시하기
    public override void OnSelected()
    {
        base.OnSelected();
        OnNodeSelected?.Invoke(this);
    }

    // 수평에 따라 로직이 수행되도록 작업
    public void SortChildren()
    {
        CompositeNode composite = node as CompositeNode;
        if (composite != null)
        {
            composite.Children.Sort(SortByHorizontalPosition);
        }
    }

    private int SortByHorizontalPosition(BTNode left, BTNode right)
    {
        return left.position.x < right.position.x ? -1 : 1; 
    }

    public void UpdateState()
    {
        RemoveFromClassList("running");
        RemoveFromClassList("failure");
        RemoveFromClassList("success");

        if (Application.isPlaying)
        {
            switch (node.GetNodeState)
            {
                case BTNode.NodeState.Running:
                    //TODO: 이전 버전으로 구성했었기에 started 변수같은것으로 체크할 수 있다.
                    if(node.GetNodeState != BTNode.NodeState.Max)
                        AddToClassList("running");
                break;
                case BTNode.NodeState.Failure:
                AddToClassList("failure");
                break;
                case BTNode.NodeState.Success:
                AddToClassList("success");
                break;
            }
        }
    }
}
