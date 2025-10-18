using System;
using System.Collections;
using UnityEngine;

public class ManagerWaiter : Singleton<ManagerWaiter>
{
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
        Instance.StartCoroutine(WaitUntilManagerReady(onReady));
    }

    private static IEnumerator WaitUntilManagerReady<T>(Action<T> onReady) where T : MonoBehaviour
    {
        yield return new WaitUntil(() => Singleton<T>.Instance != null);
        onReady?.Invoke(Singleton<T>.Instance); 
    }
}
