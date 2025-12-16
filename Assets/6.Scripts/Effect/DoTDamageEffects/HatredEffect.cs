using UnityEngine;

public class HatredEffect
    : DoTStatusEffect
{
    public HatredEffect(string id, string desc, float duration, float tick = 0, float power =0) 
        : base(id, desc, duration, tick)
    {
        Type = EffectType.DEBUFF;
        Triggers.Add(new PeriodicTickTrigger(tickInterval));
        Actions.Add(new ApplyDamageAction(DamageType.DOT_HATERD, power));
    }

    public override BuffStackPolicy StackPolicy => BuffStackPolicy.IGNOREIFEXSIST;
}
