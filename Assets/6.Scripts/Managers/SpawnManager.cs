using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnObject
{
    public string spawnTag;
    public Vector3 spawnPos;
    public Quaternion spawnQuat;

    public void Spawn()
    {
        ObjectPooler.DeferedSpawnFromPool(spawnTag, spawnPos, spawnQuat);
    }
}


public class SpawnManager : MonoBehaviour
{
    public SO_PlayerObjects soPlayerObject;
    public SO_NPCObjects soNpcObject;

    public event Action OnCompleteSpawn;

    private void Awake()
    {
        if (soPlayerObject != null)
            soPlayerObject.Init();

        if (soNpcObject != null)
            soNpcObject.Init();
    }


    public void SpawnCharacter(int id, List<Transform> spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Count <= 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Spawn Point Don't exist");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"Spawn Player");
#endif

        //TODO : 여러 캐릭터를 조작해야하면 여기를 수정
        GameObject playerObj = soPlayerObject.GetPlayerObject(id);
        if (playerObj != null)
        {
            var player = Instantiate(playerObj, spawnPoints[0].position, spawnPoints[0].rotation);
            
            if(Camera.main.TryGetComponent<CinemachineCamera>(out var cc))
                cc.Target.TrackingTarget = player.transform;
        }
    }

    public void SpawnNPC(int groupID, List<Transform> spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Count <= 0)
        {
#if UNITY_EDITOR
            Debug.Log($"Spawn Point Don't exist");
#endif
            return;
        }

        MonsterGroupData data = GameManager.Instance.GetGroupData(groupID);
        if (data == null) return;

        foreach (var id in data.monsterIDs)
        {
#if UNITY_EDITOR
            Debug.Log($"Monster ID : {id}");
#endif
            int idx = Random.Range(0, spawnPoints.Count);

            string tag = $"NPC_{id}";
            ObjectPooler.SpawnFromPool(tag, spawnPoints[idx]);
        }

        OnCompleteSpawn?.Invoke();
    }



    public void OnEndSpawn() { }
}
