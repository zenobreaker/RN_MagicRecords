using UnityEngine;

public abstract class DoTStatusEffect
    : BaseEffect
{
    protected float tickInterval;

    protected StatusComponent appliersStatus; 

    public DoTStatusEffect(string id, string desc, float duration, float tick = 0.0f)
        : base(id, desc, duration)
    {
        tickInterval = tick;
    }

    public override void OnApply(GameObject owner, GameObject appliedBy)
    {
        base.OnApply(owner, appliedBy);

        if (appliedBy != null && appliedBy.TryGetComponent<StatusComponent>(out var status))
            appliersStatus = status;
    }
}
