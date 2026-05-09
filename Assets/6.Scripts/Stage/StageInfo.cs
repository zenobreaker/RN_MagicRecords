using System.Collections.Generic;


[System.Serializable]
public class StageInfo
{
    public int id;
    public int chapter;
    public StageType type;
    public string biome;

    // 등장할 적 
    public List<int> groupIds = new List<int>();

    // 등장할 보상
    public int clearRewardId;

    public int wave = 0;

    public int mapIndex;

    public StageInfo(StageInfo other)
    {
        id = other.id;
        chapter = other.chapter;
        type = other.type;
        groupIds = new List<int>(other.groupIds);
        clearRewardId = other.clearRewardId;
        wave = other.wave;
    }

    public StageInfo() { }

    public StageInfo Copy()
    {
        return new StageInfo()
        {
            id = this.id,
            biome = this.biome,
            mapIndex = this.mapIndex,
            type = this.type,
            groupIds = new List<int>(groupIds),
            clearRewardId = clearRewardId,
            wave = wave,
        };
    }

    public override string ToString()
    {
        return chapter.ToString() + "-" + id.ToString();
    }
}

