using System;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<T>();
            return instance;
        }
        protected set
        {
            instance = value;
        }
    }

    protected bool bIsAwaked = false;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            SyncDataFromSingleton();
            Destroy(gameObject);
        }
    }

    protected virtual void SyncDataFromSingleton()
    {
        
    }
}
