using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIMapReplacer : MonoBehaviour
{
    [SerializeField] private RectTransform Content;
    [SerializeField] private GameObject NodeContainer;
    [SerializeField] private GameObject NodeObject;

    [SerializeField] private GameObject LineContainer;
    [SerializeField] private CustomLine CustomLineObject;


    private MapReplacer mapReplacer;

    private List<MapNode> mapNodes = new List<MapNode>();

    private void Awake()
    {
        mapReplacer = new MapReplacer();
    }

    private void Start()
    {
        mapReplacer.Replace();
        mapReplacer.ConnectToNode();

        ReplaceNodeObject();

        CalcFinalRectArea();

        DrawNodeLine();
    }


    private void ReplaceNodeObject()
    {
        if (NodeContainer == null || NodeObject == null) return;

        List<List<MapNode>> levels = mapReplacer.GetLevels();
        mapNodes.Clear();

        for (int level = 0; level < levels.Count; level++)
        {
            for (int n = 0; n < levels[level].Count; n++)
            {
                MapNode node = levels[level][n];

                GameObject nodeObject = Instantiate<GameObject>(NodeObject, NodeContainer.transform);
                if (nodeObject.TryGetComponent<RectTransform>(out var rect))
                    rect.anchoredPosition = node.position;

                mapNodes.Add(node);
            }
        }
    }

    private void CalcFinalRectArea()
    {
        if (Content == null || mapReplacer == null) return;

        float nodeHorizontal = mapReplacer.GetTotalHorizontalSpacing();
        float nodeVertical = mapReplacer.GetTotalVerticalSpacing();

        float width = Screen.width;
        float height = Screen.height;

        width = Mathf.Max(width, nodeHorizontal);
        height = Mathf.Max(height, nodeVertical);

        Content.sizeDelta = new Vector2(width, height);
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

}
