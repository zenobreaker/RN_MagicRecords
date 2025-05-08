using UnityEngine;

public static class DamageCalculator
{ 
    public static DamageEvent GetMyDamageEvent(StatusComponent status, DamageData data, bool bFirstHit = false)
    {
        if (status == null) return null;
        
        float attack = status.GetStatusValue(StatusType.Attack);
        float critRatio = status.GetStatusValue(StatusType.Crit_Ratio);
        float critDmg = status.GetStatusValue(StatusType.Crit_Dmg);
        bool crit = false;

        float result = attack * data.Power;
        float v = Random.Range(0.0f, 1.0f);
        if (v <= critRatio)
            crit = true;

        if (crit)
            result *= critDmg;

        return new DamageEvent(result, crit, bFirstHit);
    }
}
