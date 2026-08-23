using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class PiercingProjectile : AbstractProjectile
{
    [Header("Piercing Settings")]
    [Tooltip("-1 = 무한 관통 / 0 = 첫 대상 피격 후 소멸 / 1 = 1회 관통")]
    [SerializeField] private int basePierceCount = 0;

    // 런타임에서 실제로 남아있는 관통 횟수
    private int currentPierceCount;

    // 한 번 맞은 대상을 다시 피격하지 않도록 관리
    private readonly HashSet<GameObject> hitTargets = new();

    /// <summary>
    /// 현재 남은 관통 횟수
    /// -1이면 무한 관통
    /// </summary>
    public int PierceCount
    {
        get => currentPierceCount;
        set => currentPierceCount = value;
    }

    /// <summary>
    /// 기본 관통 횟수
    /// </summary>
    public int BasePierceCount
    {
        get => basePierceCount;
        set
        {
            basePierceCount = value;
            currentPierceCount = value;
        }
    }

    protected override void OnProjectileSpawned()
    {
        base.OnProjectileSpawned();

        // 풀링 재사용 시 반드시 초기화
        currentPierceCount = basePierceCount;
        hitTargets.Clear();
    }

    protected override void OnProjectileDespawned()
    {
        hitTargets.Clear();

        // 다음 스폰을 위해 기본값으로 복구
        currentPierceCount = basePierceCount;

        base.OnProjectileDespawned();
    }

    protected override void ProcessHit(Collider other)
    {
        if (other == null)
            return;

        // 이미 맞은 대상이면 무시
        GameObject target = GetDamageTarget(other);

        if (target == null)
            return;

        if (hitTargets.Contains(target))
            return;

        // 자신/아군 판정
        if (IsFriendlyFire(target))
            return;

        // --------------------------------------------------
        // 1. 피격 처리
        // --------------------------------------------------

        hitTargets.Add(target);
        AddIgnore(target);

        Vector3 hitPoint = collider.ClosestPoint(other.transform.position);
        hitPoint = other.transform.InverseTransformPoint(hitPoint);

        DealDamage(target, hitPoint);

        // --------------------------------------------------
        // 2. 관통 처리
        // --------------------------------------------------

        // -1 = 무한 관통
        if (currentPierceCount == -1)
            return;

        // 0 = 관통 불가
        // 이미 데미지를 줬으므로 여기서 소멸
        if (currentPierceCount <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        // 1 이상이면 관통 횟수 1회 소모
        currentPierceCount--;
    }

    /// <summary>
    /// Collider의 실제 공격 대상 Character를 찾는다.
    /// 여러 Collider를 가진 몬스터도 하나의 대상으로 처리.
    /// </summary>
    private GameObject GetDamageTarget(Collider targetCollider)
    {
        if (targetCollider == null)
            return null;

        if (targetCollider.TryGetComponent<Character>(
                out Character character))
        {
            return character.gameObject;
        }

        Character parentCharacter =
            targetCollider.GetComponentInParent<Character>();

        if (parentCharacter != null)
            return parentCharacter.gameObject;

        // 일반 오브젝트라면 Collider 자신을 대상 취급
        return targetCollider.gameObject;
    }

    /// <summary>
    /// 런타임에서 관통 횟수를 설정할 때 사용.
    /// </summary>
    public void SetPierceCount(int count)
    {
        currentPierceCount = count;
    }

    /// <summary>
    /// 무한 관통 설정
    /// </summary>
    public void SetInfinitePierce()
    {
        currentPierceCount = -1;
    }
}