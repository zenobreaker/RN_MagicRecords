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

public static class SaveManager 
{
    private static string mapDataPath = Path.Combine(Application.persistentDataPath, "mapdata.json");
    private static string stageDataPath = Path.Combine(Application.persistentDataPath, "stageData.json");

    #region MapData
    public static void SaveMap(MapData mapData)
    {
        string json = JsonUtility.ToJson(mapData);
        File.WriteAllText(mapDataPath, json);
        Debug.Log($"Map data saved at : {mapDataPath}");    
    }

    public static MapData LoadMap()
    {
        if(!File.Exists(mapDataPath))
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
        if(File.Exists(mapDataPath)) 
            File.Delete(mapDataPath);
    }
    #endregion

    public static void SaveStageNode(StageNodeData stageNodeData)
    {
        string json = JsonUtility.ToJson(stageNodeData);
        File.WriteAllText (stageDataPath, json);
        Debug.Log($"Stage data saved at : {stageDataPath}");
    }

    public static StageNodeData LoadStageNode()
    {
        if (!File.Exists(stageDataPath))
        {
            Debug.LogWarning("No saved stage data found.");
            return null;
        }

        string json = File.ReadAllText (stageDataPath);
        StageNodeData stageData = JsonUtility.FromJson<StageNodeData>(json);
        return stageData;
    }
}
