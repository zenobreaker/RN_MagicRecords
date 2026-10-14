using UnityEngine;

/// <summary>스탯 버프의 적용, 만료 및 원상복구.</summary>
public class StatBuffEffect : BaseEffect
{
    private readonly StatModifier modifier;
    private readonly BuffStackPolicy stackPolicy;
    private StatusComponent status;

    public StatBuffEffect(string id, float duration, StatModifier modifier, ModifierValueType type)
        : this(id, duration, modifier) { }

    public StatBuffEffect(string id, float duration, StatusType type,
        float amount, ModifierValueType valueType = ModifierValueType.PERCENT)
        : this(id, duration, new StatModifier(type, amount, valueType)) { }

    public StatBuffEffect(string id, float duration, StatModifier modifier)
        : this(id, duration, modifier, null, "Stat Buff", BuffStackPolicy.REFRESH_ONLY) { }

    public StatBuffEffect(string id, float duration, StatModifier modifier,
        SO_BaseEffect definition, string displayName, BuffStackPolicy stackPolicy)
        : base(id, definition != null ? definition.description : "Stat Buff", duration)
    {
        this.modifier = modifier;
        this.stackPolicy = stackPolicy;
        Type = EffectType.BUFF;
        Name = displayName;
        if (definition != null)
        {
            FxIcon = definition.icon;
            FxObject = definition.vfxObject;
        }
        Actions.Add(new ApplyStatModifierAction(modifier));
    }

    public override BuffStackPolicy StackPolicy => stackPolicy;

    public override void OnApply(GameObject target, GameObject caster)
    {
        // An effect instance owns one modifier on one target.
        if (status != null) return;
        status = target != null ? target.GetComponent<StatusComponent>() : null;
        if (status == null) return;
        base.OnApply(target, caster);
        SynchronizeHealth();
    }

    public override void OnRemove()
    {
        if (status != null)
        {
            status.RemoveBuff(modifier);
            SynchronizeHealth();
        }
        status = null;
        base.OnRemove();
    }

    private void SynchronizeHealth()
    {
        if (modifier == null || modifier.type != StatusType.HEALTH) return;
        status.GetComponent<HealthPointComponent>().SafeInvoke(
            health => health.UpdateMaxHealthPreservingCurrent(status.GetStatusValue(StatusType.HEALTH)));
    }
}
