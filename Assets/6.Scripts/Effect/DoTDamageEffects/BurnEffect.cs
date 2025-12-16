using UnityEngine;

public class BurnEffect
    : DoTStatusEffect
{
    public BurnEffect(string id, string desc, float duration, float tick = 0.0f, float power = 0.0f) 
        : base(id, desc, duration, tick)
    {
        Type = EffectType.DEBUFF;
        Triggers.Add(new PeriodicTickTrigger(tickInterval));
        Actions.Add(new ApplyDamageAction(DamageType.DOT_BURN, power));
    }

    public override int MaxStack =>  5;
    public override BuffStackPolicy StackPolicy => BuffStackPolicy.STACKABLE;
}

