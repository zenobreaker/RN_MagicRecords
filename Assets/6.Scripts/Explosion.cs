using System;
using UnityEngine;

public class Explosion 
    : MonoBehaviour
    , IPoolObject
{
    [SerializeField] private float radius = 1.0f;

    private GameObject ownerObject;   // 폭발을 일으킨 주도자
    //private Collider ownerCollider;
    public event Action<Collider, Collider, Vector3> OnExpolsionHit;
    public void OnDisableEvent()
    {
        
    }

    public void OnDisable()
    {
        OnDisableEvent();
    }

    private void Start()
    {
        
    }

    private void LateUpdate()
    {
        Collider[] colliders  = Physics.OverlapSphere(transform.position, radius);

        foreach(Collider collider in colliders)
        {
            if (collider.gameObject == ownerObject)
                continue;

            OnExpolsionHit?.Invoke(null, collider, transform.position);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        
    }
#endif
}
