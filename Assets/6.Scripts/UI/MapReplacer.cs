using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class MapNode
{
    public int id;
    public int level;
    public Vector2 position;
    public List<int> nextNodeIds = new List<int>(); // 연결된 다음 노드들

    public int stageID;
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

    private float widthMargin = 100;

    public float GetTotalHorizontalSpacing() => horizontalSpacing * maxLevel;

    public float GetTotalVerticalSpacing() => verticalSpacing * maxNodesPreLevel;

    public void Replace(float width = 0.0f, float height = 0.0f)
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

                // 왼쪽에서 오른쪽으로 배치되는 기준
                // 가운데 정렬되도록 y축 계산 
                float totalHeight = (nodeCount - 1) * verticalSpacing;
                float y = -(i * verticalSpacing - totalHeight / 2);

                float x = widthMargin  + level * horizontalSpacing;

                node.position = new Vector2(x, y);
                currentLevel.Add(node);
            }
            levels.Add(currentLevel);
        }//for(level)
    }

    // 노드 연결 
    public void ConnectToNode()
    {
        for (int level = 0; level < maxLevel - 1; level++)
        {
            List<MapNode> currentLevel = levels[level];
            List<MapNode> nextLevel = levels[level + 1];

            // 첫 번째와 마지막 노드 전 노드는 무조건 다음과 연결해야함
            if (level == 0 || level == maxLevel - 1)
            {
                foreach (MapNode node in currentLevel)
                {
                    for (int i = 0; i < nextLevel.Count; i++)
                    {
                        node.nextNodeIds.Add(nextLevel[i].id);
                    }
                }
                continue;
            }

            // 그 사이 노드들은 무조건 하나는 연결한다.
            // 추가 연결선은 확률로 결정 
            foreach (MapNode node in currentLevel)
            {
                int connectCount = UnityEngine.Random.Range(1, 4);

                var shuffledNext = nextLevel.OrderBy(_ => UnityEngine.Random.value).ToList();

                for (int i = 0; i < Mathf.Min(connectCount, shuffledNext.Count); i++)
                {
                    node.nextNodeIds.Add(shuffledNext[i].id);
                }
            }

            // 2단계: 연결되지 않은 nextLevel의 노드들을 찾아서 보완 연결
            HashSet<int> connectedNodeIds = new HashSet<int>();

            // 현재 레벨 노드들이 연결한 모든 다음 노드 id 수집
            foreach (MapNode node in currentLevel)
            {
                foreach (int nextId in node.nextNodeIds)
                {
                    connectedNodeIds.Add(nextId);
                }
            }

            // 다음 레벨 중 연결되지 않은 노드 찾아서 강제로 하나 연결
            foreach (MapNode orphanNode in nextLevel)
            {
                if (!connectedNodeIds.Contains(orphanNode.id))
                {
                    // 랜덤한 이전 레벨 노드를 골라서 연결
                    MapNode randomParent = currentLevel[UnityEngine.Random.Range(0, currentLevel.Count)];
                    randomParent.nextNodeIds.Add(orphanNode.id);
                }
            }

        }//for(level)
    }
}
