using UnityEngine;

public static class DamageCalculator
{ 
    public static DamageEvent GetMyDamageEvent(StatusComponent status, DamageData data, bool bFirstHit = false)
    {
        if (status == null) return null;
        
        float attack = status.GetStatusValue(StatusType.ATTACK);
        float critRatio = status.GetStatusValue(StatusType.CRIT_RATIO);
        float critDmg = status.GetStatusValue(StatusType.CRIT_DMG);
        bool crit = false;

        float result = attack * data.Power;
        float v = Random.Range(0.0f, 1.0f);
        if (v <= critRatio)
            crit = true;

        if (crit)
            result *= critDmg;

        return new DamageEvent(result, crit, bFirstHit, data.hitData);
    }
}
