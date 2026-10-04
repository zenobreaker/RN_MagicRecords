using UnityEngine;

// Uses existing effect duration/refresh/removal; no separate armor lifetime system.
public class HitReactionResistanceEffect : BaseEffect
{
    private readonly HitReactionResistance resistance;
    private StateComponent state;

    public HitReactionResistanceEffect(string id, string description, float duration,
        HitReactionResistance resistance) : base(id, description, duration)
    {
        this.resistance = resistance;
        Type = EffectType.BUFF;
    }

    public override void OnApply(GameObject owner, GameObject appliedBy)
    {
        base.OnApply(owner, appliedBy);
        state = owner != null ? owner.GetComponent<StateComponent>() : null;
        state?.SetReactionResistance(this, resistance);
    }

    public override void OnRemove()
    {
        state?.RemoveReactionResistance(this);
        state = null;
        base.OnRemove();
    }
}
