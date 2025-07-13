using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapNode
{
    public int id;
    public int level;
    public Vector2 position;
    public List<int> nextNodeIds = new List<int>(); // 연결된 다음 노드들
}



public class MapReplacer
{
    private List<List<MapNode>> levels = new List<List<MapNode>>();

    public List<List<MapNode>> GetLevels()
    {
        return levels;
    }

    private int nodeIdCounter = 0;
    private int maxLevel = 5;
    private int maxNodesPreLevel = 3;

    private float horizontalSpacing = 200f;
    private float verticalSpacing = 150f;

    public float GetTotalHorizontalSpacing() => horizontalSpacing * maxLevel;


    public float GetTotalVerticalSpacing() => verticalSpacing * maxNodesPreLevel;

    public void Replace()
    {
        // 배치
        for (int level = 0; level < maxLevel; level++)
        {
            int nodeCount = 1;

            if (level == 0 || level == maxLevel - 1)
                nodeCount = 1; // 시작/끝 노드는 하나만
            else
                nodeCount = UnityEngine.Random.Range(2, maxNodesPreLevel + 1);

            List<MapNode> currentLevel = new List<MapNode>();

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode node = new MapNode();
                node.id = nodeIdCounter++;
                node.level = level;

                // 가운데 정렬되도록 y축 계산 
                float totalHeight = (nodeCount - 1) * verticalSpacing;
                float y = -(i * verticalSpacing - totalHeight / 2);

                float x = level * horizontalSpacing;

                node.position = new Vector2(x, y);
                currentLevel.Add(node);
            }

            levels.Add(currentLevel);
        }

    }

    // 노드 연결 
    public void ConnectToNode()
    {
        for (int level = 0; level < maxLevel - 1; level++)
        {
            List<MapNode> currentLevel = levels[level];
            List<MapNode> nextLevel = levels[level + 1];

            foreach (MapNode node in currentLevel)
            {
                int connectCount = UnityEngine.Random.Range(1, 3);

                var shuffledNext = nextLevel.OrderBy(_ => UnityEngine.Random.value).ToList();

                for (int i = 0; i < Mathf.Min(connectCount, shuffledNext.Count); i++)
                {
                    node.nextNodeIds.Add(shuffledNext[i].id);
                }
            }
        }
    }
}
