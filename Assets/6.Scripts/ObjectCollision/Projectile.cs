using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile
    : AbstractProjectile
    , IProjectile
{
    [Header("Pierce Settings")]
    [Tooltip("0이면 1명 맞고 소멸, 1이면 1명 관통")]
    [SerializeField] private int basePierceCount = 0;

    // 💡 날아가는 동안 깎일 실제 관통 횟수 (프로퍼티와 연결)
    private int currentPierceCount;

    private int index;
    public int Index { get { return index; } set { index = value; } }

    public int PierceCount
    {
        get => currentPierceCount;
        set => currentPierceCount = value;
    }

    // 💡 1. 세상에 처음 태어났을 때 최초 1회 초기화
    protected override void Awake()
    {
        base.Awake(); // 부모의 Awake(리지드바디, 콜라이더 캐싱) 호출 필수!
        currentPierceCount = basePierceCount;
    }

    protected override void OnProjectileDespawned()
    {
        currentPierceCount = basePierceCount;
    }

    protected override void ProcessHit(Collider other)
    {
        // 무시 목록 추가 및 데미지
        AddIgnore(other.gameObject);

        Vector3 hitPoint = collider.ClosestPoint(other.transform.position);
        DealDamage(other.gameObject, other.transform.InverseTransformPoint(hitPoint));

        // 관통 로직
        if (currentPierceCount > 0)
            currentPierceCount--;
        else
            this.gameObject.SetActive(false);
    }
}
