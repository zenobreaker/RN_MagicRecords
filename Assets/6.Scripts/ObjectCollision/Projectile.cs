using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class Projectile 
    : BaseProjectile
    , ISkillEffect
{
    [Header("Projectile Settings")]
    [SerializeField] private float force = 1000.0f;
    [SerializeField] private float life = 10.0f;
    [SerializeField] private LayerMask ignoreLayer;

    [SerializeField] private string bombEffectName = "";
    [SerializeField] private string impactSoundName = ""; 

    private float curLife = 0f;

    private int index; 
    public int Index { get { return index; }  set {  index = value; } }

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


    protected override void OnDisable()
    {
        base.OnDisable();
        ObjectPooler.ReturnToPool(this.gameObject);

        if (rigidbody == null)
            return;

        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
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
        if (ignores.Contains(other.gameObject))
            return;

        if (ignoreLayer.Contains(other.gameObject))
            return;

        if (IsFriendlyFire(other.gameObject))
            return;

        OnTriggerEnterAction?.Invoke(other);

        OnProjectileHit?.Invoke(collider, other, transform.position);

        this.gameObject.SetActive(false);

        if(string.IsNullOrEmpty(bombEffectName) == false)
        {
            ObjectPooler.SpawnFromPool(bombEffectName, this.transform.position);
        }

        // Play Sound
        {
            SoundManager.Instance.SafeInvoke(v => v.PlaySFX(impactSoundName));
        }

        // Damage 
        IDamagable damage = other.GetComponent<IDamagable>();
        if (damage != null)
        {
            Vector3 hitPoint = collider.ClosestPoint(other.transform.position);
            hitPoint = other.transform.InverseTransformPoint(hitPoint);
            damage?.OnDamage(ownerObject, null, hitPoint, damageEvent);
        }
    }
}
