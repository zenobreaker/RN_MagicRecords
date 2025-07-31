using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class RoomInfo
{
    public int id;
    public GameObject mapObject;
}

public class RoomManager : MonoBehaviour
{
    [SerializeField] private List<RoomInfo> roomInfos = new(); 
    private Dictionary<int, GameObject> roomTable = new();

    private void Awake()
    {
        foreach (var roomInfo in roomInfos)
            roomTable.Add(roomInfo.id, roomInfo.mapObject);
    }

    public void LoadRoom(int mapID, ref List<Transform> mainSpawnPoints, ref List<Transform> spawnPoints)
    {
        GameObject prefab = roomTable[mapID];
        GameObject map = Instantiate<GameObject>(prefab);
        if (map == null) return;

        var mainSpawnRoot = map.transform.Find("MainSpawnPoints");
        if (mainSpawnRoot != null) 
        {
            mainSpawnPoints.Clear();
            foreach (Transform t in mainSpawnRoot)
                mainSpawnPoints.Add(t);
        }

        var spawnRoot = map.transform.Find("SpawnPoints");
        if (spawnRoot != null)
        {
            spawnPoints.Clear();
            foreach (Transform t in spawnRoot)
                spawnPoints.Add(t);
        }
    }
}
