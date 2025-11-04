using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerWaiter : Singleton<ManagerWaiter>
{
    private static readonly Dictionary<Type, List<Delegate>> waiters = new();

    public static bool TryGetManager<T>(out T manager) where T : MonoBehaviour
    {
        if(Singleton<T>.Instance != null)
        {
            manager = Singleton<T>.Instance;
            return true; 
        }

        manager = null;
        return false; 
    }

    public static void WaitForManager<T>(Action<T> onReady) where T : MonoBehaviour
    {
        if(Singleton<T>.Instance != null && Singleton<T>.IsInitialized)
        {
            onReady?.Invoke(Singleton<T>.Instance);
            return; 
        }

        if(!waiters.ContainsKey(typeof(T)))
            waiters[typeof(T)] = new List<Delegate>();

        waiters[typeof(T)].Add(onReady);
    }

    public static void NotifyManagerReady<T>(T manager) where T : MonoBehaviour    
    {
        if (!waiters.ContainsKey(typeof(T))) return;

        foreach(var w in waiters[typeof(T)])
        {
            (w as Action<T>)?.Invoke(manager); 
        }

        waiters.Remove(typeof(T));
    }
}
