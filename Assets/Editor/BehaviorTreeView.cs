using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using BT;
using System;
using BT.Nodes;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Linq;

public class BehaviorTreeView : GraphView
{
    public Action<NodeView> OnNodeSelected;
    public new class UxmlFactory : UxmlFactory<BehaviorTreeView, GraphView.UxmlTraits> { }
    private BehaviorTree tree;

    public BehaviorTreeView()
    {
        // 배경 그리드 추가 
        //GridBackground grid = new GridBackground();
        Insert(0, new GridBackground());
        //grid.StretchToParentSize();

        // 줌 및 이동 지원 
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/BehaviorTreeEditor.uss");
        styleSheets.Add(styleSheet);

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        if (tree == null) // tree가 null이면 다시 로드 시도
        {
            Debug.LogWarning("Tree reference lost. Attempting to reload...");
            tree = Selection.activeObject as BehaviorTree; // 현재 선택된 BehaviorTree 에셋 다시 로드
            if (tree == null)
            {
                Debug.LogError("Failed to reload the BehaviorTree. Please re-select the tree.");
                return;
            }
        }

        PopulateView(tree);
        AssetDatabase.SaveAssets();
    }

    // 노드뷰를 찾는 기능 Guid로 찾아내여 처리한다. 
    NodeView FindNodeView(BTNode node)
    {
        return GetNodeByGuid(node.guid) as NodeView; 
    }

    internal void PopulateView(BehaviorTree tree)
    {
        this.tree = tree;

        // 이전에 생성한 모든 이벤트를 무시하고 지운다.
        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements); // 생성된 일종의 항목 지우기 
        graphViewChanged += OnGraphViewChanged; // 재구독

        // 루트노드가 없는 트리라면 루트를 만든다.
        if(tree.rootNode == null)
        {
            tree.rootNode = tree.CreateBTNode(typeof(RootNode)) as RootNode;
            EditorUtility.SetDirty(tree); // 트리 정보를 전달
            AssetDatabase.SaveAssets();
        }

        // Create node view => 노드를 생성하는 단계
        tree.nodes.ForEach(n => CreateNodeView(n));

        // Create edges view => 에지를 생성하는 단계
        tree.nodes.ForEach(n =>
        {
            var children = tree.GetChildren(n);
            children.ForEach(c =>
            {
                // 부모노드뷰와 자식노드뷰를 찾아 엣지를 연결하고 그 연결요소를 
                // 그래프뷰에 추가한다. 
                NodeView parentView = FindNodeView(n);
                NodeView childView = FindNodeView(c);

                Edge edge = parentView.output.ConnectTo(childView.input);
                AddElement(edge);
            });
        });
    }

    // 호환 포트 가져온다. => 노드 끼리 연결되게 하기 위함
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.ToList().Where(endPort =>
        endPort.direction != startPort.direction &&
        endPort.node != startPort.node).ToList();
    }

    // 그래프 변화가 발생하면 호출된다.
    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        // 삭제 기능 
        if(graphViewChange.elementsToRemove != null)
        {
            graphViewChange.elementsToRemove.ForEach(elem =>
            {
                // 노드 지웠을 때 
                NodeView nodeView = elem as NodeView; 
                if(nodeView != null)
                {
                    tree.DeleteBTNode(nodeView.node);
                }

                // 엣지 지웠을 때 
                Edge edge = elem as Edge;
                if (edge != null)
                {
                    NodeView parentView = edge.output.node as NodeView;
                    NodeView childView = edge.input.node as NodeView;
                    tree.RemoveChild(parentView.node, childView.node);
                }
            });
        }


        // 연결 기능 
        if (graphViewChange.edgesToCreate != null)
        {
            // 각 엣지에 대해서 다음 노드 정보를 가져온다.
            graphViewChange.edgesToCreate.ForEach(edge =>
            {
                // 대상 노드뷰에서 정보 가져오기 
                NodeView parentView = edge.output.node as NodeView;
                NodeView childView = edge.input.node as NodeView;
                // 각 뷰에서 가져온 노드 정보를 tree에 전달하여 처리한다. 
                tree.AddChild(parentView.node, childView.node);
            });
        }

        // 배치에 따른 로직 변화
        if(graphViewChange.movedElements != null)
        {
            nodes.ForEach((n) =>
            {
                NodeView view = n as NodeView;
                view.SortChildren();
            });
        }

        return graphViewChange;
    }

    // 콘텐트 메뉴 추가하기 (마우스 오른쪽 메뉴)
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        //base.BuildContextualMenu(evt);
        {
            var types = TypeCache.GetTypesDerivedFrom<TaskNode>();
            foreach(var type in types)
            {
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a)=> CreateNode(type));
            }
        }

        {
            var types = TypeCache.GetTypesDerivedFrom<CompositeNode>();
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type));
            }
        }

        {
            var types = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
            foreach (var type in types)
            {
                evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type));
            }
        }
    }

    private void CreateNode(System.Type type)
    {
        if(tree == null)
        {
            Debug.LogError("Tree is Null");

            return; 
        }

        BTNode node = tree.CreateBTNode(type);
        CreateNodeView(node);
    }

    // 노드 View 생성 
    private void CreateNodeView(BTNode node)
    {
        NodeView nodeView = new NodeView(node);
        nodeView.OnNodeSelected = OnNodeSelected;
        AddElement(nodeView);
    }

    // 노드 상태 확인
    public void UpdateNodeState()
    {
        nodes.ForEach(n =>
        {
            NodeView view = n as NodeView;
            view.UpdateState();
        });
    }
}