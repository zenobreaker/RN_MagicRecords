using System.Collections.Generic;
using System.IO;
using UnityEngine;


[System.Serializable]
public class MapData
{
    public List<MapNode> nodes = new();
    public int currentStageId;
}

[System.Serializable]
public class MapToStage
{
    public int mapNodeId;
    public int stageId;
}

[System.Serializable]
public class StageNodeData
{
    public List<MapToStage> stages = new();
}


[System.Serializable]
public class AccountInfoData
{
    public int accountId;
    public string accountName;
    public int accountLevel;
    public float accountExp;
    public List<int> charIds = new();
}

[System.Serializable]
public class CharacterSaveData
{
    public int charId;
    public int charLevel;
    public int classID; 

    public List<string> equippedItemIds = new();
    public List<int> equippedSkillIds = new();
}

[System.Serializable]
public class CharInfoListData
{
    public List<CharacterSaveData> charInfoList = new();
}

[System.Serializable]
public class ItemInfoSaveData
{
    public int itemId;
    public string uniqueId;
    public ItemCategory itemCategoy;
    public int itemCount;
    public int enhanceLevel;
}

[System.Serializable]
public class ItemInfoListData
{
    public List<ItemInfoSaveData> itemInfoList = new();
}

[System.Serializable]
public class SkillSaveData
{
    public int skillID;
    public int skillLevel;
    public bool unlocked;
}

[System.Serializable]
public class CharacterSkillData
{
    public int charID;
    public int classID; 
    public List<SkillSaveData> skillSaveDatas = new();
}

public static class SaveManager
{
    private static string mapDataPath = Path.Combine(Application.persistentDataPath, "mapdata.json");
    private static string stageDataPath = Path.Combine(Application.persistentDataPath, "stagedata.json");
    private static string charInfoDatPah = Path.Combine(Application.persistentDataPath, "charinfo.json");
    private static string inventoryPath = Path.Combine(Application.persistentDataPath, "invetory.json");
    private static string skillPath = Path.Combine(Application.persistentDataPath, "learnskill.json");

    #region MapData
    public static void SaveMap(MapData mapData)
    {
        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(mapDataPath, json);
        Debug.Log($"Map data saved at : {mapDataPath}");
    }

    public static MapData LoadMap()
    {
        if (!File.Exists(mapDataPath))
        {
            Debug.LogWarning("No saved map data found.");
            return null;
        }

        string json = File.ReadAllText(mapDataPath);
        MapData mapData = JsonUtility.FromJson<MapData>(json);
        return mapData;
    }

    public static void DeleteMapData()
    {
        if (File.Exists(mapDataPath))
            File.Delete(mapDataPath);
    }
    #endregion

    #region Explore_Stage 
    public static void SaveStageNode(StageNodeData stageNodeData)
    {
        string json = JsonUtility.ToJson(stageNodeData, true);
        File.WriteAllText(stageDataPath, json);
        Debug.Log($"Stage data saved at : {stageDataPath}");
    }

    public static StageNodeData LoadStageNode()
    {
        if (!File.Exists(stageDataPath))
        {
            Debug.LogWarning("No saved stage data found.");
            return null;
        }

        string json = File.ReadAllText(stageDataPath);
        StageNodeData stageData = JsonUtility.FromJson<StageNodeData>(json);
        return stageData;
    }
    #endregion

    #region Character_Info
    public static void SaveCharacter(CharInfoListData charInfoListData)
    {
        string json = JsonUtility.ToJson(charInfoListData, true);
        File.WriteAllText(charInfoDatPah, json);
        Debug.Log($"Char info data saved at : {charInfoDatPah}");
    }

    public static CharInfoListData LoadCharInfoData()
    {
        if (!File.Exists(charInfoDatPah))
        {
            Debug.LogWarning("No save char info data found.");
            return null;
        }

        string json = File.ReadAllText(charInfoDatPah);
        CharInfoListData cild = JsonUtility.FromJson<CharInfoListData>(json);
        return cild;
    }
    #endregion

    #region Inventory 
    public static void SaveInvetoryData(ItemInfoListData itemInfoListData)
    {
        string json = JsonUtility.ToJson(itemInfoListData, true);
        File.WriteAllText(inventoryPath, json);
        Debug.Log($"Inventory data saved at : {inventoryPath}");
    }

    public static ItemInfoListData LoadInventoryData()
    {
        if (!File.Exists(inventoryPath))
        {
            Debug.LogWarning("No save inventory data found.");
            return null;
        }

        string json = File.ReadAllText(inventoryPath);
        ItemInfoListData data = JsonUtility.FromJson<ItemInfoListData>(json);
        return data;
    }
    #endregion

    #region Skill
    public static void SaveSkillData(CharacterSkillData skillSaveData)
    {
        string json = JsonUtility.ToJson(skillSaveData, true);
        File.WriteAllText(skillPath, json);
        Debug.Log($"Skill Tree Data Save at :{skillPath}");
    }

    public static CharacterSkillData LoadSkillData()
    {
        if(!File.Exists(skillPath))
        {
            Debug.LogWarning("No save skill data found.");
            return null;
        }

        string json = File.ReadAllText(@skillPath);
        CharacterSkillData data = JsonUtility.FromJson<CharacterSkillData>(json);
        return data; 
    }
    #endregion
}
