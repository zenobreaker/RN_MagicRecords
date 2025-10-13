using UnityEngine;

public static class DamageCalculator
{
    private readonly static float CONST_DEFNSE = 100.0f;
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

    public static float CalcDamage(StatusComponent status, DamageEvent damageEvent)
    {
        if (status == null ||  damageEvent == null) 
            return 0.0f;

        float value = damageEvent.value;
        float defense = status.GetStatusValue(StatusType.DEFENSE);

        //최소 방어력 제한
        defense = Mathf.Max(defense, -CONST_DEFNSE + 0.001f);

        // 데미지 계산 공식 
        // 피해 감소율 = 피격자 방어력 / (방어 상수 + 피격자 방어력)
        float ratio = defense / (CONST_DEFNSE + defense);
        
        return  value * (1 - ratio); 
    }
}
