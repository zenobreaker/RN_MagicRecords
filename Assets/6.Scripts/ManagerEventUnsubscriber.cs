using System;
using UnityEngine;

// Helper component attached to owner gameobject to automatically run unregister action on OnDisable
public class ManagerEventUnsubscriber : MonoBehaviour
{
    private Action onUnsubscribe;
    public void Init(Action onUnsubscribe)
    {
        this.onUnsubscribe = onUnsubscribe;
    }

    private void OnDisable()
    {
        onUnsubscribe?.Invoke();
        Destroy(this);
    }
}
