using System.Collections.Generic;
using UnityEngine;

public static class CombatHelper
{
    public static bool IsFriendly(
      Character owner,
      GameObject target,
       HashSet<GameObject> ignores)
    {
        if (owner == null || target == null)
            return true;

        if (ignores != null && ignores.Contains(target))
            return true;

        if(target.transform.IsChildOf(owner.transform))
            return true;

        GenenricTeamId myTeamId = TeamUtility.GetTeamId(owner);

        GenenricTeamId hitTeamId = TeamUtility.GetTeamId(target);

        return myTeamId.IsValid && hitTeamId.IsValid && myTeamId == hitTeamId;
    }


    public static void ApplyDamage(
        Character owner,
        DamageData damageData,
        GameObject target,
        Vector3 hitPoint)
    {
        if (owner == null ||
            damageData == null ||
            target == null)
            return;

        DamageEvent damageEvent = damageData.GetMyDamageEvent(owner.GetComponent<StatusComponent>());

        if (target.TryGetComponent<IDamagable>(out var damage))
        {
            damage?.OnDamage(owner, null, hitPoint, damageEvent);
        }
    }
}