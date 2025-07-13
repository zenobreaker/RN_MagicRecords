using System.Collections.Generic;
using UnityEngine;

public class UIMapReplacer : MonoBehaviour
{
    [SerializeField] private RectTransform Content;
    [SerializeField] private GameObject NodeContainer;
    [SerializeField] private GameObject NodeObject;


    private MapReplacer mapReplacer;

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
    }


    private void ReplaceNodeObject()
    {
        if (NodeContainer == null || NodeObject == null) return;

        List<List<MapNode>> levels = mapReplacer.GetLevels();

        for (int level = 0; level < levels.Count; level++)
        {
            for (int n = 0; n < levels[level].Count; n++)
            {
                MapNode node = levels[level][n];

                GameObject nodeObject = Instantiate<GameObject>(NodeObject, NodeContainer.transform);
                if(nodeObject.TryGetComponent<RectTransform>(out var rect))
                    rect.anchoredPosition = node.position;
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
}
