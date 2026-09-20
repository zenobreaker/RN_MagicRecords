using System;
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
}

public class NodeReplacer
{
    private List<List<MapNode>> levels = new List<List<MapNode>>();

    public void ClearMap()
    {
        levels.Clear();
    }

    public List<List<MapNode>> GetLevels()
    {
        return levels;
    }

    private int nodeIdCounter = 0;
    private int maxLevel = 6;
    private int maxBranchCount = 3;
    private int finalNodeId = -1;

    private float horizontalSpacing = 350f;
    private float verticalSpacing = 175f;

    private float widthMargin = 100;

    private float maxNodePosX = 0.0f;
    private float maxNodePosY = 0.0f;

    /// <summary>
    ///  배치되는 레벨의 한계 설정
    /// </summary>
    /// <param name="level"></param>
    public void SetMaxNodeLevel(int level)
    {
        maxLevel = Mathf.Max(3, level);
    }

    /// <summary>
    /// 갈래 길에 배치되는 노드의 최대 개수 설정 
    /// </summary>
    /// <param name="count"></param>
    public void SetMaxBranchCount(int count)
    { maxBranchCount = Mathf.Clamp(count, 2, 3); }



    public float GetTotalHorizontalSpacing()
    {
        // 가장 멀리 있는 노드의 X좌표에 우측 여백(시작 여백과 동일하게)을 더해줍니다.
        // 필요하다면 노드 자체의 절반 너비(NodeWidth/2)를 더 추가하셔도 좋습니다.
        return maxNodePosX + widthMargin;
    }
    public float GetTotalVerticalSpacing()
    {
        // 세로 스크롤을 막으셨다면 당장 안 쓰이겠지만, 
        // 공식의 완전성을 위해 위아래 여백을 줘서 반환합니다.
        return (maxNodePosY * 2) + verticalSpacing;
    }

    public bool IsFinalNode(int nodeId)
    {
        return nodeId == finalNodeId;
    }

    public void GenerateNodeMap(float width = 0.0f, float height = 0.0f)
    {
        Replace(width, height);
        ConnectToNode();
        if (!ValidateShopConnections())
            throw new InvalidOperationException("Invalid pre-boss shop connections.");
    }

    public void Replace(float width = 0.0f, float height = 0.0f)
    {
        levels.Clear();
        nodeIdCounter = 0;
        maxNodePosX = maxNodePosY = 0;

        // 배치
        for (int level = 0; level < maxLevel; level++)
        {
            int nodeCount = 1;

            if (level == 0 || level >= maxLevel - 2)
                nodeCount = 1; // 시작/끝 노드는 하나만
            else
                nodeCount = UnityEngine.Random.Range(2, maxBranchCount + 1);

            List<MapNode> currentLevel = new List<MapNode>();

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode node = new MapNode();
                node.id = nodeIdCounter++;
                node.level = level;

                // 마지막 노드 캐싱
                if (level == maxLevel - 1)
                    finalNodeId = node.id;

                // 왼쪽에서 오른쪽으로 배치되는 기준
                // 가운데 정렬되도록 y축 계산 
                float totalHeight = (nodeCount - 1) * verticalSpacing;
                float y = -(i * verticalSpacing - totalHeight / 2);
                maxNodePosY = Mathf.Max(y, maxNodePosY);

                float x = widthMargin + level * horizontalSpacing;
                maxNodePosX = Mathf.Max(x, maxNodePosX);

                node.position = new Vector2(x, y);
                currentLevel.Add(node);
            }
            levels.Add(currentLevel);
        }//for(level)
    }

    // 노드 연결 
    public void ConnectToNode()
    {
        foreach (var node in levels.SelectMany(row => row)) node.nextNodeIds.Clear();
        for (int level = 0; level < levels.Count - 1; level++)
        {
            List<MapNode> currentLevel = levels[level];
            List<MapNode> nextLevel = levels[level + 1];

            // 첫 번째와 마지막 노드 전 노드는 무조건 다음과 연결해야함
            if (level == 0 || nextLevel.Count == 1)
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
                    if (!randomParent.nextNodeIds.Contains(orphanNode.id))
                        randomParent.nextNodeIds.Add(orphanNode.id);
                }
            }

        }//for(level)
    }

    public List<int> GetCanEnableNodeIds(int currentID)
    {
        List<int> results = new();
        if (levels.Count <= 0)
            return null;

        for (int level = 0; level < maxLevel; level++)
        {
            for (int node = 0; node < levels[level].Count; node++)
            {
                if (levels[level][node].id == currentID)
                    results.AddRange(levels[level][node].nextNodeIds);
            }
        }

        return results;
    }

    public bool CanEnableNode(int currentId, int targetId)
    {
        var list = GetCanEnableNodeIds(currentId);
        return list != null && list.Contains(targetId);
    }

    public bool ValidateShopConnections()
    {
        if (levels.Count < 3 || levels.Any(row => row.Count == 0 || row.Count > 3)) return false;
        var shopRow = levels[levels.Count - 2];
        var bossRow = levels[levels.Count - 1];
        if (shopRow.Count != 1 || bossRow.Count != 1) return false;
        var shop = shopRow[0]; var boss = bossRow[0];
        if (!shop.nextNodeIds.SequenceEqual(new[] { boss.id }) || boss.nextNodeIds.Count != 0) return false;
        if (levels[levels.Count - 3].Any(n => !n.nextNodeIds.SequenceEqual(new[] { shop.id }))) return false;
        var nodes = levels.SelectMany(row => row).ToDictionary(n => n.id);
        return nodes.Values.All(n => n.nextNodeIds.All(id => nodes.TryGetValue(id, out var next) &&
            next.level == n.level + 1 && (id != boss.id || n.id == shop.id)));
    }

    public void RestoreMap(List<MapNode> savedNodes)
    {
        levels.Clear();
        if (savedNodes == null || savedNodes.Count == 0) { finalNodeId = -1; return; }
        maxLevel = savedNodes.Max(x => x.level) + 1;

        for (int i = 0; i < maxLevel; i++)
            levels.Add(new List<MapNode>());

        // 초기화
        maxNodePosX = 0.0f;
        maxNodePosY = 0.0f;

        bool isConnected = false;
        foreach (var node in savedNodes)
        {
            levels[node.level].Add(node);
            if (node.nextNodeIds.Count > 0)
                isConnected = true;
            maxNodePosX = Mathf.Max(maxNodePosX, node.position.x);
            maxNodePosY = Mathf.Max(maxNodePosY, Mathf.Abs(node.position.y));
        }

        finalNodeId = levels[levels.Count - 1][0].id;

        if (isConnected == false)
            ConnectToNode();
    }

    // Upgrade legacy maps without changing any existing node IDs or visits.
    public MapNode InsertShopBeforeBoss()
    {
        if (levels.Count < 2) return null;
        var bossRow = levels[levels.Count - 1];
        var shop = new MapNode { id = levels.SelectMany(r => r).Max(n => n.id) + 1,
            level = levels.Count - 1, position = bossRow[0].position };
        foreach (var node in levels.SelectMany(r => r))
            node.nextNodeIds.RemoveAll(id => bossRow.Any(b => b.id == id));
        foreach (var node in levels[levels.Count - 2])
        { node.nextNodeIds.Clear(); node.nextNodeIds.Add(shop.id); }
        foreach (var boss in bossRow)
        { boss.level++; boss.position.x += horizontalSpacing; shop.nextNodeIds.Add(boss.id); }
        levels.Insert(levels.Count - 1, new List<MapNode> { shop });
        maxLevel = levels.Count;
        maxNodePosX += horizontalSpacing;
        return shop;
    }

    public int GetNodeLevel(int nodeId)
    {
        for (int level = 0; level < maxLevel; level++)
        {
            for (int node = 0; node < levels[level].Count; node++)
            {
                if (levels[level][node].id == nodeId)
                    return level;
            }
        }
        return -1; // 못 찾았을 경우
    }
}
