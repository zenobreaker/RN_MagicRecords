using System;
using System.Collections;
using UnityEngine;

public static class ManagerWaiter
{
    private static MonoBehaviour coroutineHost;

    public static void InitializeHost(MonoBehaviour host) => coroutineHost = host;

    public static void WaitForManager<T>(Action<T> onReady) where T : MonoBehaviour
    {
        if(coroutineHost == null)
        {
            Debug.LogError($"[ManagerWaiter] Host not set. Call ManagerWaiter.InitializeHost() first.");
            return; 
        }

        coroutineHost.StartCoroutine(WaitUntilManagerReady(onReady));
    }

    private static IEnumerator WaitUntilManagerReady<T>(Action<T> onReady) where T : MonoBehaviour
    {
        yield return new WaitUntil(() => Singleton<T>.Instance != null);
        onReady?.Invoke(Singleton<T>.Instance); 
    }
}
