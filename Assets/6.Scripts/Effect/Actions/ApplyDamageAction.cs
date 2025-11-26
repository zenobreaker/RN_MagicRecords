using UnityEngine;

/// <summary>
/// 데미지를 입히는 Action
/// </summary>
public class ApplyDamageAction
    : IEffectAction
{
    private DamageType type; 
    private float power; 
    private HealthPointComponent healthPoint;
    private DamageEvent damageEvent;
    public ApplyDamageAction(DamageType type = DamageType.NORMAL, float power = 0.0f)
    {
        this.type = type;
        this.power= power;
    }

    public void Execute(GameObject target, int stackCount)
    {
        if (!target.TryGetComponent<DamageHandleComponent>(out var damageHandle))
            return;

        var evt = new DamageEvent(0);
        evt.hitData.DamageType = type;

        switch(type)
        {
            case DamageType.DOT_POISON:
                evt.IsMaxHPPercent = true;
                evt.IgnoreDefense = true;
                evt.MaxHPRatio = power * stackCount;
                break;
            case DamageType.DOT_BURN:
                evt.IsMaxHPPercent = false;
                evt.IgnoreDefense = false;
                evt.value = power * stackCount;
                break;
            case DamageType.DOT_BLEED:
                evt.IgnoreDefense = true; 
                evt.value = power * stackCount; 
                break;
        }

        Debug.Log("Burn Call");
        damageHandle.OnDamage(evt);
    }
}
