using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

public class InspectorView : VisualElement
{
    public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits> { }

    private Editor editor;

    public InspectorView() 
    { 

    }

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();  // 에디터 화면 초기화

        // editor를 생성할 때마다 해당 editor를 지워야한다.
        UnityEngine.Object.DestroyImmediate(editor);
        editor = Editor.CreateEditor(nodeView.node);
        // editor 정보를 담을 컨테이너 
        IMGUIContainer container = new IMGUIContainer(() => 
        {
            if (editor.target)
            {
                editor.OnInspectorGUI();
            }
        });
        Add(container);
    }
}
