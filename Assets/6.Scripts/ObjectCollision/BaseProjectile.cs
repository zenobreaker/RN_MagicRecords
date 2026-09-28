using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

// 💡 추상 클래스로 선언하여 직접 생성을 막고, 공통 기능만 물려줍니다.
public abstract class BaseProjectile
    : MonoBehaviour
    , ISkillEffect
{
    protected List<IProjectileOnSpawnRunner> spawnRunners = new List<IProjectileOnSpawnRunner>();
    protected List<IPorjectileOnHitRunner> hitRunners = new List<IPorjectileOnHitRunner>();
    protected List<IProjectileOnUpdateRunner> updateRunners = new List<IProjectileOnUpdateRunner>();
    protected List<IProjectileOnDestroyRunner> destroyRunners = new List<IProjectileOnDestroyRunner>();

    protected GameObject ownerObject;
    protected Character owner;
    protected DamageEvent damageEvent;

    protected DamageData cachedDamageData;
    private float shotDamageMultiplier = 1f;
    private bool shotExtraCrit;
    public Character Owner => owner;
    public DamageData DamageData => cachedDamageData;

    // 이 투사체에 배정된 탄환. Hit/Destroy runner에서도 읽을 수 있습니다.
    public IReadOnlyList<BulletData> ConsumedBullets { get; private set; } = Array.Empty<BulletData>();
    public BulletEffectApplyMode BulletApplyMode { get; private set; }

    public void SetMagicBullets(IReadOnlyList<BulletData> bullets, BulletEffectApplyMode mode)
    {
        BulletApplyMode = mode;
        if (bullets == null || bullets.Count == 0)
        {
            ConsumedBullets = Array.Empty<BulletData>();
            return;
        }

        var snapshot = new BulletData[bullets.Count];
        for (int i = 0; i < snapshot.Length; i++) snapshot[i] = bullets[i];
        ConsumedBullets = Array.AsReadOnly(snapshot);
    }

    // 자탄 생성 경로에서 선택적으로 호출합니다. 큐를 다시 소비하지 않습니다.
    public void CopyMagicBulletsTo(BaseProjectile child)
    {
        if (child != null && BulletApplyMode == BulletEffectApplyMode.InheritToChildren)
            child.SetMagicBullets(ConsumedBullets, BulletApplyMode);
    }

    // 피아식별용 공통 변수
    protected GenenricTeamId myTeamId = GenenricTeamId.NoTeamId;
    protected HashSet<GameObject> ignores = new HashSet<GameObject>();
    public HashSet<GameObject> Ignores => ignores;

    public event Action<GameObject, Vector3> OnTargetHitEvent;

    // ==========================================
    // 1. ISkillEffect 공통 구현부 (자식들은 안 써도 됨!)
    // ==========================================
    public virtual void SetDamageInfo(Character attacker, DamageData damageData, bool bExtraCrit = false, float multiplier = 1.0f)
    {
        if (attacker == null || damageData == null) return;

        ownerObject = attacker;
        owner = attacker;
        cachedDamageData = damageData;
        shotDamageMultiplier = multiplier;
        shotExtraCrit = bExtraCrit;
        damageEvent = damageData.GetMyDamageEvent(attacker.Status, false, bExtraCrit, multiplier);

        // 부모가 알아서 쏜 사람의 팀 ID를 캐싱해 둡니다.
        myTeamId = TeamUtility.GetTeamId(attacker);
    }

    public virtual void AddIgnore(GameObject ignore)
    {
        if (ignore != null) ignores.Add(ignore);
    }

    public void SetIgnores(HashSet<GameObject> ignores)
    {
        if (ReferenceEquals(this.ignores, ignores)) return;
        this.ignores.Clear();
        if (ignores != null) this.ignores.UnionWith(ignores);
    }

    // ==========================================
    // 2. 자식들이 꿀 빨게 될 마법의 공통 함수 
    // ==========================================
    protected bool IsFriendlyFire(GameObject target)
    {
        return CombatHelper.IsFriendly(
            owner,
            target,
            ignores
        );
    }

    // ==========================================
    // 3. 풀링 초기화 공통 로직
    // ==========================================
    protected virtual void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < updateRunners.Count && isActiveAndEnabled; i++)
        {
            updateRunners[i].OnUpdate(this, dt);
        }
    }

    protected virtual void OnEnable()
    {
        for (int i = 0; i < spawnRunners.Count && isActiveAndEnabled; i++)
            spawnRunners[i].OnSpawn(this);
    }

    protected virtual void OnDisable()
    {
        foreach (var destroy in destroyRunners)
        {
            destroy.OnDestroy(this);
        }
        destroyRunners.Clear();
        spawnRunners.Clear();
        hitRunners.Clear();
        updateRunners.Clear();
        ignores.Clear();
        myTeamId = GenenricTeamId.NoTeamId;
        ownerObject = null;
        owner = null;
        cachedDamageData = null;
        shotDamageMultiplier = 1f;
        shotExtraCrit = false;
        damageEvent = default;
        OnTargetHitEvent = null;
        SetMagicBullets(null, BulletEffectApplyMode.FirstAttackOnly);
    }

    public virtual void NotifyHit(GameObject target, Vector3 hitPos)
    {
        for (int i = 0; i < hitRunners.Count && isActiveAndEnabled; i++)
        {
            hitRunners[i].OnHit(this, target);
        }
    }

    //protected void DealDamage(GameObject target, Vector3 hitPoint)
    //{
    //    if (target.TryGetComponent<IDamagable>(out var damage))
    //    {
    //        damage?.OnDamage(ownerObject, null, hitPoint, damageEvent);
    //    }

    //    NotifyHit(target, hitPoint);

    //    OnTargetHitEvent?.Invoke(target, hitPoint);
    //}

    protected void DealDamage(GameObject target, Vector3 hitPoint)
    {
        if (owner == null || cachedDamageData == null) return;
        CombatHelper.ApplyDamage(
            owner,
            cachedDamageData.GetMyDamageEvent(owner.Status, false, shotExtraCrit, shotDamageMultiplier),
            target,
            hitPoint
        );

        NotifyHit(target, hitPoint);

        OnTargetHitEvent?.Invoke(target, hitPoint);
    }



    public void AddSpawnRunner(IProjectileOnSpawnRunner spawn)
    {
        spawnRunners.Unique(spawn);
    }

    public void AddHitRunner(IPorjectileOnHitRunner hit)
    {
        hitRunners.Unique(hit);
    }

    public void AddDestroyRunner(IProjectileOnDestroyRunner destroy)
    {
        destroyRunners.Unique(destroy);
    }

    public void AddUpdateRunner(IProjectileOnUpdateRunner update)
    {
        updateRunners.Unique(update);
    }
}
