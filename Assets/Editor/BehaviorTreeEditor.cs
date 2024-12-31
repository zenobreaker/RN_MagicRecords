using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using BT;
using UnityEditor.Callbacks;
using System;
using UnityEditor.Experimental.GraphView;

public class BehaviorTreeEditor : EditorWindow
{
    private BehaviorTreeView treeView;
    private InspectorView inspectorView;
    private IMGUIContainer blackboardView;

    private SerializedObject treeObject;
    private SerializedProperty blackboardProperty; 

    [MenuItem("BehaviorTreeEditor/Editor ...")]
    public static void OpenWindow()
    {
        BehaviorTreeEditor wnd = GetWindow<BehaviorTreeEditor>();
        wnd.titleContent = new GUIContent("BehaviorTreeEditor");
    }

    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceId, int line)
    {
        // 더블클릭하면 편집창을 열게한다.
        if(Selection.activeObject is BehaviorTree)
        {
            OpenWindow();
            return true; 
        }
        return false; 
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

       
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/BehaviorTreeEditor.uxml");
        visualTree.CloneTree(root);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/BehaviorTreeEditor.uss");
        root.styleSheets.Add(styleSheet);

        // 특정 유형을 쿼리로 찾아옴
        treeView = root.Q<BehaviorTreeView>();
        inspectorView = root.Q<InspectorView>();
        blackboardView = root.Q<IMGUIContainer>();
        blackboardView.onGUIHandler = () =>
        {
            if (treeObject != null)
            {
                treeObject.Update();
                if (blackboardProperty != null)
                {
                    EditorGUILayout.PropertyField(blackboardProperty);

                    SO_Blackboard blackboard = blackboardProperty.objectReferenceValue as SO_Blackboard;
                    if(blackboard != null)
                    {
                        var keys = blackboard.GetAllKeys();
                        foreach (var key in keys)
                        {
                            EditorGUILayout.LabelField("Key Name:", key.Key);
                            EditorGUILayout.LabelField("Key Type:", blackboard.GeteKeyType(key.Key).Name);
                            EditorGUILayout.LabelField("Current Value:", key.Value.GetValue()?.ToString() ?? "null");
                            EditorGUILayout.Space();
                        }
                    }
                }
                treeObject.ApplyModifiedProperties();   // 변경사항을 직렬화된 객체에 재적용
            }
        };

        treeView.OnNodeSelected = OnNodeSelectionChanged;
        OnSelectionChange();    // 수동으로 호출하여 생성 시 선택되게 설정
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange obj)
    {
        switch(obj)
        {
            case PlayModeStateChange.EnteredEditMode:
            OnSelectionChange();
            break;
            case PlayModeStateChange.ExitingEditMode: 
            break;
            case PlayModeStateChange.EnteredPlayMode:
            OnSelectionChange();
            break;
            case PlayModeStateChange.ExitingPlayMode: 
            break;
        }
    }


    private void OnSelectionChange() 
    {
        // 활성 개체가 BT 인 경우 
        BehaviorTree tree = Selection.activeObject as BehaviorTree;

        // 실행중인 Runner를 가져온다. 
        if (!tree)
        {
            if(Selection.activeGameObject)
            {
                BehaviorTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviorTreeRunner>();
                if(runner != null)
                {
                    tree = runner.tree;
                }
            }
        }

        if(Application.isPlaying)
        {
            if(tree)
            {
                treeView?.PopulateView(tree);
            }    
        }

        // 루트노드를 추가하려고 하면 직렬화할 수 없는 개체에 하위 개체를 
        // 추가할 수 없다는 버그를 발생한다. 그러므로 에디터를 열기전에 tree의 인스턴스ID를 가쟈온다.
        if(tree != null && AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID())) 
        {
            treeView?.PopulateView(tree);
        }

        // 직렬화 오브젝트 및 프로퍼티 생성
        if(tree != null)
        {
            treeObject = new SerializedObject(tree);
            blackboardProperty = treeObject?.FindProperty("blackboard");
        }
    }

    // 노드를 선택하면 발생하는 이벤트 
    private void OnNodeSelectionChanged(NodeView nodeView)
    {
        inspectorView.UpdateSelection(nodeView);
    }

    private void OnInspectorUpdate()
    {
        treeView?.UpdateNodeState();
    }
}