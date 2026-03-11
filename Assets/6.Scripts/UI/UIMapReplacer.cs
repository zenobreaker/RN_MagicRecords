using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIMapReplacer : MonoBehaviour
{
    [SerializeField] private RectTransform Content;
    [SerializeField] private GameObject NodeContainer;
    [SerializeField] private GameObject NodeObject;

    [SerializeField] private GameObject LineContainer;
    [SerializeField] private CustomLine CustomLineObject;


    private MapReplacer mapReplacer;

    private List<MapNode> mapNodes = new List<MapNode>();
    private List<UIMapNode> uiMapNodes = new List<UIMapNode>();

    public void ReplaceUINode(MapReplacer mapReplacer)
    {
        this.mapReplacer = mapReplacer;

        ReplaceNodeObject();

        CalcFinalRectArea();

        DrawNodeLine();
    }


    private void ReplaceNodeObject()
    {
        if (NodeContainer == null || NodeObject == null) return;

        List<List<MapNode>> levels = mapReplacer.GetLevels();

        mapNodes.Clear();
        uiMapNodes.Clear(); 

        for (int level = 0; level < levels.Count; level++)
        {
            for (int n = 0; n < levels[level].Count; n++)
            {
                MapNode node = levels[level][n];

                GameObject nodeObject = Instantiate<GameObject>(NodeObject, NodeContainer.transform);
                
                // 위치 맞추기
                if (nodeObject.TryGetComponent<RectTransform>(out var rect))
                    rect.anchoredPosition = node.position;

                // UI에 현재 MapNode 정보 연결
                if (nodeObject.TryGetComponent<UIMapNode>(out var uiNode))
                {
                    uiNode.Init(node);
                    uiMapNodes.Add(uiNode);
                }

                mapNodes.Add(node);
            }
        }
    }

    // 영역 재계산
    private void CalcFinalRectArea()
    {
        if (Content == null || mapReplacer == null) return;

        // Content의 부모(일반적으로 ScrollRect의 Viewport)를 가져옵니다.
        RectTransform viewport = Content.parent as RectTransform;

        Canvas.ForceUpdateCanvases();

        // 뷰포트를 찾지 못했다면 기본값 세팅
        float viewportWidth = viewport != null ? viewport.rect.width : 800f;
        float viewportHeight = viewport != null ? viewport.rect.height : 600f;

        // 노드들이 차지하는 총 길이만 가져옵니다.
        float nodeHorizontal = mapReplacer.GetTotalHorizontalSpacing();
        float nodeVertical = mapReplacer.GetTotalVerticalSpacing();

        // [가로] 뷰포트 너비보다 노드들의 너비가 길면, 노드 너비만큼 Content를 늘립니다.
        float finalWidth = Mathf.Max(viewportWidth, nodeHorizontal);

        // [세로] 세로 스크롤을 막기 위해 Content의 세로 길이를 부모(Viewport)의 길이에 딱 맞춥니다.
        float finalHeight = Mathf.Max(viewportHeight, nodeVertical);

        // 크기 적용
        // 주의: Content의 Anchor가 Left-Stretch (Min 0,0 / Max 0,1)라면 sizeDelta.y를 0으로 줘야 할 수도 있습니다.
        // 현재 코드는 Anchor가 Center-Middle (Min 0.5,0.5 / Max 0.5,0.5) 이거나 Left-Top일 때 정상 작동합니다.
        Content.sizeDelta = new Vector2(finalWidth, finalHeight);
    }

    private void DrawNodeLine()
    {
        if (mapNodes.Count == 0 || CustomLineObject == null) return;

        float width = NodeObject.GetComponent<RectTransform>().sizeDelta.x;

        for (int i = 0; i < mapNodes.Count - 1; i++)
        {
            foreach (int id in mapNodes[i].nextNodeIds)
            {
                CustomLine cl = Instantiate<CustomLine>(CustomLineObject, LineContainer.transform);

                Vector2 a = new Vector2(mapNodes[i].position.x + width * 0.5f, mapNodes[i].position.y);
                Vector2 b = new Vector2(mapNodes[id].position.x + width * 0.5f, mapNodes[id].position.y);

                cl.DrawLine(a, b);
            }
        }
    }

    public void GetUIMapNodes(ref List<UIMapNode> uiMapNodes)
    {
        uiMapNodes = this.uiMapNodes;
    }
}
