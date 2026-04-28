using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Rendering.UI;

public sealed class RoomManager : MonoBehaviour
{
    private BiomeManager biomeManager;

    private void Awake()
    {
        biomeManager = GetComponent<BiomeManager>();
    }

    public void LoadRoom(StageInfo curStage, ref List<Transform> mainSpawnPoints, ref List<Transform> spawnPoints)
    {
        if (curStage == null)
            return;

        // 1. StageInfo에 저장된 테마 씌우기 
        biomeManager.ChangeBiome(curStage.biome);

        // 2. StageInfo에 저장된 인덱스의 맵 꺼내기 
        SO_Biome biome = biomeManager.GetBiomeData(curStage.biome);
        GameObject prefab = biome.possibleRoomPrefabs[curStage.mapIndex]; 

        // 3. map 생성 
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
