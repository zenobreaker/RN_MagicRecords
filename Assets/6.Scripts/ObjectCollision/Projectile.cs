using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile
    : BaseProjectile
    , IProjectile
{
    [Header("Projectile Settings")]
    [SerializeField] private float force = 1000.0f;
    [SerializeField] private float life = 10.0f;
    [SerializeField] private LayerMask ignoreLayer;

    [SerializeField] private string bombEffectName = "";
    [SerializeField] private string impactSoundName = "";

    [Header("Pierce Settings")]
    [Tooltip("0이면 1명 맞고 소멸, 1이면 1명 관통")]
    [SerializeField] private int basePierceCount = 0;

    // 💡 날아가는 동안 깎일 실제 관통 횟수 (프로퍼티와 연결)
    private int currentPierceCount;

    private float curLife = 0f;
    private int index;
    public int Index { get { return index; } set { index = value; } }

    public int PierceCount
    {
        get => currentPierceCount;
        set => currentPierceCount = value;
    }

    private new Rigidbody rigidbody;
    private new Collider collider;

    //public event Action<Collider> OnTriggerEnterAction;
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

        currentPierceCount = basePierceCount;
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
        if (life != -1)
        {
            curLife -= Time.deltaTime;
            if (curLife <= 0f)
                this.gameObject.SetActive(false);
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (ignores.Contains(other.gameObject))
            return;

        if (ignoreLayer.Contains(other.gameObject))
            return;

        if (IsFriendlyFire(other.gameObject))
            return;

        AddIgnore(other.gameObject);
        OnProjectileHit?.Invoke(collider, other, transform.position);

        if (string.IsNullOrEmpty(bombEffectName) == false)
        {
            ObjectPooler.SpawnFromPool(bombEffectName, this.transform.position);
        }

        // Play Sound
        {
            SoundManager.Instance.SafeInvoke(v => v.PlaySFX(impactSoundName));
        }

        // Damage 
        Vector3 hitPoint = collider.ClosestPoint(other.transform.position);
        hitPoint = other.transform.InverseTransformPoint(hitPoint);
        DealDamage(other.gameObject, hitPoint);

        if (currentPierceCount > 0)
        {
            currentPierceCount--;
        }
        else 
            this.gameObject.SetActive(false);
        
    }
}
