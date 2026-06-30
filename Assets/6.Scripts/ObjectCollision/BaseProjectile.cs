using System;
using System.Collections.Generic;
using UnityEngine;

// 💡 추상 클래스로 선언하여 직접 생성을 막고, 공통 기능만 물려줍니다.
public abstract class BaseProjectile
    : MonoBehaviour
    , ISkillEffect
{
    protected GameObject ownerObject;
    protected Character owner; 
    protected DamageEvent damageEvent;

    // 피아식별용 공통 변수
    protected GenenricTeamId myTeamId = GenenricTeamId.NoTeamId;
    protected HashSet<GameObject> ignores = new HashSet<GameObject>();

    public event Action<GameObject, Vector3>  OnTargetHitEvent;

    // ==========================================
    // 1. ISkillEffect 공통 구현부 (자식들은 안 써도 됨!)
    // ==========================================
    public virtual void SetDamageInfo(Character attacker, DamageData damageData, bool bExtraCrit = false, float multiplier = 1.0f)
    {
        if (attacker == null || damageData == null) return;

        ownerObject = attacker;
        owner = attacker;
        damageEvent = damageData.GetMyDamageEvent(attacker.Status, false, bExtraCrit, multiplier);

        // 부모가 알아서 쏜 사람의 팀 ID를 캐싱해 둡니다.
        myTeamId = TeamUtility.GetTeamId(attacker);
    }

    public virtual void AddIgnore(GameObject ignore)
    {
        if (ignore != null) ignores.Add(ignore);
    }

    // ==========================================
    // 2. 자식들이 꿀 빨게 될 마법의 공통 함수 🍯
    // ==========================================
    protected bool IsFriendlyFire(GameObject target)
    {
        // 1. 애초에 무시하기로 한 대상(나 자신, 총구 등)이면 아군 취급
        if (ignores.Contains(target)) return true;

        // 2. 팀 ID를 비교해서 같은 팀이면 아군 취급 (관통)
        GenenricTeamId hitTeamId = TeamUtility.GetTeamId(target);
        return myTeamId.IsValid && hitTeamId.IsValid && myTeamId == hitTeamId;
    }

    // ==========================================
    // 3. 풀링 초기화 공통 로직
    // ==========================================
    protected virtual void OnDisable()
    {
        ignores.Clear();
        myTeamId = GenenricTeamId.NoTeamId; // 풀(Pool)에 들어갈 때 소속 초기화
        ownerObject = null;

        OnTargetHitEvent = null; // 메모리 누수 방지
    }

    protected void DealDamage(GameObject target, Vector3 hitPoint)
    {
        if (target.TryGetComponent<IDamagable>(out var damage))
        {
            damage?.OnDamage(ownerObject, null, hitPoint, damageEvent);
        }

        OnTargetHitEvent?.Invoke(target, hitPoint);
    }
}