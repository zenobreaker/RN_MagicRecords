using System;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public event Action OnStartSpawn;
    public event Action OnCompleteSpawn;

    private void Awake()
    {
        StageManager stage = GetComponent<StageManager>();
        if (stage != null)
            stage.OnBeginSpawn += OnBeginSpawn;
    }

    private void SpawnObject()
    {
        OnCompleteSpawn?.Invoke(); 
    }

    public void OnBeginSpawn()
    {
        SpawnObject();
    }

    public void OnEndSpawn() { }
}
