using UnityEngine;

/// <summary>
/// 데미지를 입히는 Action
/// </summary>
public class ApplyDamageAction
    : IEffectAction
{
    private DamageType type; 
    private float power; 
 
 

    public ApplyDamageAction(DamageType type = DamageType.NORMAL, float power = 0.0f)
    {
        this.type = type;
        this.power= power;
    }

    public void Execute(GameObject target, GameObject caster, int stackCount)
    {
        if (!target.TryGetComponent<DamageHandleComponent>(out var damageHandle))
            return;

        float finalPower = power;   

        var evt = new DamageEvent(0);
        evt.hitData.DamageType = type;

        switch(type)
        {
            case DamageType.DOT_POISON:
                evt.IsMaxHPPercent = true;
                evt.IgnoreDefense = true;
                evt.MaxHPRatio = (finalPower * stackCount ) * 0.01f;
                break;
            case DamageType.DOT_BURN:
                evt.IsMaxHPPercent = false;
                evt.IgnoreDefense = false;
                evt.value = finalPower * stackCount;
                break;
            case DamageType.DOT_BLEED:
                evt.IgnoreDefense = true; 
                evt.value = finalPower * stackCount; 
                break;
            case DamageType.DOT_HATERD:
                evt.IsMaxHPPercent = false;
                evt.IgnoreDefense = true;
                evt.value = finalPower * stackCount;
                break; 
        }

        Debug.Log("DOT Call");
        damageHandle.OnDamage(caster, evt);
    }
}
