using UnityEngine;

public class BleedEffect
    : DoTStatusEffect
{
    public BleedEffect(string id, string desc, float duration, float tick = 0.0f, float power = 0.0f) 
        : base(id, desc, duration, tick)
    {
        Triggers.Add(new PeriodicTickTrigger(tickInterval));
        Actions.Add(new ApplyDamageAction(DamageType.DOT_BLEED, power));
    }
}

