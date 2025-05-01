using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float force = 1000.0f;
    [SerializeField] private float life = 10.0f;

    [SerializeField] private string bombEffectName = "";

    private float curLife = 0f;

    private int index; 
    public int Index { get { return index; }  set {  index = value; } }

    private new Rigidbody rigidbody;
    private new Collider collider; 

    public event Action<Collider> OnTriggerEnterAction;
    public event Action<Collider, Collider, Vector3> OnProjectileHit;

    private List<GameObject> ignores = new List<GameObject>();
    public void AddIgnore(GameObject ignore)
    {
        if (ignores.Contains(ignore) == true)
            return;

        ignores.Add(ignore);
    }

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
        var ignore = ignores.Find(x=>x.gameObject == other.gameObject);
        if (ignore != null)
            return; 

        OnTriggerEnterAction?.Invoke(other);

        OnProjectileHit?.Invoke(collider,other, transform.position);

        this.gameObject.SetActive(false);

        if(string.IsNullOrEmpty(bombEffectName) == false)
        {
            ObjectPooler.SpawnFromPool(bombEffectName, this.transform.position);
        }
    }

}
