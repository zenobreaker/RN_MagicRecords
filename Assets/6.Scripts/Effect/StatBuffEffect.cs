using UnityEngine;

/// <summary>
/// 스탯 버프
/// </summary>
public class StatBuffEffect
    : BaseEffect
{
    private StatModifier modifier;
    private StatusComponent status;

    public StatBuffEffect(string id, float duration, StatModifier modifier, ModifierValueType type)
       : base(id, "Stat Buff", duration)
    {
        this.modifier = modifier;

        // 스탯 적용은 Apply에서만 한다.
        Actions.Add(new ApplyStatModifierAction(modifier));
    }

    public StatBuffEffect(string id, float duration, StatusType type,
        float amount, ModifierValueType valueType = ModifierValueType.PERCENT)
        : base(id, "Stat Buff", duration)
    {
        modifier = new StatModifier(type, amount, valueType);

        // 스탯 적용은 Apply에서만 한다.
        Actions.Add(new ApplyStatModifierAction(modifier));
    }

    public StatBuffEffect(string id, float duration, StatModifier modifier)
        : base(id, "Stat Buff", duration)
    {
        this.modifier = modifier;

    }

    public override void OnRemove()
    {
        status?.RemoveBuff(modifier);
    }
}
