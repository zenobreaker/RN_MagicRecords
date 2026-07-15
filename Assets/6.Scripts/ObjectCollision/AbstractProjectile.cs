using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class AbstractProjectile 
    : BaseProjectile
    , ISkillEffect
{
    [Header("Base Projectile Settings")]
    [SerializeField] protected float force = 20.0f;
    [SerializeField] protected float life = 5.0f;
    [SerializeField] protected LayerMask ignoreLayer;
    [SerializeField] protected string bombEffectName = "";
    [SerializeField] protected string impactSoundName = "";

    protected float curLife = 0f;
    protected Rigidbody rigid;
    protected new Collider collider;

    // 타격 이벤트
    public event Action<Collider, Collider, Vector3> OnProjectileHit;

    protected virtual void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
    }

    // 💡 [핵심] 공통 생명주기 처리는 부모가 알아서 다 합니다.
    protected override void OnEnable()
    {
        if (rigid == null) return;
        base.OnEnable();
        
        // 1. 공통 물리 / 수명 초기화
        rigid.linearVelocity = transform.forward * force;
        curLife = life;

        // 2. 자식 클래스 전용 초기화 호출 (빈 칸)
        OnProjectileSpawned();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ObjectPooler.ReturnToPool(this.gameObject);

        if (rigid != null)
        {
            rigid.linearVelocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }

        // 자식 클래스 전용 해제 로직 호출 (빈 칸)
        OnProjectileDespawned();
    }

    protected override void Update()
    {
        base.Update(); 

        if (life != -1)
        {
            curLife -= Time.deltaTime;
            if (curLife <= 0f)
            {
                this.gameObject.SetActive(false);
                return;
            }
        }

        // 자식 클래스의 매 프레임 로직 호출 (드릴 틱, 자탄 분열 타이머 등)
        OnProjectileUpdate();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (ignores.Contains(other.gameObject)) return;
        if (ignoreLayer.Contains(other.gameObject)) return;
        if (IsFriendlyFire(other.gameObject)) return;

        // 공통 이벤트 및 사운드/이펙트 재생
        OnProjectileHit?.Invoke(collider, other, transform.position);

        if (!string.IsNullOrEmpty(bombEffectName))
            ObjectPooler.SpawnFromPool(bombEffectName, transform.position);

        // Play Sound
        SoundManager.Instance.SafeInvoke(v => v.PlaySFX(impactSoundName));

        // 자식 클래스의 고유 충돌 로직 호출 (관통 카운트 감소, 드릴 리스트 추가 등)
        ProcessHit(other);
    }

    // =======================================================
    // 🧩 자식 클래스가 입맛대로 구현할 '빈 칸 (Hooks)'
    // =======================================================
    protected virtual void OnProjectileSpawned() { }
    protected virtual void OnProjectileDespawned() { }
    protected virtual void OnProjectileUpdate() { }
    protected virtual void ProcessHit(Collider other) { }
}