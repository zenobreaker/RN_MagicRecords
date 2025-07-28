using System;
using System.Collections.Generic;
using UnityEngine;

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
    public NPCObjectsSO soNpcObject;

    public event Action OnCompleteSpawn;

    private void Awake()
    {
        StageManager stage = GetComponent<StageManager>();
        if (stage != null)
            stage.OnBeginSpawn += OnBeginSpawn;
    }

    private void SpawnObject(int groupID)
    {
        // Spawn Object
        {
            
            MonsterGroupData data = GameManager.Instance.GetGroupData(groupID);
            if (data == null) return; 

            foreach(var id in data.monsterIDs)
            {
                Debug.Log($"Monster ID : {id}");
                // TODO: 맵 위치 데이터가 필요하다.
                //ObjectPooler.DeferedSpawnFromPool(id,);
            }
            
        }

        OnCompleteSpawn?.Invoke();
    }

    public void OnBeginSpawn(int groupID)
    {
        SpawnObject(groupID);
    }

    public void OnEndSpawn() { }
}
