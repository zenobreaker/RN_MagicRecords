// 💡 UI와 맵 생성을 위한 가벼운 "봉투(Envelope)" 클래스
[System.Serializable]
public class MapNodeInfo
{
    public int nodeId;         // 맵 상에서의 고유 번호 (0, 1, 2...)
    public StageType type;     // 이 노드의 정체 (Combat, Event, None 등)
    public int contentId;      // 알맹이의 ID (StageDB의 ID 이거나 EventDB의 ID)
    public string biome;       // 테마 (Forest 등)
    public int mapIndex;       // 맵 프리팹 인덱스 (-1이면 전용 맵)
    
    public bool isCleared;
    public int clearRewardId;
    public MapNodeInfo Copy()
    {
        return new MapNodeInfo
        {
            nodeId = this.nodeId,
            type = this.type,
            contentId = this.contentId,
            biome = this.biome,
            mapIndex = this.mapIndex,
            isCleared = this.isCleared,
            clearRewardId = this.clearRewardId
        };
    }

    public override string ToString()
    {
        return $"Node[{nodeId}] Type:{type} ContentID:{contentId} Biome:{biome}";
    }
}