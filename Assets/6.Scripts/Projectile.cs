using System;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float force = 1000.0f;
    [SerializeField] private float life = 10.0f;

    private new Rigidbody rigidbody;
    private new Collider collider; 


    public event Action<Collider> OnTriggerEnterAction;
    public event Action<Collider, Collider, Vector3> OnProjectileHit; 

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        Debug.Assert(rigidbody != null);
        collider = GetComponent<Collider>();

    }

    protected virtual void OnEnable()
    {
        if (rigidbody == null)
            return;

        rigidbody.AddForce(transform.forward * force);
        
    }


    protected virtual void OnDisable()
    {
        ObjectPooler.ReturnToPool(this.gameObject);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterAction?.Invoke(other);

        OnProjectileHit?.Invoke(collider,other, transform.position);

        this.gameObject.SetActive(false);
    }

}
