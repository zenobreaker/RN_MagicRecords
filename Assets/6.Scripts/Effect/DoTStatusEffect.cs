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

    public override BaseEffect CreateTransferCopy(float duration)
    {
        float power = Actions.Count > 0 && Actions[0] is ApplyDamageAction damage ? damage.Power : 0f;
        BaseEffect copy = EffectFactory.CreateDotStatusEffect(ID, Description, duration, null, tickInterval, power);
        if (copy != null)
        {
            copy.FxIcon = FxIcon;
            copy.FxObject = FxObject;
        }
        return copy;
    }

    public override void OnApply(GameObject owner, GameObject appliedBy)
    {
        base.OnApply(owner, appliedBy);

        if (appliedBy != null && appliedBy.TryGetComponent<StatusComponent>(out var status))
        {
            appliersStatus = status;
        }
    }
}
