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

    private static bool bInitialized = false;
    public static bool IsInitialized { get => bInitialized; }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            // 데이터 동기화가 필요한 것이 아니라면 파괴 후 아무 것도 안함
            SyncDataFromSingleton();
            Destroy(gameObject);
            return; 
        }
    }

    protected virtual void Start()
    {
        bInitialized = true;
        ManagerWaiter.NotifyManagerReady<T>((T)(object)this);
    }

    protected virtual void SyncDataFromSingleton()
    {
        
    }
}
