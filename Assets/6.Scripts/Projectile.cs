using System;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float force = 1000.0f;
    [SerializeField] private float life = 10.0f;

    private float curLife = 0f;

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
        
        // #. Unity6 기준 프로퍼티 명이 달라짐
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.AddForce(transform.forward * force);
        curLife = life;
    }


    protected virtual void OnDisable()
    {
        ObjectPooler.ReturnToPool(this.gameObject);
    }

    protected virtual void Update()
    {
        if(life != -1)
        {
            curLife -= Time.deltaTime;
            if (curLife <= 0f)
                this.gameObject.SetActive(false);
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterAction?.Invoke(other);

        OnProjectileHit?.Invoke(collider,other, transform.position);

        this.gameObject.SetActive(false);
    }

}
