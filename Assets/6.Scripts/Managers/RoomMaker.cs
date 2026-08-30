using System.Collections.Generic;
using UnityEngine;
public class RoomData
{
    public List<Transform> MainSpawnPoints = new List<Transform>();
    public List<Transform> EnemySpawnPoints = new List<Transform>();
}

public sealed class RoomMaker
{
    public RoomData CreateRoom(StageInfo stageInfo)
    {
        RoomData roomData = new RoomData();

        if (stageInfo == null)
            return roomData;

        // 1. StageInfo에 저장된 인덱스의 맵 꺼내기 
        GameObject prefab = GetRoomPrefab(stageInfo);

        // 2. map 생성 
        if (prefab == null) 
            return roomData;

        GameObject roomObject = UnityEngine.Object.Instantiate(prefab);
        if (roomObject == null)
            return roomData;

        CollectSpawnPoints(roomObject, roomData);

        return roomData; 
    }

    private void CollectSpawnPoints(GameObject roomObject, RoomData roomData)
    {
        if(roomObject == null || roomData == null) return;

        var mainSpawnRoot = roomObject.transform.Find("MainSpawnPoints");
        if (mainSpawnRoot != null)
        {
            foreach (Transform t in mainSpawnRoot)
                roomData.MainSpawnPoints.Add(t);
        }

        var spawnRoot = roomObject.transform.Find("SpawnPoints");
        if (spawnRoot != null)
        {
            foreach (Transform t in spawnRoot)
                roomData.EnemySpawnPoints.Add(t);
        }
    }

    private GameObject GetRoomPrefab(StageInfo stageInfo)
    {
        if(stageInfo == null) return null;  

        if(stageInfo.mapIndex < 0)
            return DataBaseManager.Instance.SafeInvoke(
            v => v.GetRandBiomeObj(stageInfo.chapter));
        else
            return DataBaseManager.Instance.SafeInvoke(
            v => v.GetTargetBiomeObj(
                stageInfo.chapter, 
            stageInfo.mapIndex));
    }
}
