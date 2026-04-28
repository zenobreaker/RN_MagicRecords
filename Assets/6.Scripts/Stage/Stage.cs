using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEditor;

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

    public bool bIsCleared = false;
    public bool bIsOpened = false;


    public StageInfo(StageInfo other)
    {
        id = other.id;
        chapter = other.chapter;
        type = other.type; 
        groupIds = new List<int>(other.groupIds);
        clearRewardId = other.clearRewardId;
        wave = other.wave;
        bIsCleared = other.bIsCleared;
        bIsOpened = other.bIsOpened;
    }

    public StageInfo() { }

    public override string ToString()
    {
        return chapter.ToString() + "-" + id.ToString();
    }
}

