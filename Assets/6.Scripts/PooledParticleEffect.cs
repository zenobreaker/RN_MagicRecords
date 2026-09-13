using UnityEngine;

/// <summary>Returns a pooled effect when its root is disabled.</summary>
[DisallowMultipleComponent]
public sealed class PooledParticleEffect : MonoBehaviour
{
    private void OnDisable()
    {
        ObjectPooler.ReturnToPool(gameObject);
        CancelInvoke();
    }
}
