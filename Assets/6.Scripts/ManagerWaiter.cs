using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerWaiter : Singleton<ManagerWaiter>
{
    private static readonly Dictionary<Type, List<Delegate>> waiters = new();

    public static bool TryGetManager<T>(out T manager) where T : MonoBehaviour
    {
        if (Singleton<T>.Instance != null)
        {
            manager = Singleton<T>.Instance;
            return true;
        }

        manager = null;
        return false;
    }

    public static void WaitForManager<T>(Action<T> onReady) where T : MonoBehaviour
    {
        if (Singleton<T>.Instance != null && Singleton<T>.IsInitialized)
        {
            onReady?.Invoke(Singleton<T>.Instance);
            return;
        }

        if (!waiters.ContainsKey(typeof(T)))
            waiters[typeof(T)] = new List<Delegate>();

        waiters[typeof(T)].Add(onReady);
    }

    // returns IDisposable registration so caller can cancel waiting before manager is ready
    public static IDisposable WaitForManagerDisposable<T>(Action<T> onReady) where T : MonoBehaviour
    {
        if (Singleton<T>.Instance != null && Singleton<T>.IsInitialized)
        {
            onReady?.Invoke(Singleton<T>.Instance);
            return new EmptyDisposable();
        }

        if (!waiters.ContainsKey(typeof(T)))
            waiters[typeof(T)] = new List<Delegate>();

        waiters[typeof(T)].Add(onReady);
        return new Registration(typeof(T), onReady);
    }

    /// <summary>
    /// Convenience helper. Waits for manager of type T to be ready, invokes onRegister(manager) immediately,
    /// and attaches a ManagerEventUnsubscriber to the owner's GameObject which will call onUnregister(manager)
    /// automatically when the owner is disabled.
    /// </summary>
    public static void RegisterManagerEvent<T>(MonoBehaviour owner, Action<T> onRegister, Action<T> onUnregister) where T : MonoBehaviour
    {
        if (owner == null) return;

        WaitForManager<T>(manager =>
        {
            try
            {
                onRegister?.Invoke(manager);
                // attach unsubscriber to owner to invoke unregister when owner is disabled
                var proxy = owner.gameObject.AddComponent<ManagerEventUnsubscriber>();
                proxy.Init(() => onUnregister?.Invoke(manager));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        });
    }

    public static void NotifyManagerReady<T>(T manager) where T : MonoBehaviour
    {
        if (!waiters.ContainsKey(typeof(T))) return;

        foreach (var w in waiters[typeof(T)])
        {
            (w as Action<T>)?.Invoke(manager);
        }

        waiters.Remove(typeof(T));
    }

    private class Registration : IDisposable
    {
        private readonly Type key;
        private readonly Delegate callback;
        private bool disposed = false;

        public Registration(Type key, Delegate callback)
        {
            this.key = key;
            this.callback = callback;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            if (!waiters.ContainsKey(key)) return;
            var list = waiters[key];
            list.Remove(callback);
            if (list.Count == 0)
                waiters.Remove(key);
        }
    }

    private class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
