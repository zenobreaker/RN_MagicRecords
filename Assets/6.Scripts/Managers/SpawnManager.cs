using System;
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
    public event Action OnCompleteSpawn;

    private void Awake()
    {
        StageManager stage = GetComponent<StageManager>();
        if (stage != null)
            stage.OnBeginSpawn += OnBeginSpawn;
    }

    private void SpawnObject()
    {
        // Spawn Object
        {

        }

        OnCompleteSpawn?.Invoke(); 
    }

    public void OnBeginSpawn()
    {
        SpawnObject();
    }

    public void OnEndSpawn() { }
}
