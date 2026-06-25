using UnityEngine;

public static class DamageCalculator
{
    private readonly static float CONST_DEFNSE = 100.0f;
    private static int GLOBAL_ATTACK_ID_COUNTER = 0; 

    private static int GetNextAttackInstanceID()
    {
        if (GLOBAL_ATTACK_ID_COUNTER >= int.MaxValue)
            GLOBAL_ATTACK_ID_COUNTER = 0;
        return GLOBAL_ATTACK_ID_COUNTER++;
    }

    public static DamageEvent GetMyDamageEvent(StatusComponent status, DamageData data, 
        bool bFirstHit = false, bool bExtraCrit = false, float multiplier = 1.0f)
    {
        float attack = status.GetStatusValue(StatusType.ATTACK);
        float critRatio = status.GetStatusValue(StatusType.CRIT_RATIO);
        float critDmg = status.GetStatusValue(StatusType.CRIT_DMG);
        bool crit = false;

        float result = data.baseDamage + (attack * data.statCoefficient);
        result *= multiplier;
        
        crit = bExtraCrit;
        if (bExtraCrit)
        {
            float v = Random.Range(0.0f, 1.0f);
            if (v <= critRatio)
                crit = true;
        }

        if (crit)
            result *= critDmg;

        DamageEvent evt = new DamageEvent(result, crit, bFirstHit, data.hitData);
        evt.AttackInstanceID = GetNextAttackInstanceID();
        return evt;
    }

    public static float CalcDamage(StatusComponent status, DamageEvent damageEvent)
    {
        if (status == null || damageEvent == null)  
            return 0.0f;

        float resultDamage = damageEvent.BaseDamage;
        float defense = status.GetStatusValue(StatusType.DEFENSE);
        float multiplier = 1.0f; // 데미지 증폭
        if( damageEvent.DamageAmp > 0.0f)
            multiplier *= damageEvent.DamageAmp;

        // 잃은 체력 비례 데미지
        if( damageEvent.MissingHPRatio > 0.0f)
        {
            float mx = status.GetMaxHP();
            float curhp = status.GetCurrentHP();
            float missingHP =  mx - curhp;
            resultDamage += missingHP * damageEvent.MissingHPRatio;
        }

        // 최대 체력 비례 데미지 
        if(damageEvent.MissingHPRatio > 0.0f)
        {
            resultDamage += status.GetMaxHP() * damageEvent.MaxHPRatio; 
        }

        // 방어 무시 
        if (damageEvent.IgnoreDefense)
            return resultDamage * multiplier;

        //최소 방어력 제한
        defense = Mathf.Max(defense, -1.0f * CONST_DEFNSE + 0.001f);

        // 데미지 계산 공식 
        // 피해 감소율 = 피격자 방어력 / (방어 상수 + 피격자 방어력)
        float ratio = defense / (CONST_DEFNSE + defense);

        return  (resultDamage * (1 - ratio)) * multiplier;
    }
}
