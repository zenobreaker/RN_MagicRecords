using UnityEngine;

public class StunEffect : CrowdControlEffect
{
    public override StatusEffectType EffectFlag => StatusEffectType.Stun;
    
    public StunEffect(string id, string desc, float duration) 
        : base(id, desc, duration)
    {
        Type = EffectType.DEBUFF;
    }

    public override void OnApply(GameObject owner, GameObject appliedBy)
    {
        base.OnApply(owner, appliedBy);
        //NotifyStatusChange(owner, StatusEffectType.Stun, isAd);
    }

    public override void OnRemove()
    {
        base.OnRemove();
    }
}
