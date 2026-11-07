using UnityEngine;

/// <summary>지정 대상에게만 도달 피해를 적용하는 어시스트 웨폰 탄환입니다.</summary>
public sealed class ExecutorProjectile : AbstractProjectile
{
    private Character target;
    private HealthPointComponent targetHealth;
    private Collider targetCollider;
    private int targetSpawnVersion;
    private bool resolved;

    public void SetTarget(Character value)
    {
        target = value;
        targetHealth = value != null ? value.GetComponent<HealthPointComponent>() : null;
        targetCollider = value != null ? value.GetComponent<Collider>() : null;
        targetSpawnVersion = value is Enemy enemy ? enemy.SpawnVersion : 0;
        resolved = false;
    }

    protected override void OnProjectileSpawned()
    {
        rigid.useGravity = false;
        rigid.linearVelocity = Vector3.zero;
        collider.enabled = false;
    }

    /// <summary>사망, 비활성화 또는 풀에서 재사용된 대상은 추적하지 않습니다.</summary>
    private bool IsTargetValid()
    {
        return target != null && target.isActiveAndEnabled && targetHealth != null &&
            targetHealth.Dead == false && ((target is Enemy) == false ||
            ((Enemy)target).SpawnVersion == targetSpawnVersion);
    }

    private void FixedUpdate()
    {
        if (resolved || owner == null || IsTargetValid() == false)
        {
            gameObject.SetActive(false);
            return;
        }
        Vector3 point = targetCollider != null ? targetCollider.bounds.center : target.transform.position + Vector3.up;
        float step = Mathf.Max(0f, force) * Time.fixedDeltaTime;
        Vector3 next = Vector3.MoveTowards(rigid.position, point, step);
        if ((next - point).sqrMagnitude <= 0.0001f)
        {
            resolved = true;
            rigid.position = point;
            if (string.IsNullOrEmpty(bombEffectName) == false)
            {
                ObjectPooler.SpawnFromPool(bombEffectName, point);
            }
            SoundManager.Instance.SafeInvoke(manager => manager.PlaySFX(impactSoundName));
            Vector3 hitPoint = target.transform.InverseTransformPoint(point);
            DealDamage(target.gameObject, hitPoint);
            gameObject.SetActive(false);
            return;
        }
        rigid.MovePosition(next);
        Vector3 direction = point - rigid.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            rigid.MoveRotation(Quaternion.LookRotation(direction));
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        // 경로상의 적, 벽, 트리거는 타격과 소멸을 발생시키지 않습니다.
    }

    protected override void OnProjectileDespawned()
    {
        target = null;
        targetHealth = null;
        targetCollider = null;
        targetSpawnVersion = 0;
        resolved = false;
        collider.enabled = true;
    }
}
